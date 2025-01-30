namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Caja : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HistorialDetalle",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        HistorialPedidoId = c.Int(nullable: false),
                        MenuItemId = c.Int(nullable: false),
                        Nombre = c.String(),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Cantidad = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HistorialPedido", t => t.HistorialPedidoId, cascadeDelete: true)
                .Index(t => t.HistorialPedidoId);
            
            CreateTable(
                "dbo.HistorialPedido",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NumeroMesa = c.Int(nullable: false),
                        MeseroId = c.Int(nullable: false),
                        MeseroNombre = c.String(),
                        FechaHora = c.DateTime(nullable: false),
                        Subtotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IVA = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Total = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Propina = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MetodoPago = c.String(),
                        EfectivoRecibido = c.Decimal(precision: 18, scale: 2),
                        Cambio = c.Decimal(precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.HistorialDetalle", "HistorialPedidoId", "dbo.HistorialPedido");
            DropIndex("dbo.HistorialDetalle", new[] { "HistorialPedidoId" });
            DropTable("dbo.HistorialPedido");
            DropTable("dbo.HistorialDetalle");
        }
    }
}
