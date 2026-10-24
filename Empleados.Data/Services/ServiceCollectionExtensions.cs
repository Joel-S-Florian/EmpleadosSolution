using Microsoft.Extensions.DependencyInjection;

namespace Empleados.Data.Services;

public static class ServiceCollectionExtensions
{
    // Servicios de negocio compartidos por la API y el MVC
    public static IServiceCollection AddServiciosEmpleados(this IServiceCollection services, string carpetaUploads)
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
        return services;
    }
}
