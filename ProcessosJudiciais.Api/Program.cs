using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Data;
using ProcessosJudiciais.Api.Endpoints;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Executa as DataAnnotations dos DTOs (ex.: regex do número CNJ) e devolve 400 com os erros
builder.Services.AddValidation();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API de Processos Judiciais v1");
    });
}

// Cria o banco automaticamente quando habilitado (usado no docker-compose)
if (app.Configuration.GetValue<bool>("Database:EnsureCreated"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.MapProcessosEndpoints();
app.MapMovimentacoesEndpoints();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory)
public partial class Program { }
