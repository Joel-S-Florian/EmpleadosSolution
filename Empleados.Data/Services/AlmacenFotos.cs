namespace Empleados.Data.Services;

// Carpeta compartida de fotos: la API y el MVC la configuran con la misma ruta (Uploads:Path) y la sirven en /uploads
public class AlmacenFotos
{
    public const string RutaPublica = "/uploads";

    public string CarpetaRaiz { get; }

    public AlmacenFotos(string carpetaRaiz)
    {
        CarpetaRaiz = Path.GetFullPath(carpetaRaiz);
    }

    public async Task<string> GuardarAsync(int empleadoId, Stream contenido, string extension)
    {
        var carpeta = Path.Combine(CarpetaRaiz, "empleados");
        Directory.CreateDirectory(carpeta);

        var nombreArchivo = $"{empleadoId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        using (var stream = new FileStream(Path.Combine(carpeta, nombreArchivo), FileMode.Create))
        {
            await contenido.CopyToAsync(stream);
        }

        return $"{RutaPublica}/empleados/{nombreArchivo}";
    }
}
