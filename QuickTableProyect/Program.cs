using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using QuickTableProyect.Infrastructure;
using QuickTableProyect.Persistencia.Datos;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Configurar Data Protection
services.AddDataProtection()
    .SetApplicationName("QuickTable")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickTable",
        "DataProtection-Keys"
    )))
    .ProtectKeysWithDpapi(); // Encripta las claves en Windows

// Registro de servicios
services.AddScoped<IPedidoService, PedidoService>();
services.AddScoped<SistemaQuickTableContext>();
services.AddScoped<BackupService>();
services.AddHttpContextAccessor();
services.AddScoped<MenuService>();
services.AddScoped<EmpleadoService>();
services.AddScoped<PedidoService>();
services.AddScoped<HistorialPedidoService>();
services.AddScoped<RegistroSesionService>();
services.AddScoped<AsistenciaService>();
services.AddScoped<TarjetaNFCService>();
services.AddScoped<CryptoService>();
services.AddScoped<ConfiguracionService>();

// Agregar MVC
services.AddControllersWithViews();
services.AddResponseCompression();

// CONFIGURACION DE SESION PARA 8 HORAS
services.AddDistributedMemoryCache(); // IMPORTANTE: Requerido para sesiones

services.AddSession(options =>
{
    // Nombre de la cookie de sesion
    options.Cookie.Name = ".QuickTable.Session";

    // Tiempo de inactividad: 8 HORAS
    // Si el usuario no hace ninguna peticion en 8 horas, la sesion expira
    options.IdleTimeout = TimeSpan.FromHours(8);

    // Tiempo maximo de vida de la cookie: 10 HORAS
    // Esto hace que la cookie persista incluso si cierras el navegador
    options.Cookie.MaxAge = TimeSpan.FromHours(23);

    // Configuraciones de seguridad
    options.Cookie.HttpOnly = true; // No accesible desde JavaScript
    options.Cookie.IsEssential = true; // Esencial para que funcione la app
    options.Cookie.SameSite = SameSiteMode.Lax; // Compatibilidad con navegadores

    // Timeout para operaciones de I/O con el almacen de sesion
    options.IOTimeout = TimeSpan.FromMinutes(2);

    // Si usas HTTPS en produccion, descomenta esta linea:
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Configurar Kestrel para obtener la IP local de la maquina
builder.WebHost.ConfigureKestrel(options =>
{
    string localIp = GetLocalIPAddress();
    int port = 5000; // Puerto 
    options.Listen(IPAddress.Parse(localIp), port);
    
});

var app = builder.Build();

// Inicializar la base de datos
DatabaseInitializer.InicializarDatos();

// AGREGAR AQUI: Abrir navegador automaticamente
Task.Run(async () =>
{
    // Esperar 2 segundos para que el servidor inicie completamente
    await Task.Delay(2000);

    string localIp = GetLocalIPAddress();
    string url = $"http://{localIp}:5000";

    try
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        // Si ocurre un error al abrir el navegador, simplemente continúa
        // El usuario puede abrir manualmente si es necesario
    }
});

// Configuracion de la aplicacion
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Solo usar HTTPS en producción
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self' data:; " +
        "frame-ancestors 'self'; " +      
        "form-action 'self';"             
    );
    await next();
});
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800"; // 7 dias
    }
});
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();

// Metodo para obtener la IP local de la maquina
string GetLocalIPAddress()
{
    var host = Dns.GetHostEntry(Dns.GetHostName());
    foreach (var ip in host.AddressList)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return ip.ToString();
        }
    }
    throw new Exception("No se pudo determinar la direccion IP local.");
}
