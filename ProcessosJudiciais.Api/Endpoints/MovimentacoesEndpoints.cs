using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Data;
using ProcessosJudiciais.Api.Dtos;
using ProcessosJudiciais.Api.Models;

namespace ProcessosJudiciais.Api.Endpoints;

public static class MovimentacoesEndpoints
{
    public static void MapMovimentacoesEndpoints(this WebApplication app)
    {
        app.MapGet("/processos/{processoId}/movimentacoes", async (int processoId, ApplicationDbContext db) =>
        {
            var processoExiste = await db.Processos.AnyAsync(p => p.Id == processoId);

            if (!processoExiste)
            {
                return Results.NotFound(new { mensagem = "Processo não encontrado." });
            }
            var movimentacoes = await db.Movimentacoes
                .Where(m => m.ProcessoId == processoId)
                .ToListAsync();

            return Results.Ok(movimentacoes);
        });
        
        app.MapPost("/processos/{processoId}/movimentacoes", async (int processoId, MovimentacaoDto dto, ApplicationDbContext db) =>
        {
            var processoExiste = await db.Processos.AnyAsync(p => p.Id == processoId);
            if (!processoExiste)
                return Results.NotFound(new { mensagem = "Processo não encontrado." });

            var novaMovimentacao = new Movimentacao
            {
                ProcessoId = processoId,
                TextoMovimentacao = dto.TextoMovimentacao,
                DataMovimentacao = DateTime.UtcNow
            };

            db.Movimentacoes.Add(novaMovimentacao);
            await db.SaveChangesAsync();

            return Results.Created($"/processos/{processoId}/movimentacoes/{novaMovimentacao.Id}", new
            {
                novaMovimentacao.Id,
                novaMovimentacao.ProcessoId,
                novaMovimentacao.TextoMovimentacao,
                novaMovimentacao.DataMovimentacao
            });
        });
    }
}