namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarTimestampsPedidos : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.HistorialPedido", "MeseroId", "dbo.Empleado");
            DropForeignKey("dbo.HistorialDetalle", "MenuItemId", "dbo.MenuItem");
            DropIndex("dbo.HistorialDetalle", new[] { "MenuItemId" });
            DropIndex("dbo.HistorialPedido", new[] { "MeseroId" });
            AddColumn("dbo.HistorialPedido", "NumeroMesa", c => c.Int(nullable: false));
            AddColumn("dbo.HistorialPedido", "MeseroNombre", c => c.String());
            AddColumn("dbo.HistorialPedido", "FechaHora", c => c.DateTime(nullable: false));
            AddColumn("dbo.HistorialPedido", "IVA", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.HistorialPedido", "Propina", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.HistorialPedido", "MetodoPago", c => c.String());
            AddColumn("dbo.HistorialPedido", "EfectivoRecibido", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.HistorialPedido", "Cambio", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.PedidosActivos", "CocinaListoAt", c => c.DateTime());
            AddColumn("dbo.PedidosActivos", "MeseroAceptadoAt", c => c.DateTime());
            DropColumn("dbo.HistorialDetalle", "PrecioUnitario");
            DropColumn("dbo.HistorialPedido", "Mesa");
            DropColumn("dbo.HistorialPedido", "Pedido");
            DropColumn("dbo.HistorialPedido", "FechaHoraCreacion");
            DropColumn("dbo.HistorialPedido", "FechaHoraListo");
            DropColumn("dbo.HistorialPedido", "FechaHoraAceptacion");
            DropColumn("dbo.HistorialPedido", "FechaHoraCambio");
            DropColumn("dbo.HistorialPedido", "FechaHoraFinalizado");
            DropColumn("dbo.HistorialPedido", "MedioPago");
        }
        
        public override void Down()
        {
            AddColumn("dbo.HistorialPedido", "MedioPago", c => c.String());
            AddColumn("dbo.HistorialPedido", "FechaHoraFinalizado", c => c.DateTime());
            AddColumn("dbo.HistorialPedido", "FechaHoraCambio", c => c.DateTime());
            AddColumn("dbo.HistorialPedido", "FechaHoraAceptacion", c => c.DateTime());
            AddColumn("dbo.HistorialPedido", "FechaHoraListo", c => c.DateTime());
            AddColumn("dbo.HistorialPedido", "FechaHoraCreacion", c => c.DateTime(nullable: false));
            AddColumn("dbo.HistorialPedido", "Pedido", c => c.Int(nullable: false));
            AddColumn("dbo.HistorialPedido", "Mesa", c => c.Int(nullable: false));
            AddColumn("dbo.HistorialDetalle", "PrecioUnitario", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            DropColumn("dbo.PedidosActivos", "MeseroAceptadoAt");
            DropColumn("dbo.PedidosActivos", "CocinaListoAt");
            DropColumn("dbo.HistorialPedido", "Cambio");
            DropColumn("dbo.HistorialPedido", "EfectivoRecibido");
            DropColumn("dbo.HistorialPedido", "MetodoPago");
            DropColumn("dbo.HistorialPedido", "Propina");
            DropColumn("dbo.HistorialPedido", "IVA");
            DropColumn("dbo.HistorialPedido", "FechaHora");
            DropColumn("dbo.HistorialPedido", "MeseroNombre");
            DropColumn("dbo.HistorialPedido", "NumeroMesa");
            CreateIndex("dbo.HistorialPedido", "MeseroId");
            CreateIndex("dbo.HistorialDetalle", "MenuItemId");
            AddForeignKey("dbo.HistorialDetalle", "MenuItemId", "dbo.MenuItem", "Id", cascadeDelete: true);
            AddForeignKey("dbo.HistorialPedido", "MeseroId", "dbo.Empleado", "Id", cascadeDelete: true);
        }
    }
}
