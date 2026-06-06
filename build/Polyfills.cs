// Polyfills for language features that require BCL types missing from older target
// frameworks (netstandard2.0). This file is linked into every packable project so the
// compiler can lower `init` accessors and `record` types. It is intentionally empty on
// modern targets where the runtime already provides these types.
#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved for use by the compiler to support <c>init</c>-only setters and records
    /// on target frameworks that predate .NET 5.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}

#endif
