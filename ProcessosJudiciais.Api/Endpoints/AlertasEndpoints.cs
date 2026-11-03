using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Data;
using ProcessosJudiciais.Api.Jobs;

namespace ProcessosJudiciais.Api.Endpoints;

public static class AlertasEndpoints
{
    public static void MapAlertasEndpoints(this WebApplication app)
    {
        app.MapGet("/alertas", async (bool? apenasAbertos, ApplicationDbContext db) =>
        {
            var consulta = db.Alertas.AsQueryable();

            if (apenasAbertos ?? true)
            {
                consulta = consulta.Where(a => a.ResolvidoEm == null);
            }

            var alertas = await consulta
                .OrderByDescending(a => a.DiasSemMovimentacao)
                .Select(a => new
                {
                    a.Id,
                    a.ProcessoId,
                    NumeroProcesso = a.Processo!.Numero,
                    a.DiasSemMovimentacao,
                    a.CriadoEm,
                    a.ResolvidoEm
                })
                .ToListAsync();

            return Results.Ok(alertas);
        });

        // Executa a mesma verificação do job diário, sob demanda
        app.MapPost("/alertas/verificar", async (VerificadorDeProcessosParados verificador, CancellationToken cancellationToken) =>
        {
            var criados = await verificador.ExecutarAsync(cancellationToken);
            return Results.Ok(new { alertasCriados = criados });
        });
    }
}
