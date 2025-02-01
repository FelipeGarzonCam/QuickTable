namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class exit : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Empleado", "FechaHoraConexion");
            DropColumn("dbo.Empleado", "FechaHoraDesconexion");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Empleado", "FechaHoraDesconexion", c => c.DateTime());
            AddColumn("dbo.Empleado", "FechaHoraConexion", c => c.DateTime());
        }
    }
}
