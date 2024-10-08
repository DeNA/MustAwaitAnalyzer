using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace FlowAnalysis;

public class DataFlowAnalysis
{
    private readonly Dictionary<int, List<IOperation>> _bbToAllOperations;
    
    private readonly Dictionary<IOperation, DefUseOperation> _opList;
    private readonly Dictionary<IOperation, BasicBlock> _opToBB;

    private readonly Dictionary<int, List<IOperation>> _def;
    private readonly Dictionary<int, List<IOperation>> _kill;
    private readonly Dictionary<int, List<IOperation>> _reach;

    public DataFlowAnalysis(SemanticModel model, ControlFlowGraph cfg)
    {
        _bbToAllOperations = new();
        _opList = new();
        _opToBB = new();
        CreateOpList(model, cfg);

        _def = new();
        ComputeDef(cfg);

        _kill = new();
        ComputeKill(cfg);

        _reach = new();
        ComputeReach(cfg);
    }

    public IList<IOperation> Def(int blockOrdinal) => _def[blockOrdinal];

    public IList<IOperation> Kill(int blockOrdinal) => _kill[blockOrdinal];

    public IList<IOperation> Reach(int blockOrdinal) => _reach[blockOrdinal];

    public IList<IOperation> ReachingFrom(IOperation op)
    {
        var defUseOp = _opList[op];
        return ReachingFrom(op, defUseOp.Def.ToArray());
    }

    public IList<IOperation> ReachingFrom(IOperation op, params ISymbol[] symbols)
    {
        var operations = new List<IOperation>();
        foreach (var candidate in _opList.Keys)
        {
            var reachingTo = ReachingTo(candidate, symbols);
            if (reachingTo.Contains(op))
            {
                operations.Add(candidate);
            }
        }

        return operations;
    }

    public IList<IOperation> ReachingTo(IOperation op)
    {
        var defUseOp = _opList[op];
        return ReachingTo(op, defUseOp.Use.ToArray());
    }

    public IList<IOperation> ReachingTo(IOperation op, params ISymbol[] symbols)
    {
        var targetSymbols = symbols.Intersect(_opList[op].Use);
        var reachOperations = new List<IOperation>();
        foreach (var s in targetSymbols)
        {
            var bb = _opToBB[op];

            if (CaseA(bb, s, out var reach))
            {
                reachOperations.Add(reach);
                continue;
            }

            reachOperations.AddRange(CaseB(bb, s));
        }

        return reachOperations;

        bool CaseA(BasicBlock bb, ISymbol s, out IOperation reach)
        {
            reach = null;
            var i = 0;
            var operations = _bbToAllOperations[bb.Ordinal];
            for (; i < operations.Count; i++)
            {
                if (operations[i].Equals(op))
                {
                    break;
                }
            }

            for (i--; i >= 0; i--)
            {
                var reachCandidate = operations[i];
                if (_opList[reachCandidate].Def.Contains(s))
                {
                    reach = reachCandidate;
                    return true;
                }
            }

            return false;
        }

        IList<IOperation> CaseB(BasicBlock bb, ISymbol s)
        {
            return _reach[bb.Ordinal].Where(reachCandidate => _opList[reachCandidate].Def.Contains(s)).ToList();
        }
    }

    public IOperation? FindOwnerDefiningVariables(IOperation operation)
    {
        if (operation is IObjectOrCollectionInitializerOperation { Parent: not null } init)
        {
            operation = init.Parent;
        }

        foreach (var owner in _opList.Keys)
        {
            var next = new Func<IOperation, IEnumerable<IOperation>>(o => o.Children);
            var condition = new Func<IOperation, bool>(o => o.Syntax.Equals(operation.Syntax) && _opList[owner].Def.Any());
            var results = FindRecursive(owner, next, condition);
            if (results.Any())
            {
                return owner;
            }
        }

        return null;
    }

    public bool TryGetOwner(IOperation operation, out IOperation foundOwner)
    {
        if (operation is IObjectOrCollectionInitializerOperation { Parent: not null } init)
        {
            operation = init.Parent;
        }

        foreach (var owner in _opList.Keys)
        {
            var next = new Func<IOperation, IEnumerable<IOperation>>(o => o.Children);
            var condition = new Func<IOperation, bool>(o => o.Syntax.Equals(operation.Syntax));
            var results = FindRecursive(owner, next, condition);
            if (results.Any())
            {
                foundOwner = owner;
                return true;
            }
        }

        foundOwner = default;
        return false;
    }

    public IList<IOperation> FindDef(ISymbol symbol)
    {
        var defs = new List<IOperation>();
        foreach (var e in _opList)
        {
            if (e.Value.Def.Contains(symbol))
            {
                defs.Add(e.Key);
            }
        }

        return defs;
    }

    public IList<ISymbol> DefinedVariablesAt(IOperation operation)
    {
        var owner = FindOwnerDefiningVariables(operation);
        return owner != null ? _opList[owner].Def : new List<ISymbol>();
    }

    private void CreateOpList(SemanticModel model, ControlFlowGraph cfg)
    {
        foreach (var bb in cfg.Blocks)
        {
            var operations = bb.Operations.ToList();
            if (bb.BranchValue != null)
            {
                operations.Add(bb.BranchValue);
            }

            _bbToAllOperations[bb.Ordinal] = operations;

            foreach (var op in operations)
            {
                _opToBB[op] = bb;

                var node = op.Syntax;
                while (node is not StatementSyntax
                       && node is not ExpressionSyntax
                       && node is not ConstructorInitializerSyntax
                       && node is not PrimaryConstructorBaseTypeSyntax
                       && node != null)
                {
                    node = node.Parent;
                }

                if (node == null)
                {
                    continue;
                }

                // Handle constructor invocation
                if (node is BaseObjectCreationExpressionSyntax
                    {
                        Parent: EqualsValueClauseSyntax
                        {
                            Parent: VariableDeclaratorSyntax
                            {
                                Parent: VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax local }
                            }
                        }
                    })
                {
                    node = local;
                }
                
                var dfa = model.AnalyzeDataFlow(node);
                var defined = new List<ISymbol>(dfa.WrittenInside);
                var used = new List<ISymbol>(dfa.ReadInside);
                if (op is IExpressionStatementOperation
                    {
                        Operation: IAssignmentOperation { Target: IFieldReferenceOperation field }
                    })
                {
                    defined.Add(field.Field);
                }

                used.AddRange(FindFieldReferences(op));
                var duOp = new DefUseOperation(op, defined, used);
                _opList[op] = duOp;
            }
        }

        return;

        IList<IFieldSymbol> FindFieldReferences(IOperation root)
        {
            var next = new Func<IOperation, IEnumerable<IOperation>>(o => o.Children);
            var condition = new Func<IOperation, bool>(o =>
                o is IFieldReferenceOperation f && !(o.Parent is IAssignmentOperation assign && assign.Target.Equals(f)));
            var fields = FindRecursive(root, next, condition);
            return fields.Select(f => ((IFieldReferenceOperation) f).Field).ToList();
        }
    }

    private void ComputeDef(ControlFlowGraph cfg)
    {
        foreach (var bb in cfg.Blocks)
        {
            var defOps = new List<IOperation>();
            var operations = _bbToAllOperations[bb.Ordinal];
            for (var i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                var successors = operations.Skip(i + 1);
                var isDef = _opList[op].Def
                    .Select(definedSymbol => successors.Select(succOp => _opList[succOp].Def)
                        .All(defSuccOp => !defSuccOp.Contains(definedSymbol))).Any(effective => effective);
                if (isDef)
                {
                    defOps.Add(op);
                }
            }

            _def[bb.Ordinal] = defOps;
        }
    }

    private void ComputeKill(ControlFlowGraph cfg)
    {
        for (var i = 0; i < cfg.Blocks.Length; i++)
        {
            var bb = cfg.Blocks[i];
            var killOps = new List<IOperation>();
            foreach (var symbol in _bbToAllOperations[bb.Ordinal].SelectMany(op => _opList[op].Def))
            {
                for (var j = 0; j < cfg.Blocks.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var bbAnother = cfg.Blocks[j];
                    killOps.AddRange(from opAnother in _bbToAllOperations[bbAnother.Ordinal]
                        from symbolAnother in _opList[opAnother].Def
                        where symbol.Equals(symbolAnother)
                        select opAnother);
                }
            }

            _kill[bb.Ordinal] = killOps;
        }
    }

    private void ComputeReach(ControlFlowGraph cfg)
    {
        foreach (var bb in cfg.Blocks)
        {
            _reach[bb.Ordinal] = new List<IOperation>();
        }

        bool changed;
        do
        {
            changed = false;
            foreach (var bb in cfg.Blocks)
            {
                var newReach = new HashSet<IOperation>();
                foreach (var pred in bb.Predecessors)
                {
                    var newReachSub = new HashSet<IOperation>();

                    var defPred = _def[pred.Source.Ordinal];
                    newReachSub.UnionWith(defPred);

                    var reachPred = new List<IOperation>(_reach[pred.Source.Ordinal]);
                    var removedByKill = reachPred.Except(_kill[pred.Source.Ordinal]);
                    newReachSub.UnionWith(removedByKill);

                    newReach.UnionWith(newReachSub);
                }

                if (_reach[bb.Ordinal].Count != newReach.Count)
                {
                    changed = true;
                    _reach[bb.Ordinal] = new List<IOperation>(newReach);
                }
            }
        } while (changed);
    }
    
    public static IEnumerable<T> FindRecursive<T>(T root, Func<T, IEnumerable<T>> next, Func<T, bool> condition)
    {
        var results = new List<T>();
        var stack = new Stack<T>();
        stack.Push(root);
        while (stack.Any())
        {
            var current = stack.Pop();
            if (condition(current))
            {
                results.Add(current);
            }
            
            foreach (var child in next(current))
            {
                stack.Push(child);
            }
        }

        return results;
    }
}
