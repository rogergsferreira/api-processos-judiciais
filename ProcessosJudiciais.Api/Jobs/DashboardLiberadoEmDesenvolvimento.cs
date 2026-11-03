using Hangfire.Dashboard;

namespace ProcessosJudiciais.Api.Jobs;

/// <summary>
/// Libera o painel do Hangfire sem login. Só é registrado no ambiente de desenvolvimento
/// (no Docker as requisições não chegam como "locais", e o filtro padrão bloquearia o acesso).
/// </summary>
public class DashboardLiberadoEmDesenvolvimento : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
