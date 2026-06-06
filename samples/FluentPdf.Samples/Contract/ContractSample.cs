using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.Contract;

/// <summary>
/// End-to-end usage of the long-form contract: build the DTO, render it through the template
/// and an adapter. The in-memory adapter paginates automatically (resolving the "Page X of Y"
/// footer) and streams pages, so a 30-40 page agreement renders with flat memory.
/// </summary>
public static class ContractSample
{
    /// <summary>Renders the sample agreement with the given adapter.</summary>
    public static Result<RenderedPdf> Render(IPdfRenderer renderer)
    {
        var document = new ContractTemplate().Build(ContractText.Sample());

        if (document.IsFailure)
        {
            return document.Error;
        }

        return new RenderDocumentUseCase(renderer).Execute(document.Value);
    }

    /// <summary>Renders the sample agreement with the reference in-memory adapter.</summary>
    public static Result<RenderedPdf> RenderWithInMemoryAdapter() =>
        Render(new InMemoryPdfRenderer());
}
