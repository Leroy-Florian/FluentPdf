using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// Builds a free-standing list of blocks with the full fluent surface, outside of any
/// section. This is the ergonomic way to author a reusable <see cref="IBlockComponent"/>:
/// the component delegates to <see cref="Compose"/> instead of constructing domain objects
/// by hand.
/// </summary>
public sealed class BlockComposer : BlockContainerBuilder<BlockComposer>
{
    private BlockComposer()
    {
    }

    /// <summary>Composes blocks fluently and returns them (or the first error).</summary>
    public static Result<IReadOnlyList<IBlock>> Compose(Action<BlockComposer> configure)
    {
        var composer = new BlockComposer();
        configure(composer);
        return composer.BuildBlocks();
    }
}
