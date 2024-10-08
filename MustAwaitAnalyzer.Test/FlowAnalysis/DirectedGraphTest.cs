using System.Text;
using FlowAnalysis;
using NUnit.Framework;

namespace DataFlowAnalysis.Test;

public class DirectedGraphTest
{
    private DirectedGraph<string> graph;

    [OneTimeSetUp]
    public void Init()
    {
        var edges = new List<(string, string)>
        {
            ("Entry", "A"),
            ("Entry", "Exit"),
            ("A", "B"),
            ("A", "C"),
            ("B", "G"),
            ("C", "D"),
            ("C", "E"),
            ("D", "F"),
            ("E", "F"),
            ("F", "G"),
            ("G", "A"),
            ("G", "Exit")
        };
        graph = new DirectedGraph<string>(edges);
    }

    [TestCase]
    public void TestVertices()
    {
        CollectionAssert.AreEquivalent(new List<string> { "A", "B", "C", "D", "E", "F", "G", "Entry", "Exit" },
            graph.Vertices);
    }

    [TestCase]
    public void TestAdjacent()
    {
        Assert.Multiple(() =>
        {
            CollectionAssert.AreEquivalent(new List<string> { "B", "C" }, graph.Adjacent("A"));
            CollectionAssert.AreEquivalent(new List<string> { "G" }, graph.Adjacent("B"));
            CollectionAssert.AreEquivalent(new List<string> { "D", "E" }, graph.Adjacent("C"));
            CollectionAssert.AreEquivalent(new List<string> { "F" }, graph.Adjacent("D"));
            CollectionAssert.AreEquivalent(new List<string> { "F" }, graph.Adjacent("E"));
            CollectionAssert.AreEquivalent(new List<string> { "G" }, graph.Adjacent("F"));
            CollectionAssert.AreEquivalent(new List<string> { "A", "Exit" }, graph.Adjacent("G"));
            CollectionAssert.AreEquivalent(new List<string> { "A", "Exit" }, graph.Adjacent("Entry"));
            CollectionAssert.AreEquivalent(new List<string>(), graph.Adjacent("Exit"));
        });
    }

    [TestCase]
    public void TestCreateReverseGraph()
    {
        var reverse = graph.CreateReverseGraph();
        Assert.Multiple(() =>
        {
            CollectionAssert.AreEquivalent(new List<string> { "A", "B", "C", "D", "E", "F", "G", "Entry", "Exit" },
                reverse.Vertices);

            CollectionAssert.AreEquivalent(new List<string> { "Entry", "G" }, reverse.Adjacent("A"));
            CollectionAssert.AreEquivalent(new List<string> { "A" }, reverse.Adjacent("B"));
            CollectionAssert.AreEquivalent(new List<string> { "A" }, reverse.Adjacent("C"));
            CollectionAssert.AreEquivalent(new List<string> { "C" }, reverse.Adjacent("D"));
            CollectionAssert.AreEquivalent(new List<string> { "C" }, reverse.Adjacent("E"));
            CollectionAssert.AreEquivalent(new List<string> { "D", "E" }, reverse.Adjacent("F"));
            CollectionAssert.AreEquivalent(new List<string> { "B", "F" }, reverse.Adjacent("G"));
            CollectionAssert.AreEquivalent(new List<string>(), reverse.Adjacent("Entry"));
            CollectionAssert.AreEquivalent(new List<string> { "Entry", "G" }, reverse.Adjacent("Exit"));
        });
    }

    [TestCase]
    public void TestDFSearch()
    {
        var visitor = new TestIDFVisitor();
        graph.DFSearch("Entry", visitor);

        var visitExpected1 = new List<string> { "Entry", "A", "B", "G", "Exit", "C", "D", "F", "E" };
        var visitExpected2 = new List<string> { "Entry", "A", "B", "G", "Exit", "C", "E", "F", "D" };
        var visitExpected3 = new List<string> { "Entry", "A", "C", "D", "F", "G", "Exit", "E", "B" };
        var visitExpected4 = new List<string> { "Entry", "A", "C", "E", "F", "G", "Exit", "D", "B" };
        var visitExpected5 = new List<string> { "Entry", "Exit", "A", "B", "G", "C", "D", "F", "E" };
        var visitExpected6 = new List<string> { "Entry", "Exit", "A", "C", "D", "F", "G", "E", "B" };
        var visitExpected7 = new List<string> { "Entry", "Exit", "A", "C", "E", "F", "G", "D", "B" };

        var leaveExpected1 = new List<string> { "Exit", "G", "B", "F", "D", "E", "C", "A", "Entry" };
        var leaveExpected2 = new List<string> { "Exit", "G", "B", "F", "E", "D", "C", "A", "Entry" };
        var leaveExpected3 = new List<string> { "Exit", "G", "F", "D", "E", "C", "B", "A", "Entry" };
        var leaveExpected4 = new List<string> { "Exit", "G", "F", "E", "D", "C", "B", "A", "Entry" };

        Assert.Multiple(() =>
        {
            CollectionAnyMatchAssert(visitor.OnVisitList, 
                visitExpected1, visitExpected2, visitExpected3,
                visitExpected4, visitExpected5, visitExpected6, visitExpected7);
            CollectionAnyMatchAssert(visitor.OnLeaveList, 
                leaveExpected1, leaveExpected2, leaveExpected3, leaveExpected4);
        });
    }

    [TestCase]
    public void TestIsConnected()
    {
        var vertices = new List<string> { "1", "2", "3", "A", "B", "C" };
        var edges = new List<(string, string)>
        {
            ("1", "2"),
            ("1", "3"),
            ("A", "B")
        };
        var disconnectedGraph = new DirectedGraph<string>(vertices, edges);
        
        Assert.Multiple(() =>
        {
            Assert.That(graph.IsConnectedAsUndirectedGraph(), Is.True);
            Assert.That(disconnectedGraph.IsConnectedAsUndirectedGraph(), Is.False);
        });
    }

    private static void CollectionAnyMatchAssert<T>(List<T> actual, params List<T>[] expected)
    {
        if (expected.Any(actual.SequenceEqual))
        {
            return;
        }

        var builder = new StringBuilder();
        var actualStr = "{ " + string.Join(", ", actual) + " }";
        builder.AppendLine($"{actualStr} did not match any of the following: ");
        foreach (var e in expected)
        {
            var eStr = "{ " + string.Join(", ", e) + " }";
            builder.AppendLine($"\t{eStr}");
        }

        Assert.Fail(builder.ToString());
    }

    private class TestIDFVisitor : IDFVisitor<string>
    {
        internal List<string> OnVisitList { get; } = new();
        internal List<string> OnLeaveList { get; } = new();

        public void OnVisit(string v)
        {
            OnVisitList.Add(v);
        }

        public void OnLeave(string v)
        {
            OnLeaveList.Add(v);
        }
    }
}
