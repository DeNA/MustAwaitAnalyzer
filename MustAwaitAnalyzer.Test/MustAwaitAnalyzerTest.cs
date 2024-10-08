using System.Collections.Immutable;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Dena.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MustAwaitAnalyzer.Test;

public class MustAwaitAnalyzerTest
{
    // Exclude errors unrelated to DENA008 from the test.
    private HashSet<string> excludeIDs = new HashSet<string>()
    {
        "CS0219",
        "CS1591",
        "CS8019",
        "CS0766",
        "CS0201",
        "CS8124",
        "CS8181",
        "CS1922",
        "CS4014",
        "CS0029",
        "CS1998",
        "CS1950",
        "CS1503"
    };

    [TestCase("SuccessCase/Task/CastWithAwait.cs")]
    [TestCase("SuccessCase/Task/LambdaWithAwait.cs")]
    [TestCase("SuccessCase/Task/ObjectCreation.cs")]
    [TestCase("SuccessCase/Task/PartialMethod.cs")]
    [TestCase("SuccessCase/Task/TargetTypedNewExpression.cs")]
    [TestCase("SuccessCase/Task/UseAwait.cs")]
    [TestCase("SuccessCase/Task/UsingWithAwait.cs")]
    [TestCase("SuccessCase/Task/YieldReturn.cs")]
    [TestCase("SuccessCase/Task/IfWithAwait.cs")]
    [TestCase("SuccessCase/UniTask/CastWithAwait.cs")]
    [TestCase("SuccessCase/UniTask/LambdaWithAwait.cs")]
    [TestCase("SuccessCase/UniTask/UseAwait.cs")]
    [TestCase("SuccessCase/UseDummyTask.cs")]
    [TestCase("SuccessCase/UniTask/MethodChain.cs")]
    [TestCase("SuccessCase/Task/DualMethodChain.cs")]
    [TestCase("SuccessCase/Task/PropertyReference.cs")]
    [TestCase("SuccessCase/Task/Variable.cs")]
    [TestCase("SuccessCase/Task/VariablePatterns.cs")]
    [TestCase("SuccessCase/Task/FieldReference.cs")]
    [TestCase("SuccessCase/Task/Excluded.cs")]
    [TestCase("SuccessCase/UniTask/Excluded.cs")]
    [TestCase("SuccessCase/Task/TaskList.cs")]
    [TestCase("SuccessCase/UniTask/TaskList.cs")]
    public async Task SuccessCase(string fileName)
    {
        var analyzer = new MustAwaitAnalyzer();
        var sourceWithEmbeddedMarkers = await File.ReadAllTextAsync(TestData.GetPath(fileName));
        var (source, expected) = TestDataParser
            .CreateSourceAndExpectedDiagnostic(sourceWithEmbeddedMarkers);


        var diagnostics = await DiagnosticAnalyzerRunner.Run(
            analyzer,
            new[] { typeof(UniTask) },
            codes: source
        );

        var actual = ExcludeDDID(diagnostics, excludeIDs);
        DiagnosticsAssert.IsEmpty(actual);
    }

    [TestCase("FailedCase/Task/CastWithOutAwait.txt")]
    [TestCase("FailedCase/Task/LambdaWithOutAwait.txt")]
    [TestCase("FailedCase/Task/ObjectCreationWithOutAwait.txt")]
    [TestCase("FailedCase/Task/PartialMethod.txt")]
    [TestCase("FailedCase/Task/TargetTypedNewExpression.txt")]
    [TestCase("FailedCase/Task/UsingWithOutAwait.txt")]
    [TestCase("FailedCase/Task/WithOutAwait.txt")]
    [TestCase("FailedCase/Task/YieldReturn.txt")]
    [TestCase("FailedCase/Task/IfWithOutAwait.txt")]
    [TestCase("FailedCase/Task/PropertyReference.txt")]
    [TestCase("FailedCase/Task/Variable.txt")]
    [TestCase("FailedCase/UniTask/CastWithOutAwait.txt")]
    [TestCase("FailedCase/UniTask/LambdaWithOutAwait.txt")]
    [TestCase("FailedCase/UniTask/WithOutAwait.txt")]
    [TestCase("FailedCase/Task/FieldReference.txt")]
    [TestCase("FailedCase/Task/TaskList.txt")]
    [TestCase("FailedCase/Task/TaskList2.txt")]
    [TestCase("FailedCase/Task/VariablePatterns.txt")]
    public async Task FailedCase(string fileName)
    {
        var analyzer = new MustAwaitAnalyzer();
        var sourceWithEmbeddedMarkers = await File.ReadAllTextAsync(TestData.GetPath(fileName));
        var (source, expected) = TestDataParser
            .CreateSourceAndExpectedDiagnostic(sourceWithEmbeddedMarkers);


        var diagnostics = await DiagnosticAnalyzerRunner.Run(
            analyzer,
            new[] { typeof(UniTask) },
            codes: source
        );

        var actual = ExcludeDDID(diagnostics, excludeIDs);
        DiagnosticsAssert.AreEqual(expected, actual);
    }

    private static Diagnostic[] ExcludeDDID(ImmutableArray<Diagnostic> diagnostics, HashSet<string> excludeIDs)
    {
        return diagnostics.Where(diagnostic => !excludeIDs.Contains(diagnostic.Id)).ToArray();
    }
}
