namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RegistroSesion : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RegistroSesion",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EmpleadoId = c.Int(nullable: false),
                        FechaHoraConexion = c.DateTime(nullable: false),
                        FechaHoraDesconexion = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Empleado", t => t.EmpleadoId, cascadeDelete: true)
                .Index(t => t.EmpleadoId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RegistroSesion", "EmpleadoId", "dbo.Empleado");
            DropIndex("dbo.RegistroSesion", new[] { "EmpleadoId" });
            DropTable("dbo.RegistroSesion");
        }
    }
}
