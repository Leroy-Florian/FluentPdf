using BenchmarkDotNet.Running;
using FluentPdf.Benchmarks;

// Pass benchmark filters on the command line, e.g.:
//   dotnet run -c Release --project benchmarks/FluentPdf.Benchmarks -- --filter *Invoice*
// or run them all by passing --filter *.
BenchmarkSwitcher.FromAssembly(typeof(InvoiceBenchmarks).Assembly).Run(args);
