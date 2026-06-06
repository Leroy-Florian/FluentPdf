using FluentPdf.Domain;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// A reusable, named recipe that turns a print DTO into a complete
/// <see cref="PdfDocument"/>. A template composes reusable
/// <see cref="IBlockComponent{TModel}"/>s through the fluent builder, so the same layout
/// can be rendered for any number of inputs (e.g. one invoice template, many invoices).
/// </summary>
/// <typeparam name="TModel">The print DTO the template renders.</typeparam>
public interface IDocumentTemplate<in TModel>
{
    /// <summary>Builds the document for <paramref name="model"/>.</summary>
    Result<PdfDocument> Build(TModel model);
}
