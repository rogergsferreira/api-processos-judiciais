using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace ProcessosJudiciais.Api.Tests;

public sealed class ProcessosEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ProcessosEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private async Task<ProcessoResposta> CriarProcessoAsync(string? numero = null)
    {
        var resposta = await _client.PostAsJsonAsync("/processos", Dados.NovoProcesso(numero));
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ProcessoResposta>())!;
    }

    [Fact]
    public async Task Post_ComDadosValidos_Retorna201ComLocation()
    {
        var numero = Dados.NovoNumeroCnj();

        var resposta = await _client.PostAsJsonAsync("/processos", Dados.NovoProcesso(numero));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var processo = await resposta.Content.ReadFromJsonAsync<ProcessoResposta>();
        Assert.NotNull(processo);
        Assert.Equal(numero, processo.Numero);
        Assert.Equal($"/processos/{processo.Id}", resposta.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("123")]
    [InlineData("00000011220248260100")]
    [InlineData("0000001-12.2024.8.26.010")]
    public async Task Post_ComNumeroForaDoPadraoCnj_Retorna400(string numeroInvalido)
    {
        var resposta = await _client.PostAsJsonAsync("/processos", Dados.NovoProcesso(numeroInvalido));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        await using var db = _factory.CriarDbContext();
        Assert.False(await db.Processos.AnyAsync());
    }

    [Fact]
    public async Task Post_ComNumeroJaCadastrado_Retorna409()
    {
        var existente = await CriarProcessoAsync();

        var resposta = await _client.PostAsJsonAsync("/processos", Dados.NovoProcesso(existente.Numero));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task GetPorId_QuandoNaoExiste_Retorna404()
    {
        var resposta = await _client.GetAsync("/processos/9999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetPorId_RetornaProcessoComMovimentacoes()
    {
        var processo = await CriarProcessoAsync();
        await _client.PostAsJsonAsync($"/processos/{processo.Id}/movimentacoes",
            new { textoMovimentacao = "Petição inicial protocolada" });

        var encontrado = await _client.GetFromJsonAsync<ProcessoResposta>($"/processos/{processo.Id}");

        Assert.NotNull(encontrado);
        Assert.Equal(processo.Numero, encontrado.Numero);
        var movimentacao = Assert.Single(encontrado.Movimentacoes!);
        Assert.Equal("Petição inicial protocolada", movimentacao.TextoMovimentacao);
    }

    [Fact]
    public async Task BuscaPorNumero_RetornaProcesso()
    {
        var processo = await CriarProcessoAsync();

        var encontrado = await _client.GetFromJsonAsync<ProcessoResposta>($"/processos/busca/{processo.Numero}");

        Assert.NotNull(encontrado);
        Assert.Equal(processo.Id, encontrado.Id);
    }

    [Fact]
    public async Task BuscaPorNumero_QuandoNaoExiste_Retorna404()
    {
        var resposta = await _client.GetAsync($"/processos/busca/{Dados.NovoNumeroCnj()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Put_ComDadosValidos_AtualizaERetorna204()
    {
        var processo = await CriarProcessoAsync();
        var novoNumero = Dados.NovoNumeroCnj();

        var resposta = await _client.PutAsJsonAsync($"/processos/{processo.Id}", new
        {
            numero = novoNumero,
            nomeAutor = "João Pereira",
            nomeReu = "Outra Empresa S.A."
        });

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var atualizado = await _client.GetFromJsonAsync<ProcessoResposta>($"/processos/{processo.Id}");
        Assert.Equal(novoNumero, atualizado!.Numero);
        Assert.Equal("João Pereira", atualizado.NomeAutor);
    }

    [Fact]
    public async Task Put_ComNumeroDeOutroProcesso_Retorna409()
    {
        var primeiro = await CriarProcessoAsync();
        var segundo = await CriarProcessoAsync();

        var resposta = await _client.PutAsJsonAsync($"/processos/{segundo.Id}", Dados.NovoProcesso(primeiro.Numero));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Put_QuandoNaoExiste_Retorna404()
    {
        var resposta = await _client.PutAsJsonAsync("/processos/9999", Dados.NovoProcesso());

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Delete_RemoveProcessoESuasMovimentacoesEmCascata()
    {
        var processo = await CriarProcessoAsync();
        await _client.PostAsJsonAsync($"/processos/{processo.Id}/movimentacoes",
            new { textoMovimentacao = "Audiência designada" });

        var resposta = await _client.DeleteAsync($"/processos/{processo.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        await using var db = _factory.CriarDbContext();
        Assert.False(await db.Processos.AnyAsync(p => p.Id == processo.Id));
        Assert.False(await db.Movimentacoes.AnyAsync(m => m.ProcessoId == processo.Id));
    }
}
