
# QuickTable Proyect

¡Bienvenido a **QuickTable**! Este proyecto es una solución de gestión de comandas diseñada para optimizar la operación en restaurantes, garantizando seguridad de datos y una experiencia de usuario ágil.

---

## 📌 Información General

- **Framework**: .NET 8 (Razor Pages)  
- **ORM**: Entity Framework 6  
- **Base de datos**: SQL Server  
- **Frontend**: Plantillas AdminLTE  
- **Autenticación**: Soporte para roles (Mesero, Cocina, Caja, Administrador, TI) y 2FA para Admin
- **Deployment**: Diseñado para funcionar en intranet sin conexión a Internet  

---

<!--## 🚀 Características Principales

1. **Gestión de Comandas en Tiempo Real**: Actualizaciones dinámicas mediante AJAX para reflejar cambios sin recargar la página.  
2. **Seguridad Avanzada**:  
   - Cifrado de datos críticos en SQL Server.  
   - Autenticación de dos factores con tarjetas NFC utilizando Raspberry Pi 3.  
   - Control de sesión activa y registro de cierres.  
3. **Interfaz Intuitiva**:  
   - Dashboard responsive con AdminLTE.  
   - Vistas adaptadas a diferentes roles con permisos granulares.  
4. **Escalabilidad y Mantenimiento**:  
   - Estructura limpia en capas (Interface, Aplicación, Dominio, Persistencia).  
   - Migraciones y seed de datos automatizadas.  

---

## ⚙️ Instalación

1. Clona este repositorio:  
   ```bash
   git clone https://github.com/tu-usuario/QuickTable.git
   cd QuickTable
   ```  
2. Configura la conexión a SQL Server en `appsettings.json`:  
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=TU_SERVIDOR;Database=QuickTableDB;Trusted_Connection=True;"
   }
   ```  
3. Aplica migraciones y seed de datos:  
   ```bash
   dotnet ef database update
   ```  
4. Ejecuta la aplicación:  
   ```bash
   dotnet run --project QuickTable.Interface
   ```  
5. Accede en tu navegador a `http://quicktable.local:5000` (o la URL configurada).

---

## 🎯 Uso

- **Registrar Usuarios**: Crea cuentas con roles y gestiona permisos.  
- **Crear Comandas**: Selecciona mesero, añade productos y envía a cocina.  
- **Rastrear Estados**: Monitorea el avance de cada plato.  
- **Cerrar Sesiones**: Registra automáticamente los cierres y detecta errores si exceden 15 horas.

---

## 🤝 Contribuciones

¡Las contribuciones son bienvenidas! Sigue estos pasos:

1. Haz fork del repositorio.  
2. Crea una rama nueva: `git checkout -b feature/nueva-funcionalidad`.  
3. Realiza tus cambios y haz commit: `git commit -m "Añade nueva funcionalidad"`.  
4. Sube tu rama: `git push origin feature/nueva-funcionalidad`.  
5. Abre un Pull Request describiendo tus mejoras.

---

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más detalles.

---

*¡Gracias por usar QuickTable! Si tienes dudas, abre un issue o contáctanos.*
``` -->
