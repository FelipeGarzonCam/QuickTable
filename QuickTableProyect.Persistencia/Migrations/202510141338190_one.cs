namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class one : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Codigo2FA",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Codigo = c.String(),
                        NavegadorId = c.Guid(nullable: false),
                        Expiracion = c.DateTime(nullable: false),
                        Confirmado = c.Boolean(nullable: false),
                        EmpleadoId = c.Int(nullable: false),
                        EsParaTarjetaEmpleado = c.Boolean(nullable: false),
                        Usado = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Empleado", t => t.EmpleadoId, cascadeDelete: true)
                .Index(t => t.EmpleadoId);
            
            CreateTable(
                "dbo.Empleado",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(),
                        Rol = c.String(),
                        Contrasena = c.String(maxLength: 60),
                        TarjetaUID = c.String(maxLength: 200),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
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
                        CocinaListoAt = c.DateTime(),
                        MeseroAceptadoAt = c.DateTime(),
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
                        CocinaListoAt = c.DateTime(),
                        MeseroAceptadoAt = c.DateTime(),
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
                        CantidadPreparada = c.Int(nullable: false),
                        Comentario = c.String(),
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
                        CocinaListoAt = c.DateTime(),
                        MeseroAceptadoAt = c.DateTime(),
                        FechaCreacion = c.DateTime(nullable: false),
                        MedioPago = c.String(),
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
            
            CreateTable(
                "dbo.RegistroSesion",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EmpleadoId = c.Int(nullable: false),
                        FechaHoraConexion = c.DateTime(nullable: false),
                        FechaHoraDesconexion = c.String(),
                        MarcoTarjetaSalida = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Empleado", t => t.EmpleadoId, cascadeDelete: true)
                .Index(t => t.EmpleadoId);
            
            CreateTable(
                "dbo.TarjetaRC",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Uid = c.String(),
                        Activa = c.Boolean(nullable: false),
                        FechaCreacion = c.DateTime(nullable: false),
                        FechaAsignacion = c.DateTime(),
                        EmpleadoId = c.Int(),
                        CodigoSesion = c.String(),
                        UidFisico = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Empleado", t => t.EmpleadoId)
                .Index(t => t.EmpleadoId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TarjetaRC", "EmpleadoId", "dbo.Empleado");
            DropForeignKey("dbo.RegistroSesion", "EmpleadoId", "dbo.Empleado");
            DropForeignKey("dbo.ItemDetalle", "PedidoActivoId", "dbo.PedidosActivos");
            DropForeignKey("dbo.HistorialDetalle", "HistorialPedidoId", "dbo.HistorialPedido");
            DropForeignKey("dbo.Codigo2FA", "EmpleadoId", "dbo.Empleado");
            DropIndex("dbo.TarjetaRC", new[] { "EmpleadoId" });
            DropIndex("dbo.RegistroSesion", new[] { "EmpleadoId" });
            DropIndex("dbo.ItemDetalle", new[] { "PedidoActivoId" });
            DropIndex("dbo.HistorialDetalle", new[] { "HistorialPedidoId" });
            DropIndex("dbo.Codigo2FA", new[] { "EmpleadoId" });
            DropTable("dbo.TarjetaRC");
            DropTable("dbo.RegistroSesion");
            DropTable("dbo.MenuItem");
            DropTable("dbo.PedidosActivos");
            DropTable("dbo.ItemDetalle");
            DropTable("dbo.HistorialPedido");
            DropTable("dbo.HistorialDetalle");
            DropTable("dbo.Empleado");
            DropTable("dbo.Codigo2FA");
        }
    }
}
