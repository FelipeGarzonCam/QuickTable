using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Linq;

namespace QuickTableProyect.Infrastructure
{
    public static class DatabaseInitializer
    {
        public static void InicializarDatos()
        {
            try
            {
                using var ctx = new SistemaQuickTableContext();

                // Verificar conexión a la base de datos con EF6
                var testConnection = ctx.Empleados.FirstOrDefault();

                CrearUsuarioTIPorDefecto(ctx);
                CrearTablaConfiguracion(ctx);

                Console.WriteLine("Sistema QuickTable inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inicializando el sistema: {ex.Message}");
            }
        }

        private static void CrearTablaConfiguracion(SistemaQuickTableContext ctx)
        {
            ctx.Database.ExecuteSqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfiguracionSistema')
                BEGIN
                    CREATE TABLE ConfiguracionSistema (
                        Id INT NOT NULL PRIMARY KEY,
                        NombreRestaurante NVARCHAR(100) NOT NULL
                    )
                    INSERT INTO ConfiguracionSistema (Id, NombreRestaurante) VALUES (1, N'Mi Restaurante')
                END
            ");
        }

        private static void CrearUsuarioTIPorDefecto(SistemaQuickTableContext ctx)
        {
            if (!ctx.Empleados.Any(e => e.Rol == "TI"))
            {
                var passwordService = new PasswordService();

                var usuarioTI = new Empleado
                {
                    Nombre = "TI",
                    Rol = "TI",
                    Contrasena = passwordService.HashPassword("12345"),
                    TarjetaUID = null
                };

                ctx.Empleados.Add(usuarioTI);
                ctx.SaveChanges();

                Console.WriteLine("Usuario TI creado automaticamente");
                Console.WriteLine("Nombre: TI");
                Console.WriteLine("Contraseña por defecto: 12345");
                Console.WriteLine("IMPORTANTE: Cambiar contraseña en el primer acceso");
            }
        }
    }
}
