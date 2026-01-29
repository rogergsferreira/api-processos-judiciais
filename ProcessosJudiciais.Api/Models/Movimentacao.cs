namespace ProcessosJudiciais.Api.Models;

public class Movimentacao
{
    public int Id { get; set; }
    public int ProcessoId { get; set; }
    public string TextoMovimentacao { get; set; } = string.Empty;
    public DateTime DataMovimentacao { get; init; } = DateTime.UtcNow;

    // Propriedade de Navegação
    public Processo? Processo { get; set; }
}