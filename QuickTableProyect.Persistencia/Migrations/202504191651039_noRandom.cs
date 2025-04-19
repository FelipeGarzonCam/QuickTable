namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class noRandom : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistorialDetalle", "CocinaListoAt", c => c.DateTime());
            AddColumn("dbo.HistorialDetalle", "MeseroAceptadoAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistorialDetalle", "MeseroAceptadoAt");
            DropColumn("dbo.HistorialDetalle", "CocinaListoAt");
        }
    }
}
