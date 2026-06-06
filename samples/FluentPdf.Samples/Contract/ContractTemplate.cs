using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Layout;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.Contract;

/// <summary>
/// Assembles a complete, multi-page facility agreement: a cover section followed by the body
/// (every article in order) and the execution block. The running footer carries a
/// "Page {page} of {pages}" field that the paginator resolves automatically, so the document
/// never hard-codes a page count — it is whatever the content flows to.
/// </summary>
public sealed class ContractTemplate : IDocumentTemplate<ContractDto>
{
    private readonly ContractCoverComponent _cover = new();
    private readonly ArticleComponent _article = new();
    private readonly SignatureBlockComponent _signature = new();

    public Result<PdfDocument> Build(ContractDto contract) =>
        PdfDocumentBuilder.Create()
            .Metadata(meta => meta
                .Title(contract.Title)
                .Subject(contract.Reference)
                .Author(contract.Lender.Name)
                .Creator("FluentPdf")
                .Keywords("facility agreement", contract.Reference))

            // Cover page.
            .Section(cover => cover
                .PageSize(PageSize.A4)
                .Margins(72d)
                .Component(_cover, contract))

            // Body: articles flow and paginate automatically; the footer numbers every page.
            .Section(body =>
            {
                body
                    .PageSize(PageSize.A4)
                    .Margins(64d)
                    .Header(header => header.Paragraph(p => p.Run(
                        $"{contract.Reference} — {contract.Title}",
                        ContractStyles.Muted)))
                    .Footer(footer => footer.PageNumber(
                        "Page {page} of {pages}",
                        HorizontalAlignment.Center));

                for (var i = 0; i < contract.Articles.Count; i++)
                {
                    body.Component(_article, new NumberedArticle(i + 1, contract.Articles[i]));
                }

                body.Component(_signature, contract.Signatories);
            })

            .Build();
}
