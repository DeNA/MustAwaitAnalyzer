using System.Collections.Generic;
using System.Linq;

namespace FlowAnalysis;

// Keith D. Cooper, Timothy J. Harvey and Ken Kennedy: A Simple, Fast Dominance Algorithm.
public class DominanceTree<T>
{

    private readonly Dictionary<T, int> _vertexToOrder = new();
    private readonly Dictionary<T, T> _immediateDominator = new();
    
    public DominanceTree(DirectedGraph<T> graph, T root)
    {
        Initialize(graph, root);
        UpdateDominator(graph);
    }
    
    private void Initialize(DirectedGraph<T> graph, T root)
    {
        var visitor = new PostOrderVisitor();
        graph.DFSearch(root, visitor);
        
        for (var i = 0; i < visitor.Vertices.Count; i++)
        {
            _vertexToOrder[visitor.Vertices[i]] = visitor.Vertices.Count - 1 - i;
        }

        foreach (var from in graph.Vertices)
        {
            foreach (var to in graph.Adjacent(from).Where(to => _vertexToOrder[from] < _vertexToOrder[to]))
            {
                _immediateDominator[to] = from;
            }
        }
    }

    private void UpdateDominator(DirectedGraph<T> graph)
    {
        var reverse = graph.CreateReverseGraph();
        
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var v in graph.Vertices)
            {
                foreach (var pred in reverse.Adjacent(v))
                {
                    var idom = _immediateDominator[v];
                    var nca = NearestCommonAncestor(idom, pred);
                    if (!idom.Equals(nca))
                    {
                        changed = true;
                        _immediateDominator[v] = nca;
                    }
                }
            }
        }
    }

    public T ImmediateDominator(T vertex) => _immediateDominator[vertex];

    private T NearestCommonAncestor(T v1, T v2)
    {
        while (!v1.Equals(v2))
        {
            var i1 = _vertexToOrder[v1];
            var i2 = _vertexToOrder[v2];
            if (i1 > i2)
            {
                v1 = _immediateDominator[v1];
            }
            else
            {
                v2 = _immediateDominator[v2];
            }
        }

        return v1;
    }

    private class PostOrderVisitor : IDFVisitor<T>
    {
        internal List<T> Vertices { get; } = new();
        
        public void OnVisit(T v)
        {
        }

        public void OnLeave(T v)
        {
            Vertices.Add(v);
        }
    }
}
