using System.Text.Json.Serialization;
using Asp.Versioning.ApiExplorer;
using ItemAuthoring.Api;
using ItemAuthoring.Api.Common;
using ItemAuthoring.Api.Diagnostics;
using ItemAuthoring.Api.GraphQL;
using ItemAuthoring.Api.Rest;
using ItemAuthoring.Api.Security;
using ItemAuthoring.Application;
using ItemAuthoring.Infrastructure;
using ItemAuthoring.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationAuthentication();
builder.Services.AddApplicationRateLimiting();
builder.Services.AddOpenApiDocumentation();
builder.Services.AddGraphQlApi(builder.Environment);

builder.Services.AddSingleton<RequestMetricsStore>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddCors(options => options.AddPolicy(
    ApiCorsPolicies.Client,
    policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, "ETag")
        .AllowCredentials()));

var app = builder.Build();

// Exporting the API contracts is a build-time task, not a request-time one: the process writes the
// two artefacts the Angular client generates from and exits without opening a port.
if (ApiContractExporter.GetOutputDirectory(args) is { } contractDirectory)
{
    await ApiContractExporter.ExportAsync(app, contractDirectory);
    return;
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(ApiCorsPolicies.Client);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting(RateLimitingPolicies.Standard);
app.MapGraphQL();
app.MapHealthChecks("/health").AllowAnonymous();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment(ApiEnvironments.Benchmark))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"Item Authoring API {description.GroupName}");
        }
    });
}

await app.SeedDatabaseAsync();
await app.RunAsync();

/// <summary>
/// The composition root of the API host.
/// </summary>
/// <remarks>
/// The type is made visible so the integration test project can drive the real pipeline through
/// <c>WebApplicationFactory</c> rather than a reconstructed approximation of it.
/// </remarks>
public partial class Program;
