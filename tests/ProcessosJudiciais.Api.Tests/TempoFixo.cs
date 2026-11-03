namespace ProcessosJudiciais.Api.Tests;

/// <summary>Relógio parado em um instante fixo, para testar regras que dependem da data atual.</summary>
public sealed class TempoFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}
