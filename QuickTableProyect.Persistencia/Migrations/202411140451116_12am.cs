namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _12am : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItemDetalle", "CantidadPreparada", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItemDetalle", "CantidadPreparada");
        }
    }
}
