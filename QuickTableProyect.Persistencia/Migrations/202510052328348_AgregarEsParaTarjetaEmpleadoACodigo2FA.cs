namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarEsParaTarjetaEmpleadoACodigo2FA : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Codigo2FA", "EsParaTarjetaEmpleado", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Codigo2FA", "EsParaTarjetaEmpleado");
        }
    }
}
