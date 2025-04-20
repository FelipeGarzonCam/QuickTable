namespace QuickTableProyect.Dominio
{
    public class SuperAdmin
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Contrasena { get; set; }
        // Relación 1-N con las tarjetas RFID
        public virtual ICollection<RfidTagAssignment> RfidTags { get; set; }
    }
}

