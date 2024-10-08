using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace FlowAnalysis;

public class ControlFlowAnalysis
{
    public ControlFlowGraph CFG { get; }
    private readonly Dictionary<BasicBlock, IList<IOperation>> bbToOperations = new();
    private readonly Dictionary<IOperation, BasicBlock> operationToBB = new();
    private readonly Dictionary<BasicBlock, IList<BasicBlock>> postDominators;
    private readonly DirectedGraph<int> cfgDag;
    
    public IList<IOperation> Operations => operationToBB.Keys.ToList();
    
    public ControlFlowAnalysis(IOperation operation)
    {
        if (!TryGetRootOperation(operation, out var root))
        {
            throw new ControlFlowAnalysisException();
        }

        if (operation.SemanticModel == null)
        {
            throw new ControlFlowAnalysisException();
        }
        
        CFG = ControlFlowGraph.Create(root.Syntax, operation.SemanticModel) ?? throw new ControlFlowAnalysisException();

        if (TryGetLocalFunction(operation, out var localFunction))
        {
            CFG = CFG.GetLocalFunctionControlFlowGraph(localFunction);
        }
        
        if (TryGetLambda(operation, CFG, out var lambda))
        {
            CFG = CFG.GetAnonymousFunctionControlFlowGraph(lambda);
        }
        
        cfgDag = CreateCFGDag(out var ordinalToBB, out var exit);
        if (!cfgDag.IsConnectedAsUndirectedGraph())
        {
            cfgDag = CreateConnectedGraphFromEntry(cfgDag);
        }

        var postDominanceTree = new DominanceTree<int>(cfgDag.CreateReverseGraph(), exit);

        var blockToParent = new Dictionary<BasicBlock, BasicBlock>();
        foreach (var v in cfgDag.Vertices)
        {
            if (v == exit)
            {
                continue;
            }

            var myself = ordinalToBB[v];
            var parent = ordinalToBB[postDominanceTree.ImmediateDominator(v)];
            blockToParent[myself] = parent;
        }

        postDominators = new();
        foreach (var e in blockToParent)
        {
            var doms = new List<BasicBlock> { e.Value };
            var next = e.Value;
            while (blockToParent.TryGetValue(next, out var parent))
            {
                doms.Add(parent);
                next = parent;
            }

            postDominators[e.Key] = doms;
        }
    }

    private static bool TryGetLambda(IOperation current, ControlFlowGraph cfg, out IFlowAnonymousFunctionOperation lambda)
    {
        if (!InLambda(current))
        {
            lambda = default;
            return false;
        }
        
        var blockOperations = cfg.Blocks.SelectMany(bb => bb.Operations).ToList();
        var branches = cfg.Blocks.Select(bb => bb.BranchValue).Where(op => op != null).ToList();
        blockOperations.AddRange(branches);
        foreach (var candidate in blockOperations)
        {
            for (var op = current; op != null; op = op.Parent)
            {
                if (op.Syntax.Equals(candidate.Syntax))
                {
                    var func = candidate.DescendantsAndSelf().OfType<IFlowAnonymousFunctionOperation>();
                    lambda = func.FirstOrDefault();
                    return func.Any();
                }
            }
        }

        lambda = default;
        return false;

        bool InLambda(IOperation operation)
        {
            for (var op = operation; op != null; op = op.Parent)
            {
                if (op is IAnonymousFunctionOperation)
                {
                    return true;
                }
            }

            return false;
        }
    }
    
    private static bool TryGetLocalFunction(IOperation current, out IMethodSymbol localFunction)
    {
        for (var op = current; op != null; op = op.Parent)
        {
            if (op is ILocalFunctionOperation local)
            {
                localFunction = local.Symbol;
                return true;
            }
        }

        localFunction = default;
        return false;
    }
    
    private bool TryGetRootOperation(IOperation current, out IOperation root)
    {
        for (var op = current; op != null; op = op.Parent)
        {
            if (op is IMethodBodyOperation)
            {
                root = op;
                return true;
            }
        }

        root = null;
        return false;
    }
    
    private DirectedGraph<int> CreateConnectedGraphFromEntry(DirectedGraph<int> cfgDag)
    {
        var visitor = new DirectedGraph<int>.SimpleVisitor();
        cfgDag.DFSearch(0, visitor);

        var edges = new List<(int, int)>();
        foreach (var from in cfgDag.Vertices)
        {
            foreach (var to in cfgDag.Adjacent(from))
            {
                if (visitor.Visited.Contains(from) && visitor.Visited.Contains(to))
                {
                    edges.Add((from, to));
                }
            }
        }

        return new DirectedGraph<int>(edges);
    }

    private DirectedGraph<int> CreateCFGDag(out Dictionary<int, BasicBlock> ordinalToBB, out int exit)
    {
        ordinalToBB = new Dictionary<int, BasicBlock>();
        var edges = new List<(int, int)>();
        var entry = 0;
        exit = 0;
        foreach (var bb in CFG.Blocks)
        {
            switch (bb.Kind)
            {
                case BasicBlockKind.Entry:
                    entry = bb.Ordinal;
                    break;
                case BasicBlockKind.Exit:
                    exit = bb.Ordinal;
                    break;
            }

            var operations = bb.Operations.ToList();
            if (bb.BranchValue != null)
            {
                operations.Add(bb.BranchValue);
            }

            bbToOperations[bb] = operations;
            
            foreach (var op in operations)
            {
                operationToBB[op] = bb;
            }

            ordinalToBB[bb.Ordinal] = bb;

            if (bb.FallThroughSuccessor is { Destination: not null })
            {
                edges.Add((bb.Ordinal, bb.FallThroughSuccessor.Destination.Ordinal));
            }

            if (bb.ConditionalSuccessor is { Destination: not null })
            {
                edges.Add((bb.Ordinal, bb.ConditionalSuccessor.Destination.Ordinal));
            }
        }

        edges.Add((entry, exit));

        var cfgDag = new DirectedGraph<int>(ordinalToBB.Keys, edges);
        return cfgDag;
    }

    public IList<IOperation> PostDominators(IOperation op)
    {
        if (!operationToBB.TryGetValue(op, out var basicBlock))
        {
            return new List<IOperation>();
        }
        
        if (!postDominators.TryGetValue(basicBlock, out var postBlocks))
        {
            return new List<IOperation>();
        }
        
        var list = postBlocks.SelectMany(bb => bbToOperations[bb]).ToList();
        var blockOperations = bbToOperations[operationToBB[op]];
        var successor = false;
        foreach (var neighbor in blockOperations)
        {
            if (!successor && neighbor.Equals(op))
            {
                successor = true;
                continue;
            }

            if (successor)
            {
                list.Add(neighbor);
            }
        }

        return list;
    }

    public IList<IOperation> OperationsInSuccessorBlock(IOperation operation)
    {
        var bb = operationToBB[operation];
        var successors = cfgDag.Adjacent(bb.Ordinal);

        var result = new List<IOperation>();
        foreach (var succ in successors)
        {
            var op = bbToOperations[CFG.Blocks[succ]];
            result.AddRange(op);
        }

        return result;
    }
}

public class ControlFlowAnalysisException : Exception
{
    public ControlFlowAnalysisException() : base(nameof(ControlFlowAnalysis) +
                                                 " cannot analyze a method including exception handling.")
    {
    }
}
