using HotChocolate.Execution;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace ItemAuthoring.Api;

/// <summary>
/// Writes the OpenAPI document and the GraphQL schema to disk and exits.
/// </summary>
/// <remarks>
/// The Angular client generates its typed clients from these two artefacts. Reading them from files
/// rather than from a running server is what makes <c>npm run codegen</c> — and therefore the client
/// build — reproducible offline and in continuous integration.
/// </remarks>
public static class ApiContractExporter
{
    /// <summary>The command line switch that requests an export.</summary>
    public const string Switch = "--export-contracts";

    /// <summary>Determines whether the process was started to export the contracts.</summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The output directory when an export was requested, otherwise <see langword="null"/>.</returns>
    public static string? GetOutputDirectory(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var index = Array.IndexOf(args, Switch);
        if (index < 0)
        {
            return null;
        }

        return index + 1 < args.Length ? args[index + 1] : "artifacts";
    }

    /// <summary>Writes both contracts and returns.</summary>
    /// <param name="app">The built application.</param>
    /// <param name="outputDirectory">The directory the artefacts are written to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ExportAsync(WebApplication app, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(app);

        Directory.CreateDirectory(outputDirectory);

        var swagger = app.Services.GetRequiredService<ISwaggerProvider>();
        var document = swagger.GetSwagger("v1");
        var openApiPath = Path.Combine(outputDirectory, "openapi.json");
        await using (var stream = File.Create(openApiPath))
        {
            await using var writer = new StreamWriter(stream);
            document.SerializeAsV3(new OpenApiJsonWriter(writer));
            await writer.FlushAsync();
        }

        var executorProvider = app.Services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync();
        var schemaPath = Path.Combine(outputDirectory, "schema.graphql");
        await File.WriteAllTextAsync(schemaPath, executor.Schema.ToString());

        app.Logger.LogInformation(
            "Exported the API contracts to {OpenApiPath} and {SchemaPath}.",
            openApiPath,
            schemaPath);
    }
}
