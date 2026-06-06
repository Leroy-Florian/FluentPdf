using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>Internal helpers for collapsing a sequence of results into a single result.</summary>
internal static class ResultList
{
    /// <summary>
    /// Returns the materialised list when every result succeeds, or the first error
    /// encountered. This is how the fluent builders surface a single, deterministic failure
    /// instead of throwing mid-chain.
    /// </summary>
    public static Result<IReadOnlyList<T>> Collect<T>(IEnumerable<Result<T>> results)
    {
        var list = new List<T>();

        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result.Error;
            }

            list.Add(result.Value);
        }

        IReadOnlyList<T> readOnly = list;
        return Result.Success(readOnly);
    }

    /// <summary>Wraps a typed block result as an untyped block result.</summary>
    public static Result<IBlock> AsBlock<TBlock>(this Result<TBlock> result)
        where TBlock : IBlock =>
        result.IsSuccess
            ? Result.Success<IBlock>(result.Value)
            : Result.Failure<IBlock>(result.Error);
}
