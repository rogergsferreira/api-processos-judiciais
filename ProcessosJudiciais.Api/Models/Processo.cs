namespace ProcessosJudiciais.Api.Models;

public class Processo
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string NomeAutor { get; set; } = string.Empty;
    public string NomeReu { get; set; } = string.Empty;
    public DateTime DataCadastro { get; init; } = DateTime.UtcNow;

    // Propriedade de Navegação: Um processo possui muitas movimentações
    public ICollection<Movimentacao> Movimentacoes { get; set; } = [];
}
