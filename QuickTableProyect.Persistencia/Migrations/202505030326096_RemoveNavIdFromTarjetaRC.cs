namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveNavIdFromTarjetaRC : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.TarjetaRC", "NavId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.TarjetaRC", "NavId", c => c.String());
        }
    }
}
