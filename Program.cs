var builder = WebApplication.CreateBuilder(args);

// Registra i servizi
builder.Services.AddSingleton<ChatService>();
builder.Services.AddRazorPages();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

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