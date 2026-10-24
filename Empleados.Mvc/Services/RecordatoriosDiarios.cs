using Empleados.Data.Services;

namespace Empleados.Mvc.Services;

// Genera una vez al día los avisos de cumpleaños, fin de período de prueba y vacaciones por vencer
public class RecordatoriosDiarios : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<RecordatoriosDiarios> _logger;

    public RecordatoriosDiarios(IServiceScopeFactory scopes, ILogger<RecordatoriosDiarios> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        while (!detener.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var creados = await scope.ServiceProvider.GetRequiredService<RecordatoriosService>().GenerarAsync(DateTime.Today);
                if (creados > 0)
                    _logger.LogInformation("Recordatorios del día: {Cantidad}", creados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron generar los recordatorios del día");
            }
            // Hasta las 6:00 del día siguiente
            await Task.Delay(DateTime.Today.AddDays(1).AddHours(6) - DateTime.Now, detener);
        }
    }
}
