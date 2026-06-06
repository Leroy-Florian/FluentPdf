namespace FluentPdf.Samples.Contract;

/// <summary>The print DTO for a long-form facility agreement.</summary>
public sealed record ContractDto(
    string Title,
    string Reference,
    DateOnly Date,
    string Currency,
    decimal FacilityAmount,
    Party Lender,
    Party Borrower,
    IReadOnlyList<string> Recitals,
    IReadOnlyList<ContractArticle> Articles,
    IReadOnlyList<Party> Signatories);

/// <summary>A party to the agreement.</summary>
public sealed record Party(string Name, string Role, string Address);

/// <summary>A numbered article: a title and its ordered clauses.</summary>
public sealed record ContractArticle(string Title, IReadOnlyList<string> Clauses);

/// <summary>An article paired with its 1-based number, the model an article block renders.</summary>
public sealed record NumberedArticle(int Number, ContractArticle Article);
