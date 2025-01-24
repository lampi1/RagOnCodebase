using CodebaseAI.Models;
using CodebaseAI.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Configure localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[] { "en", "it" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

// Configure options
builder.Services.AddOptions<AzureOpenAIOptions>()
    .Bind(builder.Configuration.GetSection("AzureOpenAI"))
    .ValidateDataAnnotations();

builder.Services.AddOptions<ElasticsearchOptions>()
    .Bind(builder.Configuration.GetSection("Elasticsearch"))
    .ValidateDataAnnotations();

// Configure HttpClient
builder.Services.AddHttpClient("AzureOpenAI", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
    client.DefaultRequestHeaders.Add("api-key", options.ApiKey);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseRequestLocalization();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/", async context =>
{
    context.Response.Redirect("/Chat");
    await Task.CompletedTask;
});

app.MapRazorPages();
app.Run();