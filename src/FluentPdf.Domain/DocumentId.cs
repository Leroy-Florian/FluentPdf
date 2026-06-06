using FluentPdf.Kernel;

namespace FluentPdf.Domain;

/// <summary>A strongly-typed identifier for a <see cref="PdfDocument"/>.</summary>
public sealed class DocumentId : ValueObject
{
    private DocumentId(Guid value) => Value = value;

    public Guid Value { get; }

    /// <summary>Generates a new, unique identifier.</summary>
    public static DocumentId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing, non-empty <see cref="Guid"/>.</summary>
    public static Result<DocumentId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Error.NullValue;
        }

        return new DocumentId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
