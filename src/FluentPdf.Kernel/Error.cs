namespace FluentPdf.Kernel;

/// <summary>
/// An immutable, structured representation of a failure. Business operations return an
/// <see cref="Error"/> through <see cref="Result"/> rather than throwing exceptions.
/// </summary>
/// <param name="Code">A stable, machine-readable identifier (e.g. <c>"Document.Empty"</c>).</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>The absence of an error, used by successful results.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>A canonical error describing an unexpected <see langword="null"/> value.</summary>
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    /// <summary>Creates a validation error.</summary>
    public static Error Validation(string code, string message) => new(code, message);
}
