namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SuperAdmin : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RfidTagAssignment",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TagUid = c.String(),
                        IsActive = c.Boolean(nullable: false),
                        SuperAdminId = c.Int(nullable: false),
                        AssignedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SuperAdmin", t => t.SuperAdminId, cascadeDelete: true)
                .Index(t => t.SuperAdminId);
            
            CreateTable(
                "dbo.SuperAdmin",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(),
                        Contrasena = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RfidTagAssignment", "SuperAdminId", "dbo.SuperAdmin");
            DropIndex("dbo.RfidTagAssignment", new[] { "SuperAdminId" });
            DropTable("dbo.SuperAdmin");
            DropTable("dbo.RfidTagAssignment");
        }
    }
}
