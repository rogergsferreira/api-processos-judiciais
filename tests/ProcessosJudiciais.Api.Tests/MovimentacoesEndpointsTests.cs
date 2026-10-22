using System.Net;
using System.Net.Http.Json;

namespace ProcessosJudiciais.Api.Tests;

public sealed class MovimentacoesEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public MovimentacoesEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private async Task<ProcessoResposta> CriarProcessoAsync()
    {
        var resposta = await _client.PostAsJsonAsync("/processos", Dados.NovoProcesso());
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ProcessoResposta>())!;
    }

    [Fact]
    public async Task Post_AdicionaMovimentacaoERetorna201()
    {
        var processo = await CriarProcessoAsync();

        var resposta = await _client.PostAsJsonAsync($"/processos/{processo.Id}/movimentacoes",
            new { textoMovimentacao = "Sentença publicada" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var movimentacao = await resposta.Content.ReadFromJsonAsync<MovimentacaoResposta>();
        Assert.NotNull(movimentacao);
        Assert.Equal(processo.Id, movimentacao.ProcessoId);
        Assert.Equal("Sentença publicada", movimentacao.TextoMovimentacao);
    }

    [Fact]
    public async Task Post_EmProcessoInexistente_Retorna404()
    {
        var resposta = await _client.PostAsJsonAsync("/processos/9999/movimentacoes",
            new { textoMovimentacao = "Sentença publicada" });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ok")]
    public async Task Post_ComTextoInvalido_Retorna400(string texto)
    {
        var processo = await CriarProcessoAsync();

        var resposta = await _client.PostAsJsonAsync($"/processos/{processo.Id}/movimentacoes",
            new { textoMovimentacao = texto });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_ListaSomenteAsMovimentacoesDoProcesso()
    {
        var processo = await CriarProcessoAsync();
        var outroProcesso = await CriarProcessoAsync();
        await _client.PostAsJsonAsync($"/processos/{processo.Id}/movimentacoes", new { textoMovimentacao = "Citação expedida" });
        await _client.PostAsJsonAsync($"/processos/{processo.Id}/movimentacoes", new { textoMovimentacao = "Contestação juntada" });
        await _client.PostAsJsonAsync($"/processos/{outroProcesso.Id}/movimentacoes", new { textoMovimentacao = "Despacho proferido" });

        var movimentacoes = await _client.GetFromJsonAsync<List<MovimentacaoResposta>>(
            $"/processos/{processo.Id}/movimentacoes");

        Assert.NotNull(movimentacoes);
        Assert.Equal(2, movimentacoes.Count);
        Assert.All(movimentacoes, m => Assert.Equal(processo.Id, m.ProcessoId));
    }

    [Fact]
    public async Task Get_EmProcessoInexistente_Retorna404()
    {
        var resposta = await _client.GetAsync("/processos/9999/movimentacoes");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
