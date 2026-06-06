using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// A reusable, self-contained fragment of document content. Define a component once (e.g.
/// an invoice header or a signature block) and drop it into any document or container via
/// <see cref="BlockContainerBuilder{TSelf}.Component(IBlockComponent)"/>.
/// </summary>
public interface IBlockComponent
{
    /// <summary>Produces the blocks that make up this component.</summary>
    Result<IReadOnlyList<IBlock>> Build();
}

/// <summary>
/// A reusable fragment of content rendered from a print DTO. This is the bridge between an
/// application's data and the document model: the builder receives the DTO and the
/// component maps it to blocks.
/// </summary>
/// <typeparam name="TModel">The DTO the component renders.</typeparam>
public interface IBlockComponent<in TModel>
{
    /// <summary>Produces the blocks that represent <paramref name="model"/>.</summary>
    Result<IReadOnlyList<IBlock>> Build(TModel model);
}
