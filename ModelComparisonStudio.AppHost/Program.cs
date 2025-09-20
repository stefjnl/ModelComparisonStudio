var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ModelComparisonStudio>("modelcomparisonstudio");

builder.Build().Run();
