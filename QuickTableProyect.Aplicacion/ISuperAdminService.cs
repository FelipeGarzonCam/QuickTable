using System;
using System.Threading.Tasks;
using QuickTableProyect.Dominio; // Ajusta según donde estén tus modelos
public interface ISuperAdminService
{
    Task<SuperAdmin> CreateSuperAdminAsync(string userName, string password);
    Task<RfidTagAssignment> CreateTagAsync(int superAdminId, string tagUid); 
    Task<bool> RevokeTagAsync(Guid tagId);
    Task<SuperAdmin> AuthenticateAsync(string nombre, string contrasena);
}
