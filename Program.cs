using CodebaseAI.Models;

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

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// Configure HttpClient
builder.Services.AddHttpClient("AzureOpenAI", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
    client.DefaultRequestHeaders.Add("api-key", options.ApiKey);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();

// Reindirizza la root ("/") alla pagina di chat ("/Chat")
app.MapGet("/", async context =>
{
    context.Response.Redirect("/Chat");
    await Task.CompletedTask;
});
app.MapRazorPages();
app.Run();
