using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Models;

namespace ProcessosJudiciais.Api.Tests;

public sealed class AlertasEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public AlertasEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private record ResultadoVerificacao(int AlertasCriados);

    private record AlertaResposta(int Id, int ProcessoId, string NumeroProcesso, int DiasSemMovimentacao, DateTime? ResolvidoEm);

    private async Task<Processo> AdicionarProcessoParadoAsync(int diasParado)
    {
        await using var db = _factory.CriarDbContext();
        var processo = new Processo
        {
            Numero = Dados.NovoNumeroCnj(),
            NomeAutor = "Maria da Silva",
            NomeReu = "Empresa Exemplo Ltda",
            DataCadastro = DateTime.UtcNow.AddDays(-diasParado)
        };
        db.Processos.Add(processo);
        await db.SaveChangesAsync();
        return processo;
    }

    [Fact]
    public async Task PostVerificar_CriaAlertasEGetListaOsAbertos()
    {
        var parado = await AdicionarProcessoParadoAsync(diasParado: 45);
        await AdicionarProcessoParadoAsync(diasParado: 2);

        var resposta = await _client.PostAsync("/alertas/verificar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<ResultadoVerificacao>();
        Assert.Equal(1, resultado!.AlertasCriados);

        var alertas = await _client.GetFromJsonAsync<List<AlertaResposta>>("/alertas");
        var alerta = Assert.Single(alertas!);
        Assert.Equal(parado.Id, alerta.ProcessoId);
        Assert.Equal(parado.Numero, alerta.NumeroProcesso);
        Assert.Equal(45, alerta.DiasSemMovimentacao);
    }

    [Fact]
    public async Task NovaMovimentacao_ResolveOAlertaAbertoDoProcesso()
    {
        var parado = await AdicionarProcessoParadoAsync(diasParado: 60);
        await _client.PostAsync("/alertas/verificar", null);

        var resposta = await _client.PostAsJsonAsync($"/processos/{parado.Id}/movimentacoes",
            new { textoMovimentacao = "Juntada de petição" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<List<AlertaResposta>>("/alertas"))!);

        var todos = await _client.GetFromJsonAsync<List<AlertaResposta>>("/alertas?apenasAbertos=false");
        Assert.NotNull(Assert.Single(todos!).ResolvidoEm);

        await using var db = _factory.CriarDbContext();
        Assert.False(await db.Alertas.AnyAsync(a => a.ResolvidoEm == null));
    }
}
