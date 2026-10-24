using BenchmarkDotNet.Running;
using FanWebsiteAPI.Benchmarks;

// Run with: dotnet run -c Release --project FanWebsiteAPI.Benchmarks -- --filter "*"
BenchmarkSwitcher.FromAssembly(typeof(GetReportsBenchmarks).Assembly).Run(args);
