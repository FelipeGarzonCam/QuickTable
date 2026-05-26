namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PrimeraNOBORAR : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PedidosActivos", "FechaUltimaEdicion", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PedidosActivos", "FechaUltimaEdicion");
        }
    }
}
