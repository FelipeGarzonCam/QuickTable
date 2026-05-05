using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace QuickTableProyect.Aplicacion
{
    public class BackupService
    {
        private readonly SistemaQuickTableContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _backupPath;
        private readonly string _databaseName;

        public BackupService(SistemaQuickTableContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _backupPath = _configuration["BackupSettings:BackupPath"] ?? @"C:\QuickTableBackups\";
            _databaseName = "QuickTableProyectDB";

            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }
        }

        public (bool Exito, string Mensaje, int? BackupId) CrearBackupCompleto(int empleadoId, string nombreEmpleado, string observaciones = null)

        {
            string connectionString = null;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string nombreArchivo = $"QuickTableDB_{timestamp}.bak";
            string rutaCompleta = Path.Combine(_backupPath, nombreArchivo);

            try
            {
                connectionString = _configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    return (false, "No se pudo obtener la cadena de conexión", null);
                }

                string rutaBackup = rutaCompleta;

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Crear el directorio desde SQL Server para que la cuenta de servicio tenga acceso
                    try
                    {
                        using var cmdDir = new SqlCommand(
                            $"EXEC master.dbo.xp_create_subdir N'{_backupPath.Replace("'", "''")}'",
                            connection);
                        cmdDir.CommandTimeout = 15;
                        cmdDir.ExecuteNonQuery();
                    }
                    catch { /* xp_create_subdir puede no estar disponible o el directorio ya existe */ }

                    var backupCommand = $@"
                        BACKUP DATABASE [{_databaseName}]
                        TO DISK = N'{rutaBackup}'
                        WITH FORMAT, INIT, NAME = 'QuickTable-Full Backup', SKIP, NOREWIND, NOUNLOAD;";

                    using (var cmd = new SqlCommand(backupCommand, connection))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.ExecuteNonQuery();
                    }
                }

                var fileInfo = new FileInfo(rutaCompleta);
                var tamanioMB = (decimal)fileInfo.Length / (1024 * 1024);

                // Intentar backup de certificado TDE si existe
                string rutaCertificado = null;
                string rutaClavePrivada = null;

                try
                {
                    var nombreCertificado = $"TDE_Cert_{timestamp}.cer";
                    var nombreClavePrivada = $"TDE_Cert_{timestamp}.pvk";
                    rutaCertificado = Path.Combine(_backupPath, nombreCertificado);
                    rutaClavePrivada = Path.Combine(_backupPath, nombreClavePrivada);

                    using (var connection2 = new SqlConnection(connectionString))
                    {
                        connection2.Open();

                        // Obtener el nombre del certificado que protege la BD con TDE
                        var obtenerCertificado = @"
            USE master;
            SELECT TOP 1 c.name 
            FROM sys.certificates c
            INNER JOIN sys.dm_database_encryption_keys dek 
                ON c.thumbprint = dek.encryptor_thumbprint
            WHERE dek.database_id = DB_ID('QuickTableProyectDB')
              AND dek.encryption_state = 3;";

                        string nombreCert = null;

                        using (var cmdGetCert = new SqlCommand(obtenerCertificado, connection2))
                        {
                            var result = cmdGetCert.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                nombreCert = result.ToString();
                            }
                        }

                        if (!string.IsNullOrEmpty(nombreCert))
                        {
                            // Hacer backup del certificado
                            var backupCertCommand = $@"
                USE master;
                BACKUP CERTIFICATE [{nombreCert}]
                TO FILE = N'{rutaCertificado.Replace("\\", "\\\\")}'
                WITH PRIVATE KEY (
                    FILE = N'{rutaClavePrivada.Replace("\\", "\\\\")}',
                    ENCRYPTION BY PASSWORD = 'QuickTable2025TDE!Secure'
                );";

                            using (var cmdBackupCert = new SqlCommand(backupCertCommand, connection2))
                            {
                                cmdBackupCert.CommandTimeout = 60;
                                cmdBackupCert.ExecuteNonQuery();
                            }

                            // Verificar que los archivos se crearon
                            if (File.Exists(rutaCertificado) && File.Exists(rutaClavePrivada))
                            {
                                // Todo OK
                            }
                            else
                            {
                                rutaCertificado = "N/A (archivos no creados)";
                                rutaClavePrivada = "N/A (archivos no creados)";
                            }
                        }
                        else
                        {
                            rutaCertificado = "N/A (sin TDE activo)";
                            rutaClavePrivada = "N/A (sin TDE activo)";
                        }
                    }
                }
                catch (Exception exTDE)
                {
                    rutaCertificado = $"N/A (error: {exTDE.Message.Substring(0, Math.Min(50, exTDE.Message.Length))})";
                    rutaClavePrivada = $"N/A (error al respaldar)";
                }


                var historial = new HistorialBackup
                {
                    FechaCreacion = DateTime.Now,
                    NombreArchivo = nombreArchivo,
                    RutaArchivo = rutaCompleta,
                    TamanioMB = Math.Round(tamanioMB, 2),
                    EmpleadoId = empleadoId,
                    UsuarioCreador = nombreEmpleado,
                    TipoBackup = "Completo",
                    Estado = "Exitoso",
                    Observaciones = string.IsNullOrWhiteSpace(observaciones)
                         ? "Backup creado correctamente"
                         : observaciones,
                    RutaCertificado = rutaCertificado,
                    RutaClavePrivada = rutaClavePrivada
                };


                _context.HistorialBackups.Add(historial);
                _context.SaveChanges();

                return (true, $"Backup creado exitosamente: {nombreArchivo} ({tamanioMB:F2} MB)", historial.Id);
            }
            catch (SqlException sqlEx)
            {
                var errorDetallado = $"Error SQL: {sqlEx.Message} (Número: {sqlEx.Number})";

                var historialError = new HistorialBackup
                {
                    FechaCreacion = DateTime.Now,
                    NombreArchivo = nombreArchivo,
                    RutaArchivo = rutaCompleta,
                    TamanioMB = 0,
                    EmpleadoId = empleadoId,
                    UsuarioCreador = nombreEmpleado,
                    TipoBackup = "Completo",
                    Estado = "Fallido",
                    Observaciones = errorDetallado,
                    RutaCertificado = "N/A",
                    RutaClavePrivada = "N/A"
                };

                try
                {
                    _context.HistorialBackups.Add(historialError);
                    _context.SaveChanges();
                }
                catch { }

                return (false, errorDetallado, null);
            }
            catch (Exception ex)
            {
                var errorDetallado = $"Error: {ex.Message}";

                var historialError = new HistorialBackup
                {
                    FechaCreacion = DateTime.Now,
                    NombreArchivo = nombreArchivo,
                    RutaArchivo = rutaCompleta,
                    TamanioMB = 0,
                    EmpleadoId = empleadoId,
                    UsuarioCreador = nombreEmpleado,
                    TipoBackup = "Completo",
                    Estado = "Fallido",
                    Observaciones = errorDetallado,
                    RutaCertificado = "N/A",
                    RutaClavePrivada = "N/A"
                };

                try
                {
                    _context.HistorialBackups.Add(historialError);
                    _context.SaveChanges();
                }
                catch { }

                return (false, errorDetallado, null);
            }
        }

        public List<HistorialBackup> ObtenerHistorialBackup()
        {
            try
            {
                return _context.HistorialBackups
                    .OrderByDescending(h => h.FechaCreacion)
                    .ToList();
            }
            catch
            {
                return new List<HistorialBackup>();
            }
        }

        public (bool Exito, string Mensaje) RestaurarBackup(int backupId, int empleadoId, string nombreEmpleado)
        {
            SqlConnection connectionPrincipal = null;
            List<HistorialBackup> backupsGuardados = null;

            try
            {
                var backup = _context.HistorialBackups.Find(backupId);
                if (backup == null)
                {
                    return (false, "Backup no encontrado");
                }

                if (!File.Exists(backup.RutaArchivo))
                {
                    return (false, "El archivo de backup no existe en el disco");
                }

                //  PASO 1: GUARDAR TODOS LOS BACKUPS EN MEMORIA 
                backupsGuardados = _context.HistorialBackups.OrderBy(b => b.Id).ToList();

                if (backupsGuardados == null || backupsGuardados.Count == 0)
                {
                    return (false, "Error: No se pudo cargar el historial en memoria");
                }

                System.Diagnostics.Debug.WriteLine($"Backups guardados en memoria: {backupsGuardados.Count}");

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var rutaBackup = backup.RutaArchivo;

                //  PASO 2: CERRAR ENTITY FRAMEWORK 
                try
                {
                    if (_context.Database.Connection.State == System.Data.ConnectionState.Open)
                    {
                        _context.Database.Connection.Close();
                    }
                }
                catch { }

                System.Threading.Thread.Sleep(1000);

                //  PASO 3: CREAR NUEVA CONEXIÓN 
                connectionPrincipal = new SqlConnection(connectionString);
                connectionPrincipal.Open();

                //  PASO 4: RESTAURAR BASE DE DATOS 
                var restaurarDB = $@"
            USE master;
            
            DECLARE @kill varchar(max) = '';
            SELECT @kill = @kill + 'KILL ' + CONVERT(varchar(5), session_id) + ';'
            FROM sys.dm_exec_sessions
            WHERE database_id = DB_ID('QuickTableProyectDB')
                AND session_id <> @@SPID;
            
            IF LEN(@kill) > 0
                EXEC(@kill);
            
            ALTER DATABASE [QuickTableProyectDB] 
            SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            
            RESTORE DATABASE [QuickTableProyectDB]
            FROM DISK = N'{rutaBackup}'
            WITH REPLACE, RECOVERY;
            
            ALTER DATABASE [QuickTableProyectDB] 
            SET MULTI_USER;";

                using (var cmdRestore = new SqlCommand(restaurarDB, connectionPrincipal))
                {
                    cmdRestore.CommandTimeout = 600;
                    cmdRestore.ExecuteNonQuery();
                }

                System.Diagnostics.Debug.WriteLine("Restore ejecutado, esperando...");

                //  PASO 5: ESPERAR Y VERIFICAR QUE LA RESTAURACIÓN TERMINÓ 
                bool restauracionCompleta = false;
                int intentos = 0;
                int maxIntentos = 10;

                while (!restauracionCompleta && intentos < maxIntentos)
                {
                    System.Threading.Thread.Sleep(2000); // Esperar 2 segundos
                    intentos++;

                    try
                    {
                        var verificarEstado = @"
                    USE master;
                    SELECT 
                        CASE 
                            WHEN state_desc = 'ONLINE' AND is_read_only = 0 
                            THEN 1 
                            ELSE 0 
                        END
                    FROM sys.databases 
                    WHERE name = 'QuickTableProyectDB';";

                        using (var cmdVerificar = new SqlCommand(verificarEstado, connectionPrincipal))
                        {
                            var resultado = cmdVerificar.ExecuteScalar();
                            if (resultado != null && Convert.ToInt32(resultado) == 1)
                            {
                                restauracionCompleta = true;
                                System.Diagnostics.Debug.WriteLine($"BD restaurada y online (intento {intentos})");
                            }
                        }
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine($"Intento {intentos}: BD aún no disponible");
                    }
                }

                if (!restauracionCompleta)
                {
                    return (false, "Timeout: La restauración tardó demasiado");
                }

                // Espera adicional de seguridad
                System.Threading.Thread.Sleep(2000);

                //  PASO 6: ELIMINAR Y RECREAR TABLA 
                System.Diagnostics.Debug.WriteLine("Recreando tabla HistorialBackup...");

                var recrearTabla = @"
            USE QuickTableProyectDB;
            
            IF OBJECT_ID('HistorialBackup', 'U') IS NOT NULL
                DROP TABLE HistorialBackup;
            
            CREATE TABLE HistorialBackup (
                Id INT PRIMARY KEY IDENTITY(1,1),
                FechaCreacion DATETIME NOT NULL,
                NombreArchivo NVARCHAR(200) NOT NULL,
                RutaArchivo NVARCHAR(500) NOT NULL,
                TamanioMB DECIMAL(18,2) NOT NULL,
                EmpleadoId INT NOT NULL,
                UsuarioCreador NVARCHAR(100) NOT NULL,
                TipoBackup NVARCHAR(50) NOT NULL,
                Estado NVARCHAR(50) NOT NULL,
                Observaciones NVARCHAR(MAX) NULL,
                RutaCertificado NVARCHAR(500) NULL,
                RutaClavePrivada NVARCHAR(500) NULL
            );";

                using (var cmdRecrear = new SqlCommand(recrearTabla, connectionPrincipal))
                {
                    cmdRecrear.CommandTimeout = 60;
                    cmdRecrear.ExecuteNonQuery();
                }

                System.Diagnostics.Debug.WriteLine("Tabla recreada");

                //  PASO 7: RE-INSERTAR DESDE MEMORIA 
                System.Diagnostics.Debug.WriteLine("Insertando backups desde memoria...");

                using (var cmdIdentityOn = new SqlCommand("USE QuickTableProyectDB; SET IDENTITY_INSERT HistorialBackup ON;", connectionPrincipal))
                {
                    cmdIdentityOn.ExecuteNonQuery();
                }

                int insertados = 0;
                foreach (var bk in backupsGuardados)
                {
                    try
                    {
                        var insertSQL = @"
                    USE QuickTableProyectDB;
                    INSERT INTO HistorialBackup 
                    (Id, FechaCreacion, NombreArchivo, RutaArchivo, TamanioMB, 
                     EmpleadoId, UsuarioCreador, TipoBackup, Estado, Observaciones,
                     RutaCertificado, RutaClavePrivada)
                    VALUES 
                    (@Id, @FechaCreacion, @NombreArchivo, @RutaArchivo, @TamanioMB,
                     @EmpleadoId, @UsuarioCreador, @TipoBackup, @Estado, @Observaciones,
                     @RutaCertificado, @RutaClavePrivada);";

                        using (var cmdInsert = new SqlCommand(insertSQL, connectionPrincipal))
                        {
                            cmdInsert.Parameters.AddWithValue("@Id", bk.Id);
                            cmdInsert.Parameters.AddWithValue("@FechaCreacion", bk.FechaCreacion);
                            cmdInsert.Parameters.AddWithValue("@NombreArchivo", bk.NombreArchivo ?? "");
                            cmdInsert.Parameters.AddWithValue("@RutaArchivo", bk.RutaArchivo ?? "");
                            cmdInsert.Parameters.AddWithValue("@TamanioMB", bk.TamanioMB);
                            cmdInsert.Parameters.AddWithValue("@EmpleadoId", bk.EmpleadoId);
                            cmdInsert.Parameters.AddWithValue("@UsuarioCreador", bk.UsuarioCreador ?? "");
                            cmdInsert.Parameters.AddWithValue("@TipoBackup", bk.TipoBackup ?? "");
                            cmdInsert.Parameters.AddWithValue("@Estado", bk.Estado ?? "");
                            cmdInsert.Parameters.AddWithValue("@Observaciones",
                                string.IsNullOrEmpty(bk.Observaciones) ? (object)DBNull.Value : bk.Observaciones);
                            cmdInsert.Parameters.AddWithValue("@RutaCertificado",
                                string.IsNullOrEmpty(bk.RutaCertificado) ? (object)DBNull.Value : bk.RutaCertificado);
                            cmdInsert.Parameters.AddWithValue("@RutaClavePrivada",
                                string.IsNullOrEmpty(bk.RutaClavePrivada) ? (object)DBNull.Value : bk.RutaClavePrivada);

                            cmdInsert.ExecuteNonQuery();
                            insertados++;
                        }
                    }
                    catch (Exception exInsert)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al insertar backup {bk.Id}: {exInsert.Message}");
                    }
                }

                using (var cmdIdentityOff = new SqlCommand("USE QuickTableProyectDB; SET IDENTITY_INSERT HistorialBackup OFF;", connectionPrincipal))
                {
                    cmdIdentityOff.ExecuteNonQuery();
                }

                System.Diagnostics.Debug.WriteLine($"Total insertados: {insertados}");

                //  PASO 8: CERRAR CONEXIÓN 
                if (connectionPrincipal != null && connectionPrincipal.State == System.Data.ConnectionState.Open)
                {
                    connectionPrincipal.Close();
                    connectionPrincipal.Dispose();
                    connectionPrincipal = null;
                }

                return (true, $"Restauración exitosa. {insertados} de {backupsGuardados.Count} backups preservados.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
            finally
            {
                if (connectionPrincipal != null)
                {
                    try
                    {
                        if (connectionPrincipal.State == System.Data.ConnectionState.Open)
                        {
                            connectionPrincipal.Close();
                        }
                        connectionPrincipal.Dispose();
                    }
                    catch { }
                }
            }
        }

        public (bool Exito, string Mensaje) EliminarBackup(int backupId)
        {
            try
            {
                var backup = _context.HistorialBackups.Find(backupId);
                if (backup == null)
                {
                    return (false, "Backup no encontrado");
                }

                if (File.Exists(backup.RutaArchivo))
                {
                    File.Delete(backup.RutaArchivo);
                }

                if (!string.IsNullOrEmpty(backup.RutaCertificado) &&
                    backup.RutaCertificado != "N/A" &&
                    File.Exists(backup.RutaCertificado))
                {
                    try { File.Delete(backup.RutaCertificado); } catch { /* creado por SQL Server service account */ }
                }

                if (!string.IsNullOrEmpty(backup.RutaClavePrivada) &&
                    backup.RutaClavePrivada != "N/A" &&
                    File.Exists(backup.RutaClavePrivada))
                {
                    try { File.Delete(backup.RutaClavePrivada); } catch { /* creado por SQL Server service account */ }
                }

                _context.HistorialBackups.Remove(backup);
                _context.SaveChanges();

                return (true, "Backup eliminado correctamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al eliminar: {ex.Message}");
            }
        }

        public long ObtenerEspacioDisponibleMB()
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(_backupPath));
                return drive.AvailableFreeSpace / (1024 * 1024);
            }
            catch
            {
                return -1;
            }
        }
    }
}
