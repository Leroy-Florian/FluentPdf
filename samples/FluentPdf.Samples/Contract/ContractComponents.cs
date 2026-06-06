using System.Globalization;
using FluentPdf.Application.Building;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.Contract;

/// <summary>Shared text styles for the agreement.</summary>
internal static class ContractStyles
{
    public static TextStyle Title => TextStyle.Default.WithFontSize(22d).WithBold();

    public static TextStyle ArticleHeading => TextStyle.Default.WithFontSize(13d).WithBold();

    public static TextStyle Recital => TextStyle.Default.WithFontSize(11d);

    public static TextStyle Muted =>
        TextStyle.Default.WithFontSize(9d).WithColor(Color.FromHex("#666666").Value);
}

/// <summary>
/// Renders one numbered article: a bold heading followed by its clauses, each numbered
/// "N.M". The clause paragraphs are long, so the paginator splits them across pages
/// automatically. Reusable for any article in any agreement.
/// </summary>
public sealed class ArticleComponent : IBlockComponent<NumberedArticle>
{
    public Result<IReadOnlyList<IBlock>> Build(NumberedArticle model) =>
        BlockComposer.Compose(blocks =>
        {
            blocks
                .Paragraph(p => p.Run(
                    $"Article {model.Number}. {model.Article.Title}",
                    ContractStyles.ArticleHeading))
                .Spacer(6d);

            for (var i = 0; i < model.Article.Clauses.Count; i++)
            {
                var number = $"{model.Number}.{i + 1}";
                blocks
                    .Paragraph(p => p
                        .Bold($"{number}  ")
                        .Text(model.Article.Clauses[i]))
                    .Spacer(6d);
            }

            blocks.Spacer(10d);
        });
}

/// <summary>The cover block: title, reference, parties and recitals. Reusable for any agreement.</summary>
public sealed class ContractCoverComponent : IBlockComponent<ContractDto>
{
    public Result<IReadOnlyList<IBlock>> Build(ContractDto contract) =>
        BlockComposer.Compose(blocks =>
        {
            blocks
                .Spacer(120d)
                .Paragraph(p => p.Run(contract.Title, ContractStyles.Title))
                .Spacer(8d)
                .Paragraph(p => p.Run(contract.Reference, ContractStyles.Muted))
                .Paragraph(p => p.Run(
                    $"Dated {contract.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}",
                    ContractStyles.Muted))
                .Spacer(28d)
                .Paragraph(p => p.Bold("Between"))
                .Paragraph($"{contract.Lender.Name} (the \"{contract.Lender.Role}\")")
                .Paragraph(contract.Lender.Address)
                .Spacer(8d)
                .Paragraph(p => p.Bold("and"))
                .Paragraph($"{contract.Borrower.Name} (the \"{contract.Borrower.Role}\")")
                .Paragraph(contract.Borrower.Address)
                .Spacer(28d)
                .Paragraph(p => p.Bold("Recitals"))
                .Spacer(4d);

            var index = 0;
            foreach (var recital in contract.Recitals)
            {
                index++;
                blocks
                    .Paragraph(p => p
                        .Bold($"({Letter(index)})  ")
                        .Text(recital))
                    .Spacer(6d);
            }
        });

    private static char Letter(int oneBased) => (char)('A' + oneBased - 1);
}

/// <summary>The execution / signature block. Reusable for any agreement.</summary>
public sealed class SignatureBlockComponent : IBlockComponent<IReadOnlyList<Party>>
{
    public Result<IReadOnlyList<IBlock>> Build(IReadOnlyList<Party> signatories) =>
        BlockComposer.Compose(blocks =>
        {
            blocks
                .Spacer(16d)
                .Paragraph(p => p.Run("Execution", ContractStyles.ArticleHeading))
                .Spacer(6d)
                .Paragraph("IN WITNESS WHEREOF the parties have executed this Agreement as a deed "
                    + "and have caused it to be delivered on the date stated at the beginning of "
                    + "this Agreement.")
                .Spacer(16d);

            foreach (var party in signatories)
            {
                blocks
                    .Paragraph(p => p.Bold(party.Role))
                    .Paragraph(party.Name)
                    .Spacer(6d)
                    .Paragraph("Signature: ______________________________")
                    .Paragraph("Name:")
                    .Paragraph("Title:")
                    .Spacer(18d);
            }
        });
}
