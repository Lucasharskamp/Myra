using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    ///     Reserved to be used by the compiler for tracking metadata.
    ///     This class should not be used by developers in source code.
    /// </summary>
    [ExcludeFromCodeCoverage, DebuggerNonUserCode]
    internal static class IsExternalInit
    {
    }
}
#endif