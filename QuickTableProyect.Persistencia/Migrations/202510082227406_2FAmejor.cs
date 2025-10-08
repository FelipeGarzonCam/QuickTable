namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _2FAmejor : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Codigo2FA", "Usado", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Codigo2FA", "Usado");
        }
    }
}
