using System;
using System.Runtime.CompilerServices;

namespace DenaUnityAnalyzers.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Remove namespace aliases (e.g., global::) from the fully qualified name of an ISymbol.
        /// </summary>
        /// <param name="symbolFullname"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ExcludeNamespaceAlias(this string symbolFullname)
        {
            var aliasIndex = symbolFullname.IndexOf("::", StringComparison.Ordinal);
            if (aliasIndex < 0)
            {
                return symbolFullname;
            }

            return symbolFullname.Substring(aliasIndex + 2);
        }
    }
}
