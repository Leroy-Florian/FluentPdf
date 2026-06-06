using FluentPdf.Kernel;

namespace FluentPdf.Application;

/// <summary>
/// An application use case that maps an input to a <see cref="Result{T}"/> without throwing
/// for business failures.
/// </summary>
/// <typeparam name="TInput">The input the use case operates on.</typeparam>
/// <typeparam name="TResult">The produced value on success.</typeparam>
public interface IUseCase<in TInput, TResult>
{
    /// <summary>Executes the use case.</summary>
    Result<TResult> Execute(TInput input);
}
