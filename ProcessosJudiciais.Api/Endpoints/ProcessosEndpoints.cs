using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Data;
using ProcessosJudiciais.Api.Dtos;
using ProcessosJudiciais.Api.Models;

namespace ProcessosJudiciais.Api.Endpoints;

public static class ProcessosEndpoints
{
    public static void MapProcessosEndpoints(this WebApplication app)
    {
        app.MapGet("/processos", async (ApplicationDbContext db) =>
            await db.Processos.ToListAsync());

        app.MapGet("/processos/busca/{numero}", async (string numero, ApplicationDbContext db) =>
        {
            // Decodificação da URL caso venha com caracteres especiais:
            var numeroDecodificado = System.Net.WebUtility.UrlDecode(numero);

            var processo = await db.Processos
                .Include(p => p.Movimentacoes)
                .FirstOrDefaultAsync(p => p.Numero == numeroDecodificado);

            return processo is not null ? Results.Ok(processo) : Results.NotFound("Processo não encontrado.");
        });

        app.MapGet("/processos/{id}", async (int id, ApplicationDbContext db) =>
            await db.Processos
                .Include(p => p.Movimentacoes)
                .FirstOrDefaultAsync(p => p.Id == id)
                is Processo processo
                    ? Results.Ok(processo)
                    : Results.NotFound());

        app.MapPost("/processos", async (ProcessoDto dto, ApplicationDbContext db) =>
        {
            var existeProcesso = await db.Processos.AnyAsync(p => p.Numero == dto.Numero);

            if (existeProcesso)
            {
                return Results.Conflict(new { mensagem = "Já existe um processo cadastrado com este número." });
            }

            var novoProcesso = new Processo
            {
                Numero = dto.Numero,
                NomeAutor = dto.NomeAutor,
                NomeReu = dto.NomeReu,
                DataCadastro = DateTime.UtcNow
            };

            db.Processos.Add(novoProcesso);
            await db.SaveChangesAsync();

            return Results.Created($"/processos/{novoProcesso.Id}", novoProcesso);
        });

        app.MapPut("/processos/{id}", async (int id, ProcessoDto dto, ApplicationDbContext db) =>
        {
            var processo = await db.Processos.FindAsync(id);
            if (processo is null) return Results.NotFound(new { mensagem = "Processo não encontrado." });

            var numeroEmUso = await db.Processos
                .AnyAsync(p => p.Numero == dto.Numero && p.Id != id);

            if (numeroEmUso)
            {
                return Results.Conflict(new { mensagem = "Já existe outro processo cadastrado com este número." });
            }

            processo.Numero = dto.Numero;
            processo.NomeAutor = dto.NomeAutor;
            processo.NomeReu = dto.NomeReu;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/processos/{id}", async (int id, ApplicationDbContext db) =>
        {
            var processo = await db.Processos.FindAsync(id);
            if (processo is null) return Results.NotFound(new { mensagem = "Processo não encontrado." });

            db.Processos.Remove(processo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
