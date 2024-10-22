using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AnalyzerExtensions;
using FlowAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ControlFlowAnalysis = FlowAnalysis.ControlFlowAnalysis;
using DataFlowAnalysis = FlowAnalysis.DataFlowAnalysis;

namespace MustAwaitAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MustAwaitAnalyzer : DiagnosticAnalyzer
{
    private static ImmutableArray<string> s_targetTypes =
        ImmutableArray.Create<string>("System.Threading.Tasks.Task", "Cysharp.Threading.Tasks.UniTask");

    private ImmutableArray<string> _blackList;

    /// <summary>
    /// Exclude method chains that contain the registered method calls from analysis.
    /// </summary>
    public MustAwaitAnalyzer()
    {
        _blackList = ImmutableArray.Create<string>
        (
            "Terminate",
            "Forget"
        );
    }

    // Constructor for testing
    [ExcludeFromCodeCoverage]
    public MustAwaitAnalyzer(IEnumerable<string> blackList)
    {
        _blackList = blackList.ToImmutableArray();
    }

    private static readonly DiagnosticDescriptor Rule01 = new DiagnosticDescriptor(
        id: "DENA008",
        title: "Must use await",
        messageFormat: "Must use await",
        category: "DenaUnityAnalyzers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule01);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeAsyncMethod, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
    }

    private static void AnalyzeFieldReference(OperationAnalysisContext context)
    {
        var fieldReference = (IFieldReferenceOperation)context.Operation;
        if (!IsTarget(fieldReference))
        {
            return;
        }

        if (!IsAwait(fieldReference.Parent.Syntax) && !IsAwaitOnDataFlow(fieldReference))
        {
            context.ReportDiagnostic(fieldReference.Syntax.CreateDiagnostic(
                Rule01)
            );
        }
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        var propertyReference = (IPropertyReferenceOperation)context.Operation;
        if (!IsTarget(propertyReference))
        {
            return;
        }

        if (!IsAwait(propertyReference.Parent.Syntax) && !IsAwaitOnDataFlow(propertyReference))
        {
            context.ReportDiagnostic(propertyReference.Syntax.CreateDiagnostic(
                Rule01)
            );
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        var objectCreation = (IObjectCreationOperation)context.Operation;
        if (!IsTarget(objectCreation))
        {
            return;
        }

        if (!IsAwait(objectCreation.Parent.Syntax) && !IsAwaitOnDataFlow(objectCreation))
        {
            context.ReportDiagnostic(objectCreation.Syntax.CreateDiagnostic(
                Rule01)
            );
        }
    }

    private void AnalyzeAsyncMethod(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (!IsTarget(invocation))
        {
            return;
        }

        var invocationList = new List<IOperation>();
        for (IOperation op = invocation; op != null; op = op.Parent)
        {
            if (op.Syntax is LambdaExpressionSyntax)
            {
                break;
            }

            if (op is IInvocationOperation or IPropertyReferenceOperation)
            {
                invocationList.Add(op);
            }
        }

        if (invocationList.Last() is IInvocationOperation inv)
        {
            if (_blackList.Contains(inv.TargetMethod.Name))
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (!IsAwait(invocation.Parent.Syntax) && !IsAwaitOnDataFlow(invocation))
        {
            context.ReportDiagnostic(invocation.Syntax.CreateDiagnostic(
                Rule01)
            );
        }
    }

    private static bool IsTarget(IObjectCreationOperation creation)
    {
        return s_targetTypes.Contains(creation.Type.FullName());
    }

    private static bool IsAwaitOnDataFlow(IOperation target)
    {
        ControlFlowAnalysis cfa;
        try
        {
            cfa = new ControlFlowAnalysis(target);
        }
        catch (ControlFlowAnalysisException)
        {
            return false;
        }

        var dataflow = new DataFlowAnalysis(target.SemanticModel, cfa.CFG);
        if (!dataflow.TryGetOwner(target, out var def))
        {
            return false;
        }

        if (TryGetTernaryConditionalOperator(def, out var cond))
        {
            def = cond;
        }

        // Determine whether def is an assignment to a variable (there are assignments to local variables and field variables).
        if (def is IAssignmentOperation or IExpressionStatementOperation { Operation: IAssignmentOperation })
        {
            var taskVariables = dataflow.DefinedVariablesAt(def)
                .Where(s => s switch
                {
                    ILocalSymbol l => s_targetTypes.Contains(l.Type.FullName()),
                    IFieldSymbol f => s_targetTypes.Contains(f.Type.FullName()),
                    IParameterSymbol p => s_targetTypes.Contains(p.Type.FullName()),
                    _ => false
                }).ToList();
            if (taskVariables.Any())
            {
                return IsAwaitViaVariable(def, cfa, dataflow, taskVariables);
            }
        }

        return IsAwaitViaList(target, cfa, dataflow);

        // Handle the ternary conditional operator.
        bool TryGetTernaryConditionalOperator(IOperation op, out IOperation result)
        {
            foreach (var succ in cfa.OperationsInSuccessorBlock(op))
            {
                if (succ is IAssignmentOperation { Value.Syntax: ConditionalExpressionSyntax cd } 
                    && (cd.WhenTrue.Equals(op.Syntax) || cd.WhenFalse.Equals(op.Syntax)))
                {
                    result = succ;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }

    private static bool IsAwaitViaVariable(IOperation source, ControlFlowAnalysis cfa, DataFlowAnalysis dataflow,
        IEnumerable<ISymbol> variables)
    {
        var postDominators = cfa.PostDominators(source);

        foreach (var v in variables)
        {
            var sink = dataflow.ReachingFrom(source, v);
            if (!sink.Any())
            {
                return false;
            }

            if (!sink.All(s => postDominators.Contains(s) && s.Children.Any(c => c is IAwaitOperation)))
            {
                if (!sink.All(s => TryGetLocalReference(s, v, out var local) && IsAwaitViaList(local, cfa, dataflow)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsAwaitViaList(IOperation itemToBeAdded, ControlFlowAnalysis cfa, DataFlowAnalysis dataflow)
    {
        if (itemToBeAdded.Parent is IConversionOperation)
        {
            itemToBeAdded = itemToBeAdded.Parent;
        }

        if (itemToBeAdded.Parent is not IArgumentOperation { Parent: IInvocationOperation invocationForList })
        {
            return false;
        }

        if (!TryGetTaskList(invocationForList, out var listVar))
        {
            return false;
        }

        // Warn if there is a path where the Task variable is not added to the list.
        if (itemToBeAdded is ILocalReferenceOperation local)
        {
            var itemSource = dataflow.FindDef(local.Local);
            if (!dataflow.TryGetOwner(invocationForList, out var itemSink)
                || !itemSource.All(source => cfa.PostDominators(source).Contains(itemSink)))
            {
                return false;
            }
        }

        if (!dataflow.TryGetOwner(invocationForList, out var listOperation))
        {
            return false;
        }
        var postDominators = cfa.PostDominators(listOperation);
        
        // Find the instruction that defines the Task list,
        // and if the list is awaited among the post-dominators of the list manipulation instructions, do not issue a warning.
        var defs = dataflow.FindDef(listVar);
        foreach (var source in defs)
        {
            var sink = dataflow.ReachingFrom(source);
            if (sink.Any(s => postDominators.Contains(s) && s.Children.Any(IsAwaitingForList)))
            {
                return true;
            }
        }

        return false;

        bool TryGetTaskList(IInvocationOperation inv, out ISymbol taskListVariable)
        {
            taskListVariable = default;

            if (inv.Parent is IObjectOrCollectionInitializerOperation init)
            {
                var vars = dataflow.DefinedVariablesAt(init).Where(IsTaskListType).ToArray();
                if (vars.Length == 1)
                {
                    taskListVariable = vars.First();
                    return true;
                }

                return false;
            }

            var receiver = inv.TargetMethod.ReceiverType;
            if (receiver == null)
            {
                return false;
            }

            if (inv.Instance is not ILocalReferenceOperation l)
            {
                return false;
            }

            taskListVariable = l.Local;
            return receiver.AllInterfaces.Any(i => i.FullName() == "System.Collections.Generic.IList")
                   && inv.TargetMethod.Name is "Add" or "Insert";
        }

        bool IsAwaitingForList(IOperation o)
        {
            return (o is IAwaitOperation { Operation: IInvocationOperation i }
                    && s_targetTypes.Contains(i.TargetMethod.ReturnType.FullName())
                    && i.TargetMethod.Name == "WhenAll")
                   || (o is IAwaitOperation
                       {
                           Operation: ILocalReferenceOperation { Local.Type: INamedTypeSymbol named }
                       }
                       && IsShorthandForUniTask(named));
        }

        bool IsShorthandForUniTask(INamedTypeSymbol type)
        {
            // Refer to the following for the omitted notation in UniTask.
            // https://github.com/Cysharp/UniTask/blob/master/src/UniTask/Assets/Plugins/UniTask/Runtime/UniTaskExtensions.Shorthand.cs
            return type.AllInterfaces.Any(i => i.FullName() == "System.Collections.Generic.IEnumerable")
                   && type.TypeArguments.Length == 1
                   && type.TypeArguments.First().FullName() == "Cysharp.Threading.Tasks.UniTask";
        }
    }

    private static bool IsTaskListType(ISymbol symbol)
    {
        var typeSymbol = symbol switch
        {
            ILocalSymbol l => l.Type,
            IFieldSymbol f => f.Type,
            IParameterSymbol p => p.Type,
            _ => null
        };

        if (typeSymbol is INamedTypeSymbol named)
        {
            return named.AllInterfaces.Any(i => i.FullName() == "System.Collections.Generic.IList")
                   && named.TypeArguments.Length == 1
                   && s_targetTypes.Contains(named.TypeArguments.First().FullName());
        }

        return false;
    }
    
    private static bool TryGetLocalReference(IOperation root, ISymbol symbol, out ILocalReferenceOperation local)
    {
        var next = new Func<IOperation, IEnumerable<IOperation>>(o => o.Children);
        var condition = new Func<IOperation, bool>(o => o is ILocalReferenceOperation l && l.Local.Name == symbol.Name);
        var results = DataFlowAnalysis.FindRecursive(root, next, condition).ToArray();
        if (results.Any())
        {
            local = (ILocalReferenceOperation)results.First();
            return true;
        }

        local = default;
        return false;
    }

    private static bool IsTarget(IInvocationOperation invocation)
    {
        return s_targetTypes.Contains(invocation.TargetMethod.ReturnType.FullName())
               && invocation.TargetMethod.Name != "FromResult";
    }

    private static bool IsTarget(IFieldReferenceOperation field)
    {
        return s_targetTypes.Contains(field.Field.Type.FullName())
               && field.Field.Name != "CompletedTask";
    }

    private static bool IsTarget(IPropertyReferenceOperation property)
    {
        return s_targetTypes.Contains(property.Property.Type.FullName())
               && property.Property.Name != "CompletedTask";
    }

    private static bool IsAwait(SyntaxNode? syntaxNode)
    {
        while (true)
        {
            switch (syntaxNode)
            {
                case ExpressionStatementSyntax or null:
                    return false;
                case AwaitExpressionSyntax:
                    return true;
                default:
                    syntaxNode = syntaxNode.Parent;
                    break;
            }
        }
    }
}
