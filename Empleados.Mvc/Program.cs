using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar DbContext con Identity
builder.Services.AddDbContext<EmpleadosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // AddDefaultIdentity fija 128; 0 deja el tamaño de las migraciones (nvarchar(450)) igual que la API
    options.Stores.MaxLengthForKeys = 0;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<EmpleadosDbContext>();

// Servicios de negocio. Las fotos van a la carpeta compartida con la API (Uploads:Path, relativa al proyecto)
var carpetaUploads = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Uploads:Path"] ?? "../uploads"));
builder.Services.AddServiciosEmpleados(carpetaUploads);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RRHHOnly", policy => policy.RequireRole("Admin", "RRHH"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Montos y fechas siempre en formato dominicano, sin depender de la cultura del servidor
app.UseRequestLocalization("es-DO");
app.UseHttpsRedirection();
app.UseStaticFiles();
Directory.CreateDirectory(carpetaUploads);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(carpetaUploads), RequestPath = AlmacenFotos.RutaPublica });
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Crear roles y usuario admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var db = scope.ServiceProvider.GetRequiredService<EmpleadosDbContext>();
    // Aplicar migraciones pendientes automáticamente
    await db.Database.MigrateAsync();

    string[] roleNames = { "Admin", "RRHH" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Usuarios de prueba: solo se crean si su contraseña está configurada (appsettings.Development.json)
    var seedUsers = new[]
    {
        (Email: "admin@mvc.com", Rol: "Admin", Password: app.Configuration["SeedUsers:AdminPassword"]),
        (Email: "rrhh@mvc.com", Rol: "RRHH", Password: app.Configuration["SeedUsers:RrhhPassword"])
    };
    foreach (var seed in seedUsers)
    {
        if (string.IsNullOrEmpty(seed.Password) || await userManager.FindByEmailAsync(seed.Email) != null)
            continue;

        var user = new IdentityUser { UserName = seed.Email, Email = seed.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, seed.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo crear {seed.Email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        await userManager.AddToRoleAsync(user, seed.Rol);
    }

    // SeedData (solo desarrollo): si no hay departamentos o puestos, poblar datos de prueba (borra y reseed si se fuerza con arg --seed)
    if (app.Environment.IsDevelopment())
    {
        var forceSeed = args.Contains("--seed");
        var hasData = await db.Departamentos.AnyAsync() && await db.Puestos.AnyAsync();
        if (forceSeed || !hasData)
        {
            if (forceSeed)
                Console.WriteLine("Forzando SeedData --seed...");
            await SeedData.InitializeAsync(db);
            Console.WriteLine("SeedData completado.");
        }

        // Los usuarios de prueba sin empresa reciben acceso a la primera
        var primeraEmpresa = await db.Empresas.OrderBy(e => e.Id).Select(e => (int?)e.Id).FirstOrDefaultAsync();
        if (primeraEmpresa.HasValue)
        {
            foreach (var seed in seedUsers)
            {
                var user = await userManager.FindByEmailAsync(seed.Email);
                if (user != null && !await db.UsuariosEmpresas.AnyAsync(u => u.UsuarioId == user.Id))
                    db.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = user.Id, EmpresaId = primeraEmpresa.Value });
            }
            await db.SaveChangesAsync();
        }
    }
}

app.Run();