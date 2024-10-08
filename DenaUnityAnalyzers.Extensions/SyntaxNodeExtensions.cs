// (c) DeNA Co., Ltd.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace DenaUnityAnalyzers.Extensions
{
    public static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Return a Diagnostic created from the SyntaxNode.
        /// </summary>
        /// <param name="syntaxNode"></param>
        /// <param name="rule"></param>
        /// <param name="messageArgs"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Diagnostic CreateDiagnostic(this SyntaxNode syntaxNode, DiagnosticDescriptor rule,
            params object[]? messageArgs)
        {
            return Diagnostic.Create(
                rule,
                syntaxNode.GetLocation(),
                messageArgs ?? Array.Empty<object>()
            );
        }
    }
}
