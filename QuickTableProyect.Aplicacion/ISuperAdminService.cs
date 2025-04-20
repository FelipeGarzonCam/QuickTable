using System;
using System.Threading.Tasks;
using QuickTableProyect.Dominio; // Ajusta según donde estén tus modelos
public interface ISuperAdminService
{
    Task<SuperAdmin> CreateSuperAdminAsync(string userName, string password);
    Task<RfidTagAssignment> CreateTagAsync(Guid superAdminId, string tagUid);
    Task<bool> RevokeTagAsync(Guid tagId);
    // ... otros métodos de listado o auditoría
}
