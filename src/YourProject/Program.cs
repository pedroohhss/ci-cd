// -----------------------------------------------------------------------
// VERSION INTEGRATION SNIPPET — copy/merge the relevant sections into
// your existing Program.cs. Do not replace your full Program.cs with this.
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.OpenApi.Models;
using YourProject.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Load version from version.json ---
var versionJson = File.ReadAllText("version.json");
var version = JsonSerializer.Deserialize<VersionModel>(versionJson)
    ?? new VersionModel();

builder.Services.AddSingleton(version);
// --------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Inject version into Swagger ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Your API",
        Version = version.Version
    });
});
// -----------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => "Hello, World!");

app.Run();
