namespace ProcessosJudiciais.Api.Models;

/// <summary>
/// Alerta gerado quando um processo fica tempo demais sem movimentação.
/// Fica aberto até o processo receber uma nova movimentação.
/// </summary>
public class Alerta
{
    public int Id { get; set; }
    public int ProcessoId { get; set; }
    public int DiasSemMovimentacao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }

    // Propriedade de Navegação
    public Processo? Processo { get; set; }
}
