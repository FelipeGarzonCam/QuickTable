 namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class emparejamiento : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Codigo2FA", "EsParaTarjetaEmpleado", c => c.Boolean(nullable: false));
            AddColumn("dbo.Empleado", "TarjetaUID", c => c.String(maxLength: 200));
            AddColumn("dbo.Empleado", "Activo", c => c.Boolean(nullable: false));
            AddColumn("dbo.RegistroSesion", "MarcoTarjetaSalida", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RegistroSesion", "MarcoTarjetaSalida");
            DropColumn("dbo.Empleado", "Activo");
            DropColumn("dbo.Empleado", "TarjetaUID");
            DropColumn("dbo.Codigo2FA", "EsParaTarjetaEmpleado");
        }
    }
}
