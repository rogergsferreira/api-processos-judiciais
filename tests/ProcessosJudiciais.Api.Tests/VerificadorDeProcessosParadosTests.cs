using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcessosJudiciais.Api.Data;
using ProcessosJudiciais.Api.Jobs;
using ProcessosJudiciais.Api.Models;

namespace ProcessosJudiciais.Api.Tests;

public sealed class VerificadorDeProcessosParadosTests : IDisposable
{
    private static readonly DateTime Hoje = new(2026, 11, 3, 11, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public VerificadorDeProcessosParadosTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private VerificadorDeProcessosParados CriarVerificador(int diasSemMovimentacao = 30) => new(
        _db,
        new TempoFixo(Hoje),
        Options.Create(new AlertasOptions { DiasSemMovimentacao = diasSemMovimentacao }),
        NullLogger<VerificadorDeProcessosParados>.Instance);

    private Processo AdicionarProcesso(int diasDesdeCadastro, params int[] diasDesdeMovimentacoes)
    {
        var processo = new Processo
        {
            Numero = Dados.NovoNumeroCnj(),
            NomeAutor = "Maria da Silva",
            NomeReu = "Empresa Exemplo Ltda",
            DataCadastro = Hoje.AddDays(-diasDesdeCadastro)
        };

        foreach (var dias in diasDesdeMovimentacoes)
        {
            processo.Movimentacoes.Add(new Movimentacao
            {
                TextoMovimentacao = "Despacho proferido",
                DataMovimentacao = Hoje.AddDays(-dias)
            });
        }

        _db.Processos.Add(processo);
        _db.SaveChanges();
        return processo;
    }

    [Fact]
    public async Task CriaAlerta_QuandoUltimaMovimentacaoPassouDoLimite()
    {
        var parado = AdicionarProcesso(diasDesdeCadastro: 200, 120, 45);

        var criados = await CriarVerificador().ExecutarAsync();

        Assert.Equal(1, criados);
        var alerta = Assert.Single(await _db.Alertas.ToListAsync());
        Assert.Equal(parado.Id, alerta.ProcessoId);
        Assert.Equal(45, alerta.DiasSemMovimentacao);
        Assert.Null(alerta.ResolvidoEm);
    }

    [Fact]
    public async Task NaoCriaAlerta_QuandoHouveMovimentacaoRecente()
    {
        AdicionarProcesso(diasDesdeCadastro: 200, 120, 5);

        var criados = await CriarVerificador().ExecutarAsync();

        Assert.Equal(0, criados);
        Assert.False(await _db.Alertas.AnyAsync());
    }

    [Fact]
    public async Task UsaDataDeCadastro_QuandoProcessoNaoTemMovimentacoes()
    {
        var antigo = AdicionarProcesso(diasDesdeCadastro: 40);
        AdicionarProcesso(diasDesdeCadastro: 10);

        await CriarVerificador().ExecutarAsync();

        var alerta = Assert.Single(await _db.Alertas.ToListAsync());
        Assert.Equal(antigo.Id, alerta.ProcessoId);
        Assert.Equal(40, alerta.DiasSemMovimentacao);
    }

    [Fact]
    public async Task RespeitaOLimiteConfigurado()
    {
        AdicionarProcesso(diasDesdeCadastro: 100, 20);

        Assert.Equal(0, await CriarVerificador(diasSemMovimentacao: 30).ExecutarAsync());
        Assert.Equal(1, await CriarVerificador(diasSemMovimentacao: 15).ExecutarAsync());
    }

    [Fact]
    public async Task NaoDuplicaAlerta_QuandoJaExisteAlertaAberto()
    {
        AdicionarProcesso(diasDesdeCadastro: 90, 60);
        var verificador = CriarVerificador();

        var primeira = await verificador.ExecutarAsync();
        var segunda = await verificador.ExecutarAsync();

        Assert.Equal(1, primeira);
        Assert.Equal(0, segunda);
        Assert.Equal(1, await _db.Alertas.CountAsync());
    }

    [Fact]
    public async Task CriaNovoAlerta_QuandoOAnteriorFoiResolvidoEOProcessoContinuaParado()
    {
        var processo = AdicionarProcesso(diasDesdeCadastro: 90, 60);
        _db.Alertas.Add(new Alerta
        {
            ProcessoId = processo.Id,
            DiasSemMovimentacao = 31,
            CriadoEm = Hoje.AddDays(-29),
            ResolvidoEm = Hoje.AddDays(-20)
        });
        _db.SaveChanges();

        var criados = await CriarVerificador().ExecutarAsync();

        Assert.Equal(1, criados);
        Assert.Equal(1, await _db.Alertas.CountAsync(a => a.ResolvidoEm == null));
    }
}
