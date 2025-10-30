using Microsoft.Extensions.DependencyInjection;
using QuickTableProyect.Persistencia.Datos;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using QuickTableProyect.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

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

// Agregar MVC
services.AddControllersWithViews();

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

// Configuracion de la aplicacion
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// IMPORTANTE: UseSession debe estar DESPUES de UseRouting y ANTES de UseAuthorization
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
