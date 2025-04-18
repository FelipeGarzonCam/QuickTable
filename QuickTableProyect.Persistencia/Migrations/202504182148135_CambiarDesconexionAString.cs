namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CambiarDesconexionAString : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.RegistroSesion", "FechaHoraDesconexion", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.RegistroSesion", "FechaHoraDesconexion", c => c.DateTime());
        }
    }
}
