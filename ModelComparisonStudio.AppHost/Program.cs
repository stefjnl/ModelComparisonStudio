var builder = DistributedApplication.CreateBuilder(args);

// Add the ModelComparisonStudio project
var modelComparison = builder.AddProject<Projects.ModelComparisonStudio>("modelcomparisonstudio");

builder.Build().Run();
