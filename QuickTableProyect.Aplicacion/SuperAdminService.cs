using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Aplicacion
{
    public class SuperAdminService
    {
        private readonly SistemaQuickTableContext _context;
        public SuperAdminService(SistemaQuickTableContext context)
        {
            _context = context;
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

