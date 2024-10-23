using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace AnalyzerExtensions
{
    public static class INamespaceOrTypeSymbolExtensions
    {
        /// <summary>
        /// Recursively concatenate ContainingNamespace.Name to return the full name.
        /// </summary>
        /// <remarks>In Roslyn, fully qualified names are concatenated with "." (even for nested types).</remarks>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FullName(this INamespaceOrTypeSymbol symbol)
        {
            var collectedNamespace = new List<string>();
            while (symbol.Name != "")
            {
                collectedNamespace.Add(symbol.Name);
                symbol = symbol.ContainingNamespace;
            }

            collectedNamespace.Reverse();
            return string.Join(".", collectedNamespace);
        }
    }
}
