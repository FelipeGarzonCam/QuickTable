using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace QuickTableProyect.Aplicacion
{
    public class SuperAdminService : ISuperAdminService
    {
        private readonly SistemaQuickTableContext _context;
        public SuperAdminService(SistemaQuickTableContext context)
        {
            _context = context;
        }
        public async Task<SuperAdmin> AuthenticateAsync(string nombre, string contrasena)
        {
            return await _context.SuperAdmins
                .FirstOrDefaultAsync(sa => sa.Nombre == nombre && sa.Contrasena == contrasena);
        }
        // Implementación de los métodos de la interfaz
        public async Task<SuperAdmin> CreateSuperAdminAsync(string userName, string password)
        {
            var superAdmin = new SuperAdmin
            {
                Nombre = userName,
                Contrasena = password
            };

            _context.SuperAdmins.Add(superAdmin);
            await _context.SaveChangesAsync();
            return superAdmin;
        }

        public async Task<RfidTagAssignment> CreateTagAsync(int superAdminId, string tagUid)
        {
            var tag = new RfidTagAssignment
            {
                SuperAdminId = superAdminId,
                TagUid = tagUid,
                IsActive = true,
                AssignedAt = DateTime.Now
            };

            _context.RfidTagAssignments.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<bool> RevokeTagAsync(Guid tagId)
        {
            var tag = await _context.RfidTagAssignments.FindAsync(tagId);
            if (tag != null)
            {
                tag.IsActive = false;
                _context.Entry(tag).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public List<SuperAdmin> ObtenerSuperAdmins()
            => _context.SuperAdmins.ToList();

        public SuperAdmin ObtenerPorId(int id)
            => _context.SuperAdmins.Find(id);

        public void CrearSuperAdmin(SuperAdmin sa)
        {
            _context.SuperAdmins.Add(sa);
            _context.SaveChanges();
        }

        public void CrearTag(int superAdminId, string tagUid)
        {
            var tag = new RfidTagAssignment
            {
                SuperAdminId = superAdminId,
                TagUid = tagUid,
                IsActive = true,
                AssignedAt = DateTime.Now
            };
            _context.RfidTagAssignments.Add(tag);
            _context.SaveChanges();
        }

        public void RevocarTag(int tagId)
        {
            var t = _context.RfidTagAssignments.Find(tagId);
            if (t != null)
            {
                t.IsActive = false;
                _context.Entry(t).State = EntityState.Modified;
                _context.SaveChanges();
            }
        }
    }
}

