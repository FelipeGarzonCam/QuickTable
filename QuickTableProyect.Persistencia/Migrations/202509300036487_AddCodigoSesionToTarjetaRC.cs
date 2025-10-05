namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCodigoSesionToTarjetaRC : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TarjetaRC", "CodigoSesion", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.TarjetaRC", "CodigoSesion");
        }
    }
}
