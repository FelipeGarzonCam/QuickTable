namespace QuickTableProyect.Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AumentarTamanoContrasena : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Empleado", "Contrasena", c => c.String(maxLength: 60));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Empleado", "Contrasena", c => c.String());
        }
    }
}
