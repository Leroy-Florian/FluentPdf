using FluentPdf.Domain;

namespace FluentPdf.Application.Building;

/// <summary>Fluent builder for <see cref="DocumentMetadata"/>.</summary>
public sealed class MetadataBuilder
{
    private readonly List<string> _keywords = [];
    private string? _title;
    private string? _author;
    private string? _subject;
    private string? _creator;

    /// <summary>Sets the document title.</summary>
    public MetadataBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Sets the document author.</summary>
    public MetadataBuilder Author(string author)
    {
        _author = author;
        return this;
    }

    /// <summary>Sets the document subject.</summary>
    public MetadataBuilder Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    /// <summary>Sets the creating application.</summary>
    public MetadataBuilder Creator(string creator)
    {
        _creator = creator;
        return this;
    }

    /// <summary>Adds one or more keywords.</summary>
    public MetadataBuilder Keywords(params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                _keywords.Add(keyword);
            }
        }

        return this;
    }

    internal DocumentMetadata Build() => new()
    {
        Title = _title,
        Author = _author,
        Subject = _subject,
        Creator = _creator,
        Keywords = [.. _keywords],
    };
}
