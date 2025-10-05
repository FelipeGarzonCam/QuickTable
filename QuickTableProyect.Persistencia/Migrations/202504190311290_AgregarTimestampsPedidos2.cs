namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarTimestampsPedidos2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PedidosActivos", "FechaCreacion", c => c.DateTime(nullable: false));
            AddColumn("dbo.PedidosActivos", "MedioPago", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PedidosActivos", "MedioPago");
            DropColumn("dbo.PedidosActivos", "FechaCreacion");
        }
    }
}
