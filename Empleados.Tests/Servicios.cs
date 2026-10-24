using Empleados.Data.Data;
using Empleados.Data.Services;

namespace Empleados.Tests;

// Construye los servicios reales sobre el DbContext en memoria de cada test
internal static class Servicios
{
    public static EmpleadoService Empleados(EmpleadosDbContext context) =>
        new(context, new NotificacionService(context), new AlmacenFotos(Path.Combine(Path.GetTempPath(), "empleados-tests")));

    public static PuestoService Puestos(EmpleadosDbContext context) => new(context);
    public static NotificacionService Notificaciones(EmpleadosDbContext context) => new(context);
    public static ReporteService Reportes(EmpleadosDbContext context) => new(context);
}
