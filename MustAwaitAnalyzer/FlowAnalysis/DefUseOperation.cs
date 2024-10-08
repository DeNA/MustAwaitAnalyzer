using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace FlowAnalysis;

public class DefUseOperation
{
    public IOperation Op { get; }
    public IList<ISymbol> Def { get; }
    public IList<ISymbol> Use { get; }

    public DefUseOperation(IOperation op, IList<ISymbol> def, IList<ISymbol> use)
    {
        Op = op;
        Def = def;
        Use = use;
    }
}
