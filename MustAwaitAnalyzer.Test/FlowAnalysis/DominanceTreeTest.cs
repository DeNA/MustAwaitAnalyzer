using FlowAnalysis;
using NUnit.Framework;

namespace DataFlowAnalysis.Test;

public class DominanceTreeTest
{
    private DirectedGraph<string> graph;
    private string root;

    [OneTimeSetUp]
    public void Init()
    {
        var edges = new List<(string, string)>
        {
            ("A", "Entry"),
            ("Exit", "Entry"),
            ("B", "A"),
            ("C", "A"),
            ("G", "B"),
            ("D", "C"),
            ("E", "C"),
            ("F", "D"),
            ("F", "E"),
            ("G", "F"),
            ("A", "G"),
            ("Exit", "G")
        };
        graph = new DirectedGraph<string>(edges);
        root = "Exit";
    }

    [TestCase]
    public void TestDominanceTree()
    {
        var dominanceTree = new DominanceTree<string>(graph, root);
        Assert.Multiple(() =>
        {
            Assert.That(dominanceTree.ImmediateDominator("A"), Is.EqualTo("G"));
            Assert.That(dominanceTree.ImmediateDominator("B"), Is.EqualTo("G"));
            Assert.That(dominanceTree.ImmediateDominator("C"), Is.EqualTo("F"));
            Assert.That(dominanceTree.ImmediateDominator("D"), Is.EqualTo("F"));
            Assert.That(dominanceTree.ImmediateDominator("E"), Is.EqualTo("F"));
            Assert.That(dominanceTree.ImmediateDominator("F"), Is.EqualTo("G"));
            Assert.That(dominanceTree.ImmediateDominator("G"), Is.EqualTo("Exit"));
            Assert.That(dominanceTree.ImmediateDominator("Entry"), Is.EqualTo("Exit"));
        });
    }

}
