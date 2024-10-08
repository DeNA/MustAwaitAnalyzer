using System.Collections.Generic;
using System.Linq;

namespace FlowAnalysis;

public class DirectedGraph<T>
{
    public List<T> Vertices { get; }
    private readonly Dictionary<T, List<T>> _forward = new();
    private static readonly List<T> s_empty = new();

    public DirectedGraph(IEnumerable<T> vertices, IEnumerable<(T, T)> edges)
    {
        var vSet = new HashSet<T>();
        foreach (var (from, to) in edges)
        {
            vSet.Add(from);
            vSet.Add(to);
            if (!_forward.TryGetValue(from, out var toList))
            {
                toList = new();
            }

            toList.Add(to);
            _forward[from] = toList;
        }

        foreach (var v in vertices)
        {
            if (!vSet.Contains(v))
            {
                _forward[v] = s_empty;
                vSet.Add(v);
            }
        }

        Vertices = vSet.ToList();
    }

    public DirectedGraph(IEnumerable<(T, T)> edges) : this(new List<T>(), edges)
    {
    }

    public List<T> Adjacent(T from) => _forward.ContainsKey(from) ? _forward[from] : s_empty;

    public DirectedGraph<T> CreateReverseGraph()
    {
        var edges = (from @from in Vertices from to in Adjacent(@from) select (to, @from)).ToList();
        return new DirectedGraph<T>(edges);
    }

    public void DFSearch(T root, IDFVisitor<T> visitor)
    {
        var visited = new HashSet<T>();
        var stack = new Stack<DFSearchProgress>();
        stack.Push(new DFSearchProgress(this, root));

        while (stack.Any())
        {
            var progress = stack.Peek();
            
            if (!visited.Contains(progress.Vertex))
            {
                visitor.OnVisit(progress.Vertex);
            }
            visited.Add(progress.Vertex);

            if (FindNext(progress, out var next))
            {
                stack.Push(new DFSearchProgress(this, next));
            }
            else
            {
                stack.Pop();
                visitor.OnLeave(progress.Vertex);
            }
        }

        return;

        bool FindNext(DFSearchProgress progress, out T next)
        {
            next = default;
            while (progress.HasNext())
            {
                var nextCandidate = progress.Next();
                if (!visited.Contains(nextCandidate))
                {
                    next = nextCandidate;
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsConnectedAsUndirectedGraph()
    {
        foreach (var v in Vertices)
        {
            var visitor = new SimpleVisitor();
            DFSearch(v, visitor);
            if (visitor.Visited.Count == Vertices.Count)
            {
                return true;
            }
        }

        return false;
    }

    public class SimpleVisitor : IDFVisitor<T>
    {
        internal HashSet<T> Visited { get; } = new();

        public void OnVisit(T v)
        {
            Visited.Add(v);
        }

        public void OnLeave(T v)
        {
        }
    }

    private class DFSearchProgress
    {
        private readonly DirectedGraph<T> graph;
        internal T Vertex { get; }
        private int edgeCurrentIndex;

        internal DFSearchProgress(DirectedGraph<T> graph, T v)
        {
            this.graph = graph;
            this.Vertex = v;
            this.edgeCurrentIndex = 0;
        }

        internal bool HasNext()
        {
            var toList = graph.Adjacent(Vertex);
            return edgeCurrentIndex < toList.Count;
        }

        internal T Next()
        {
            var next = graph.Adjacent(Vertex)[edgeCurrentIndex];
            edgeCurrentIndex++;
            return next;
        }
    }
}

public interface IDFVisitor<T>
{
    public void OnVisit(T v);

    public void OnLeave(T v);
}
