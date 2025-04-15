using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickTableProyect.Persistencia.Datos;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using System.Net;
using QuickTableProyect.Interface.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Registro del contexto de base de datos
builder.Services.AddScoped<SistemaQuickTableContext>();

// Otros servicios
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EmpleadoService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<HistorialPedidoService>();

builder.Services.AddControllersWithViews();
builder.Services.AddSession(); // sesiones
builder.Services.AddSignalR(); // SignalR 

// Configuración de Kestrel para obtener la IP local
builder.WebHost.ConfigureKestrel(options =>
{
    string localIp = GetLocalIPAddress();
    int port = 5000; // Puerto
    options.Listen(IPAddress.Parse(localIp), port);
});

var app = builder.Build();

// Configuración de la aplicación
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // Habilitar sesiones
app.UseAuthorization();

// Configuración de endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// Mapear el Hub de SignalR
app.MapHub<ComunicacionHub>("/comunicacionHub");

app.Run();

// Configuración de opciones de sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1); // Tiempo de sesión
    options.Cookie.HttpOnly = true; // Mejora la seguridad
    options.Cookie.IsEssential = true; // Necesario para GDPR
});

// Método para obtener la IP local de la máquina
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
    throw new Exception("No network adapters with an IPv4 address in the system!");
}
