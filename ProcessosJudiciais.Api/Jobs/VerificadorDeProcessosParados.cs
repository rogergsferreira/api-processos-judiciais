using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcessosJudiciais.Api.Data;
using ProcessosJudiciais.Api.Models;

namespace ProcessosJudiciais.Api.Jobs;

/// <summary>
/// Procura processos sem movimentação há mais dias do que o configurado e abre um alerta para cada um.
/// Executado diariamente pelo Hangfire e também sob demanda pelo endpoint POST /alertas/verificar.
/// </summary>
public class VerificadorDeProcessosParados(
    ApplicationDbContext db,
    TimeProvider tempo,
    IOptions<AlertasOptions> opcoes,
    ILogger<VerificadorDeProcessosParados> logger)
{
    public async Task<int> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var agora = tempo.GetUtcNow().UtcDateTime;
        var limite = agora.AddDays(-opcoes.Value.DiasSemMovimentacao);

        // A última atividade é a movimentação mais recente; sem movimentações, vale a data de cadastro.
        // Processos que já têm alerta aberto ficam de fora, para não duplicar alertas a cada execução.
        var parados = await db.Processos
            .Where(p => !db.Alertas.Any(a => a.ProcessoId == p.Id && a.ResolvidoEm == null))
            .Select(p => new
            {
                p.Id,
                UltimaAtividade = p.Movimentacoes.Max(m => (DateTime?)m.DataMovimentacao) ?? p.DataCadastro
            })
            .Where(p => p.UltimaAtividade < limite)
            .ToListAsync(cancellationToken);

        foreach (var processo in parados)
        {
            db.Alertas.Add(new Alerta
            {
                ProcessoId = processo.Id,
                DiasSemMovimentacao = (int)(agora - processo.UltimaAtividade).TotalDays,
                CriadoEm = agora
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Verificação de processos parados concluída: {Quantidade} alerta(s) criado(s) para processos sem movimentação há mais de {Dias} dias.",
            parados.Count, opcoes.Value.DiasSemMovimentacao);

        return parados.Count;
    }
}
