using Microsoft.Extensions.DependencyInjection;

namespace Empleados.Data.Services;

public static class ServiceCollectionExtensions
{
    // Servicios de negocio compartidos por la API y el MVC
    public static IServiceCollection AddServiciosEmpleados(this IServiceCollection services, string carpetaUploads, OpcionesCorreo? correo = null)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ContextoUsuarioHttp>();
        services.AddScoped<IContextoUsuario>(sp => sp.GetRequiredService<ContextoUsuarioHttp>());
        services.AddSingleton(new AlmacenFotos(carpetaUploads));
        services.AddScoped<EmpresaService>();
        services.AddScoped<ParametrosLegalesService>();
        services.AddScoped<NotificacionService>();
        services.AddScoped<EmpleadoService>();
        services.AddScoped<DepartamentoService>();
        services.AddScoped<PuestoService>();
        services.AddScoped<ReporteService>();
        services.AddScoped<NominaService>();
        services.AddScoped<CicloNominaService>();
        services.AddScoped<PrestamoService>();
        services.AddScoped<IncidenciaService>();
        services.AddScoped<BonificacionService>();
        services.AddScoped<LiquidacionService>();
        services.AddScoped<ArchivosService>();
        services.AddSingleton(correo ?? new OpcionesCorreo());
        services.AddSingleton<IEnviadorCorreo, EnviadorSmtp>();
        services.AddScoped<RecibosCorreoService>();
        services.AddScoped<PortalService>();
        services.AddScoped<RecordatoriosService>();
        return services;
    }
}
