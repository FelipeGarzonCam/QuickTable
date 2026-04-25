using QuickTableProyect.Persistencia.Datos;

namespace QuickTableProyect.Aplicacion
{
    public class ConfiguracionService
    {
        public string GetNombreRestaurante()
        {
            using var ctx = new SistemaQuickTableContext();
            var result = ctx.Database
                .SqlQuery<string>("SELECT NombreRestaurante FROM ConfiguracionSistema WHERE Id = 1")
                .FirstOrDefault();
            return result ?? "Mi Restaurante";
        }

        public void ActualizarNombreRestaurante(string nombre)
        {
            using var ctx = new SistemaQuickTableContext();
            int rows = ctx.Database.ExecuteSqlCommand(
                "UPDATE ConfiguracionSistema SET NombreRestaurante = @p0 WHERE Id = 1", nombre);
            if (rows == 0)
            {
                ctx.Database.ExecuteSqlCommand(
                    "INSERT INTO ConfiguracionSistema (Id, NombreRestaurante) VALUES (1, @p0)", nombre);
            }
        }
    }
}
