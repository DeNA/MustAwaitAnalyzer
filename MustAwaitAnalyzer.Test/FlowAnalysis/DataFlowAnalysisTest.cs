using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using MustAwaitAnalyzer.Test;
using NUnit.Framework;

namespace DataFlowAnalysis.Test;

public class DataFlowAnalysisTest
{
    [TestCase]
    public void TestDefKillReach()
    {
        var path = "FlowAnalysis/Program02.cs";
        var dfa = CreateDataFlowAnalysis(path, out var cfg);
        var operations = cfg.Blocks.SelectMany(b => b.Operations).ToList();
        /*
         * index     operations
               0     j = 10;
               1     i = -8;

                     L3:
               2     i = i + 1;

                     L4:
               3     j = j - 1;
               4     c = j != 0;
                     if (c) goto L3;

               5     j = i / 2;
               6     c = i < 8;
                     if (c) goto L11;

               7     i = 2;

                     L11:
                     goto L4;
         */

        Assert.Multiple(() =>
        {
            // Validate DEF
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[0], operations[1] }, dfa.Def(1));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[2] }, dfa.Def(2));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[3], operations[4] }, dfa.Def(3));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[5], operations[6] }, dfa.Def(4));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[7] }, dfa.Def(5));

            // Validate KILL
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[2], operations[3], operations[5], operations[7] }, dfa.Kill(1));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[1], operations[7] }, dfa.Kill(2));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[0], operations[5], operations[6] }, dfa.Kill(3));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[0], operations[3], operations[4] }, dfa.Kill(4));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[1], operations[2] }, dfa.Kill(5));

            // Validate REACH for blocks
            CollectionAssert.AreEquivalent(new List<IOperation>(), dfa.Reach(1));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[0], operations[1], operations[2], operations[3], operations[4], operations[7] },
                dfa.Reach(2));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[0], operations[2], operations[3], operations[4], operations[5], operations[6], operations[7] }, 
                dfa.Reach(3));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[2], operations[3], operations[4], operations[7] }, 
                dfa.Reach(4));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[2], operations[5], operations[6], operations[7] }, 
                dfa.Reach(5));

            // Validate REACH for operation
            CollectionAssert.AreEquivalent(new List<IOperation>(), dfa.ReachingTo(operations[0]));
            CollectionAssert.AreEquivalent(new List<IOperation>(), dfa.ReachingTo(operations[1]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[1], operations[2], operations[7] },
                dfa.ReachingTo(operations[2]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[0], operations[3], operations[5] },
                dfa.ReachingTo(operations[3]));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[3] }, dfa.ReachingTo(operations[4]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[2], operations[7] },
                dfa.ReachingTo(operations[5]));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[2], operations[7] },
                dfa.ReachingTo(operations[6]));
            CollectionAssert.AreEquivalent(new List<IOperation>(), dfa.ReachingTo(operations[7]));

            CollectionAssert.AreEquivalent(new List<IOperation> { operations[3] }, dfa.ReachingFrom(operations[0]));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[2] }, dfa.ReachingFrom(operations[1]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[2], operations[5], operations[6] },
                dfa.ReachingFrom(operations[2]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[3], operations[4] },
                dfa.ReachingFrom(operations[3]));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[3] }, dfa.ReachingFrom(operations[5]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[2], operations[5], operations[6] },
                dfa.ReachingFrom(operations[7]));
        });
    }

    [TestCase]
    public void TestFind()
    {
        var path = "FlowAnalysis/Program03.cs";
        var dfa = CreateDataFlowAnalysis(path, out var cfg);
        var operations = cfg.Blocks.SelectMany(b => b.Operations).ToList();
        
        var referI = operations[2].Children.Last().Children.Last();
        Assert.Multiple(() =>
        {
            Assert.That(referI, Is.InstanceOf(typeof(ILocalReferenceOperation)));
            var variableI = (ILocalReferenceOperation)referI;

            Assert.That(dfa.TryGetOwner(operations[2], out var owner1), Is.EqualTo(true));
            Assert.That(owner1, Is.EqualTo(operations[2]));

            Assert.That(dfa.TryGetOwner(variableI, out var owner2), Is.EqualTo(true));
            Assert.That(owner2, Is.EqualTo(operations[2]));

            CollectionAssert.AreEquivalent(new List<IOperation> { operations[1] }, dfa.FindDef(variableI.Local));
            CollectionAssert.AreEquivalent(new List<ISymbol> { variableI.Local }, dfa.DefinedVariablesAt(operations[1]));
        });
    }

    [TestCase]
    public void TestFieldVariables()
    {
        var path = "FlowAnalysis/Program04.cs";
        var dfa = CreateDataFlowAnalysis(path, out var cfg);
        var operations = cfg.Blocks.SelectMany(b => b.Operations).ToList();
        /*
         * index     operations
               0     _j = 10;
               1     _i = -8;

                     L3:
               2     _i = _i + 1;

                     L4:
               3     _j = _j - 1;
               4     c = _j != 0;
                     if (c) goto L3;

               5     _j = _i / 2;
               6     c = _i < 8;
                     if (c) goto L11;

               7     _i = 2;

                     L11:
                     goto L4;
         */

        Assert.Multiple(() =>
        {
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[3] }, dfa.ReachingFrom(operations[0]));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[2] }, dfa.ReachingFrom(operations[1]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[2], operations[5], operations[6] },
                dfa.ReachingFrom(operations[2]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[3], operations[4] },
                dfa.ReachingFrom(operations[3]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { cfg.Blocks[3].BranchValue },
                dfa.ReachingFrom(operations[4]));
            CollectionAssert.AreEquivalent(new List<IOperation> { operations[3] }, dfa.ReachingFrom(operations[5]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { cfg.Blocks[4].BranchValue },
                dfa.ReachingFrom(operations[6]));
            CollectionAssert.AreEquivalent(
                new List<IOperation> { operations[2], operations[5], operations[6] },
                dfa.ReachingFrom(operations[7]));
        });
    }
    
    private static FlowAnalysis.DataFlowAnalysis CreateDataFlowAnalysis(string path, out ControlFlowGraph cfg)
    {
        var source = File.ReadAllText(TestData.GetPath(path));
        var options = CSharpParseOptions.Default;
        var tree = CSharpSyntaxTree.ParseText(source, options);
        var compilation = CSharpCompilation.Create("c", new[] { tree });
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        var methodBodySyntax = tree.GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<BaseMethodDeclarationSyntax>()
            .Last();
        cfg = ControlFlowGraph.Create(methodBodySyntax, model);
        var dfa = new FlowAnalysis.DataFlowAnalysis(model, cfg);
        return dfa;
    }
}
