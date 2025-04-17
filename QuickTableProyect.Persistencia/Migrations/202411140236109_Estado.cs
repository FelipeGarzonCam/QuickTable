namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Estado : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Empleado",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(),
                        Rol = c.String(),
                        Contrasena = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ItemDetalle",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MenuItemId = c.Int(nullable: false),
                        Nombre = c.String(),
                        Cantidad = c.Int(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PedidoActivoId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PedidosActivos", t => t.PedidoActivoId, cascadeDelete: true)
                .Index(t => t.PedidoActivoId);
            
            CreateTable(
                "dbo.PedidosActivos",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MeseroId = c.Int(nullable: false),
                        EmpleadoNombre = c.String(),
                        NumeroMesa = c.Int(nullable: false),
                        Subtotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IVA = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Total = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Estado = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MenuItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(),
                        Descripcion = c.String(),
                        Categoria = c.String(),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ItemDetalle", "PedidoActivoId", "dbo.PedidosActivos");
            DropIndex("dbo.ItemDetalle", new[] { "PedidoActivoId" });
            DropTable("dbo.MenuItem");
            DropTable("dbo.PedidosActivos");
            DropTable("dbo.ItemDetalle");
            DropTable("dbo.Empleado");
        }
    }
}
