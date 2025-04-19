namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddComentarioToItemDetalle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItemDetalle", "Comentario", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItemDetalle", "Comentario");
        }
    }
}
