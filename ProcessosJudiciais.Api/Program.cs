using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Data;
using ProcessosJudiciais.Api.Endpoints;
using ProcessosJudiciais.Api.Jobs;
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

// Rotina de processos parados: o serviço fica sempre disponível (também é usado pelo endpoint
// POST /alertas/verificar); o agendamento diário pelo Hangfire pode ser desligado via "Jobs:Habilitados".
builder.Services.Configure<AlertasOptions>(builder.Configuration.GetSection("Alertas"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<VerificadorDeProcessosParados>();

var jobsHabilitados = builder.Configuration.GetValue("Jobs:Habilitados", true);
if (jobsHabilitados)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            // Usa o mesmo driver do EF Core (Microsoft.Data.SqlClient) de forma explícita
            SqlClientFactory = Microsoft.Data.SqlClient.SqlClientFactory.Instance
        }));
    builder.Services.AddHangfireServer();
}

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

if (jobsHabilitados)
{
    var alertas = app.Configuration.GetSection("Alertas").Get<AlertasOptions>() ?? new AlertasOptions();

    if (!TimeZoneInfo.TryFindSystemTimeZoneById(alertas.FusoHorario, out var fusoHorario))
    {
        app.Logger.LogWarning("Fuso horário '{Fuso}' não encontrado; o job será agendado em UTC.", alertas.FusoHorario);
        fusoHorario = TimeZoneInfo.Utc;
    }

    app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<VerificadorDeProcessosParados>(
        "verificar-processos-parados",
        verificador => verificador.ExecutarAsync(CancellationToken.None),
        alertas.Cron,
        new RecurringJobOptions { TimeZone = fusoHorario });

    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new DashboardLiberadoEmDesenvolvimento()]
        });
    }
}

app.MapProcessosEndpoints();
app.MapMovimentacoesEndpoints();
app.MapAlertasEndpoints();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory)
public partial class Program { }
