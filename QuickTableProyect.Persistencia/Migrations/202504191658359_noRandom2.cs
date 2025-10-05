namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class noRandom2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistorialPedido", "CocinaListoAt", c => c.DateTime());
            AddColumn("dbo.HistorialPedido", "MeseroAceptadoAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistorialPedido", "MeseroAceptadoAt");
            DropColumn("dbo.HistorialPedido", "CocinaListoAt");
        }
    }
}
