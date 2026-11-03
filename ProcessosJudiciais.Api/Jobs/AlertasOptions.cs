namespace ProcessosJudiciais.Api.Jobs;

/// <summary>Configuração da rotina de processos parados (seção "Alertas" do appsettings).</summary>
public class AlertasOptions
{
    /// <summary>Quantos dias sem movimentação fazem um processo ser considerado parado.</summary>
    public int DiasSemMovimentacao { get; set; } = 30;

    /// <summary>Quando o job roda, em formato cron (padrão: todo dia às 8h).</summary>
    public string Cron { get; set; } = "0 8 * * *";

    /// <summary>Fuso horário usado para interpretar o cron.</summary>
    public string FusoHorario { get; set; } = "America/Sao_Paulo";
}
