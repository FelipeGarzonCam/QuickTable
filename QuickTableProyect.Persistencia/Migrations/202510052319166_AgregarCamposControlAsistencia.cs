namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarCamposControlAsistencia : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Empleado", "TarjetaUID", c => c.String(maxLength: 200));
            AddColumn("dbo.RegistroSesion", "MarcoTarjetaSalida", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RegistroSesion", "MarcoTarjetaSalida");
            DropColumn("dbo.Empleado", "TarjetaUID");
        }
    }
}
