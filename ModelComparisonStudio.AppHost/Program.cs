var builder = DistributedApplication.CreateBuilder(args);

var modelComparison = builder.AddProject<Projects.ModelComparisonStudio>("modelcomparisonstudio");

builder.Build().Run();
