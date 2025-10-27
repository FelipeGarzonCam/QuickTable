namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BackUp : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HistorialBackup",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FechaCreacion = c.DateTime(nullable: false),
                        NombreArchivo = c.String(nullable: false, maxLength: 200),
                        RutaArchivo = c.String(nullable: false, maxLength: 500),
                        TamanioMB = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmpleadoId = c.Int(nullable: false),
                        UsuarioCreador = c.String(),
                        TipoBackup = c.String(nullable: false, maxLength: 50),
                        Estado = c.String(nullable: false, maxLength: 50),
                        Observaciones = c.String(),
                        RutaCertificado = c.String(),
                        RutaClavePrivada = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.HistorialBackup");
        }
    }
}
