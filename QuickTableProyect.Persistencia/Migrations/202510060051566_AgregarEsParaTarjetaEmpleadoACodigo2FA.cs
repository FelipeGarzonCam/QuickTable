namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarEsParaTarjetaEmpleadoACodigo2FA : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empleado", "Activo", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Empleado", "Activo");
        }
    }
}
