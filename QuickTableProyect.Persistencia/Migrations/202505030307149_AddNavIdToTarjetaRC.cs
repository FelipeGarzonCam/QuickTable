namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNavIdToTarjetaRC : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TarjetaRC", "NavId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.TarjetaRC", "NavId");
        }
    }
}
