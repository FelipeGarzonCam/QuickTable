namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTarjetaRCAndCodigo2FA : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Codigo2FA",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Codigo = c.String(),
                        NavegadorId = c.Guid(nullable: false),
                        Expiracion = c.DateTime(nullable: false),
                        Confirmado = c.Boolean(nullable: false),
                        EmpleadoId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Empleado", t => t.EmpleadoId, cascadeDelete: true)
                .Index(t => t.EmpleadoId);
            
            CreateTable(
                "dbo.TarjetaRC",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Uid = c.String(),
                        FechaCreacion = c.DateTime(nullable: false),
                        Activa = c.Boolean(nullable: false),
                        EmpleadoId = c.Int(),
                        FechaAsignacion = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Empleado", t => t.EmpleadoId)
                .Index(t => t.EmpleadoId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TarjetaRC", "EmpleadoId", "dbo.Empleado");
            DropForeignKey("dbo.Codigo2FA", "EmpleadoId", "dbo.Empleado");
            DropIndex("dbo.TarjetaRC", new[] { "EmpleadoId" });
            DropIndex("dbo.Codigo2FA", new[] { "EmpleadoId" });
            DropTable("dbo.TarjetaRC");
            DropTable("dbo.Codigo2FA");
        }
    }
}
