using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MustAwaitAnalyzer.Test;
using NUnit.Framework;
using ControlFlowAnalysis = FlowAnalysis.ControlFlowAnalysis;

namespace DataFlowAnalysis.Test;

public class ControlFlowAnalysisTest
{
    [TestCase]
    public void TestControlFlowAnalysis()
    {
        var path = "FlowAnalysis/Program01.cs";
        var source = File.ReadAllText(TestData.GetPath(path));
        var options = CSharpParseOptions.Default;
        var tree = CSharpSyntaxTree.ParseText(source, options);
        var compilation = CSharpCompilation.Create("c", new[] { tree });
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        var methodBodySyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Last();
        var cfa = new ControlFlowAnalysis(model.GetOperation(methodBodySyntax));
        var operations = cfa.Operations;
        /*
         * index     operations
                     L:
               0     a = 1;
               1     if (b1)
                     {
               2         b = 2;
               3         some = 0;
                     }
                     else
                     {
               4         c = 3;
               5         if (b2)
                         {
               6             d = 4;
                         }
                         else
                         {
               7             e = 5;
                         }

               8         f = 6;
                     }

               9     g = 7;
              10     if (b3)
                     {
                         goto L;
                     }
         */
        
        Assert.Multiple(() =>
        {
            CollectionAssert.AreEquivalent(new List<IOperation> {operations[1], operations[9], operations[10]}, cfa.PostDominators(operations[0]));
            CollectionAssert.AreEquivalent(new List<IOperation> {operations[3], operations[9], operations[10]}, cfa.PostDominators(operations[2]));
            CollectionAssert.AreEquivalent(new List<IOperation> {operations[9], operations[10]}, cfa.PostDominators(operations[3]));
            CollectionAssert.AreEquivalent(new List<IOperation> {operations[5], operations[8], operations[9], operations[10]}, cfa.PostDominators(operations[4]));
            CollectionAssert.AreEquivalent(new List<IOperation> {operations[8], operations[9], operations[10]}, cfa.PostDominators(operations[6]));
            CollectionAssert.AreEquivalent(new List<IOperation> {operations[8], operations[9], operations[10]}, cfa.PostDominators(operations[7]));
            CollectionAssert.AreEquivalent(new List<IOperation> {operations[9], operations[10]}, cfa.PostDominators(operations[8]));
            CollectionAssert.AreEquivalent(new List<IOperation> (), cfa.PostDominators(operations[10]));
        });
    }

}
