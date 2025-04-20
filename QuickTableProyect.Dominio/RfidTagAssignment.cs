namespace QuickTableProyect.Dominio
{
    public class RfidTagAssignment
    {
        public int Id { get; set; }
        public string TagUid { get; set; }
        public bool IsActive { get; set; }
        public int SuperAdminId { get; set; }
        public virtual SuperAdmin SuperAdmin { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
