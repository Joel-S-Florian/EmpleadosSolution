using Microsoft.Extensions.DependencyInjection;

namespace Empleados.Data.Services;

public static class ServiceCollectionExtensions
{
    // Servicios de negocio compartidos por la API y el MVC
    public static IServiceCollection AddServiciosEmpleados(this IServiceCollection services, string carpetaUploads)
    {
        services.AddSingleton(new AlmacenFotos(carpetaUploads));
        services.AddScoped<NotificacionService>();
        services.AddScoped<EmpleadoService>();
        services.AddScoped<DepartamentoService>();
        services.AddScoped<PuestoService>();
        services.AddScoped<ReporteService>();
        return services;
    }
}
