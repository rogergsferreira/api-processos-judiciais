namespace ProcessosJudiciais.Api.Tests;

// Formatos das respostas da API, usados para ler o JSON nos testes
public record ProcessoResposta(
    int Id,
    string Numero,
    string NomeAutor,
    string NomeReu,
    DateTime DataCadastro,
    List<MovimentacaoResposta>? Movimentacoes);

public record MovimentacaoResposta(
    int Id,
    int ProcessoId,
    string TextoMovimentacao,
    DateTime DataMovimentacao);

public static class Dados
{
    private static int _sequencia;

    /// <summary>Gera um número CNJ válido e diferente a cada chamada.</summary>
    public static string NovoNumeroCnj()
    {
        var n = Interlocked.Increment(ref _sequencia);
        return $"{n:D7}-12.2024.8.26.0100";
    }

    public static object NovoProcesso(string? numero = null) => new
    {
        numero = numero ?? NovoNumeroCnj(),
        nomeAutor = "Maria da Silva",
        nomeReu = "Empresa Exemplo Ltda"
    };
}
