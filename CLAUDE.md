# QuickTable — Guía de desarrollo para Claude

Proyecto de tesis universitaria (Universidad Santo Tomás). Sistema de gestión de restaurante con control de asistencia por NFC. Desplegado en red LAN local, sin exposición a internet.

---

## Estructura del repositorio

```
QuickTable/
├── CLAUDE.md                              ← Este archivo
├── QuickTableProyect/                     ← ASP.NET Core 8, servidor principal
│   ├── Program.cs                         ← DI, middleware, sesiones, seguridad
│   ├── appsettings.json                   ← Conexión DB, configuración backups
│   ├── Controllers/
│   │   ├── LoginController.cs             ← Auth, 2FA, QR generation
│   │   ├── HomeController.cs              ← Home, session keep-alive
│   │   ├── MeseroController.cs            ← Gestión de pedidos (mesero)
│   │   ├── CocinaController.cs            ← Preparación de pedidos (cocina)
│   │   ├── CajaController.cs              ← Finalización y pago (cajero)
│   │   ├── AdministradorController.cs     ← Gestión completa + reportes
│   │   └── TIController.cs                ← Gestión usuarios y tarjetas NFC
│   ├── Api/
│   │   ├── AsistenciaApiController.cs     ← POST marcar salida NFC
│   │   ├── TarjetaApiController.cs        ← Gestión tarjetas (pendiente/confirmar/asignar)
│   │   └── BackupApiController.cs         ← CRUD backups SQL Server
│   ├── Infrastructure/
│   │   └── DatabaseInitializer.cs         ← Crea usuario TI por defecto al arrancar
│   └── Views/                             ← Razor Pages (.cshtml) por rol
├── QuickTableProyect.Aplicacion/          ← Capa de negocio (servicios)
│   ├── PedidoService.cs                   ← CRUD pedidos, transiciones de estado
│   ├── MenuService.cs                     ← CRUD menú
│   ├── EmpleadoService.cs                 ← CRUD empleados
│   ├── HistorialPedidoService.cs          ← Historial con paginación
│   ├── RegistroSesionService.cs           ← Logs de sesión, jornadas diarias
│   ├── AsistenciaService.cs               ← Marcar salida con tarjeta NFC
│   ├── TarjetaNFCService.cs               ← Asignación/revocación de tarjetas
│   ├── CryptoService.cs                   ← AES-256 para UIDs de empleados
│   ├── PasswordService.cs                 ← BCrypt hash/verify
│   └── BackupService.cs                   ← Backup/restore SQL Server
├── QuickTableProyect.Dominio/             ← Modelos de dominio
│   ├── Empleado.cs                        ← Usuario con rol y UID tarjeta cifrado
│   ├── MenuItem.cs                        ← Item de menú (nombre, precio, categoría)
│   ├── PedidosActivos.cs                  ← Pedido activo con máquina de estado
│   ├── ItemDetalle.cs                     ← Línea de pedido con estado preparación
│   ├── HistorialPedido.cs                 ← Pedido archivado con métricas de tiempo
│   ├── RegistroSesion.cs                  ← Log sesión empleado
│   ├── Codigo2FA.cs                       ← Código 2FA temporal (10 min, guid+code)
│   ├── TarjetaRC.cs                       ← Tarjeta admin NFC (UID físico + escrito)
│   └── HistorialBackup.cs                 ← Auditoría de backups
├── QuickTableProyect.Persistencia/        ← EF6 DbContext + Migrations
│   ├── SistemaQuickTableContext.cs        ← Sin pluralización de tablas
│   └── Migrations/202602111843108_PrimeraNOBORRAR.cs
└── Raspberry/
    └── NFC_quicktable.py                  ← App Python completa para Raspberry Pi
```

---

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 8, Razor Pages |
| ORM | Entity Framework 6 |
| Base de datos | SQL Server (local) |
| Autenticación | Sesiones + 2FA con tarjeta NFC |
| Cifrado contraseñas | BCrypt work factor 12 |
| Cifrado UIDs | AES-256 CBC + IV aleatorio + Base64 |
| Frontend | AdminLTE (Bootstrap), AJAX/jQuery |
| Raspberry Pi | Python 3, Tkinter 800×480, MFRC522 SPI |
| Reportes | EPPlus (Excel), QRCoder |

---

## Roles del sistema

| Rol | Acceso |
|-----|--------|
| `Mesero` | Crear/editar pedidos |
| `Cocina` | Preparar pedidos, marcar listos |
| `Cajero` | Finalizar pedidos, cobrar |
| `Admin` | Gestión completa + 2FA obligatorio con tarjeta NFC |
| `TI` | Gestión usuarios, tarjetas, backups |

---

## Flujos clave

### Autenticación y 2FA (Admin)
```
1. Admin ingresa credenciales → LoginController.Autenticar()
2. Genera código 6 dígitos + Guid → guarda en Codigo2FA (expira 10 min)
3. JS modal hace polling a Check2FA()
4. Raspberry Pi lee UID físico + texto escrito en tarjeta
5. POST /Login/Confirmar2FA {navId, uidFisico, textoEscrito}
6. Valida contra TarjetasRC + Codigo2FA
7. Session["2FACompletado"] = "true" → redirige a /Administrador
```

### Ciclo de vida de un pedido
```
Mesero → CrearPedido() → Estado: "EnPreparacion"
Cocina → MarcarPedidoListo() → CocinaListoAt = now → Estado: "Listo"
Mesero → MarcarPedidoComoAceptado() → MeseroAceptadoAt = now
Cajero → FinalizarPedido() → crea HistorialPedido → elimina PedidosActivos
```

### Marcar salida NFC (Raspberry → API)
```
Raspberry lee UID → codifica Base64(decimal)
POST /api/asistencia/marcar-salida
AsistenciaService: decodifica → hex → busca Empleado.TarjetaUID
Actualiza último RegistroSesion activo: FechaHoraDesconexion, MarcoTarjetaSalida = true
```

### Asignación tarjeta empleado
```
TI genera código sesión 6 dígitos → Raspberry con modo empleado
Usuario ingresa código en Raspberry → POST /api/tarjeta/validar-sesion
Raspberry lee UID tarjeta → POST /api/tarjeta/asignar-empleado
Server cifra UID con AES → guarda en Empleado.TarjetaUID
```

### Backup/Restore
```
Restore: carga HistorialBackup en memoria → cierra EF → SINGLE_USER
         RESTORE DATABASE → poll estado DB (2s, max 10 intentos)
         Recrea tabla HistorialBackup → re-inserta → MULTI_USER
```

---

## Modelos de dominio importantes

### PedidosActivos
- Estado: `"EnPreparacion"` | `"Editado"` | `"Listo"` | `"Finalizado"`
- Métricas: `FechaCreacion`, `CocinaListoAt`, `MeseroAceptadoAt`

### TarjetaRC (tarjeta Admin 2FA)
- `Uid`: UID escrito en el chip (para validación 2FA)
- `UidFisico`: UID físico del chip (para identificar tarjeta)
- `Activa`: bool
- `CodigoSesion`: código sesión asignación TI

### Codigo2FA
- `Codigo`: 6 dígitos
- `NavId`: Guid para correlacionar polling JS
- `Confirmado`: bool
- `FechaExpiracion`: now + 10 min

### RegistroSesion
- `FechaHoraConexion`, `FechaHoraDesconexion`
- `MarcoTarjetaSalida`: bool (true = salió con NFC)
- `TiempoTrabajado`: calculado al cerrar sesión

---

## Configuración y constantes

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=QuickTableProyectDB;Trusted_Connection=True;"
  },
  "BackupSettings": {
    "BackupPath": "C:\\QuickTableBackups\\",
    "MaxBackupSizeMB": 500,
    "RetentionDays": 30
  }
}
```

**Constantes hardcodeadas (NO modificar sin evaluar impacto):**
- IVA: `0.19m` (19%)
- Puerto servidor: `5000`
- BCrypt work factor: `12`
- Longitud código 2FA: `6 dígitos`
- Expiración 2FA: `10 minutos`
- Timeout sesión idle: `8 horas` (cookie), `18 horas` (desconexión automática)
- Clave AES: `"QuickTable2024SecureKey123456!"` en `CryptoService.cs`
- Password TI default: `"12345"` en `DatabaseInitializer.cs`
- Cookie sesión: `.QuickTable.Session`

**Raspberry Pi (`config.json`):**
```json
{ "server_url": "http://192.168.1.100:5000" }
```

---

## Arquitectura Raspberry Pi (`NFC_quicktable.py`)

### Clases principales
- **`QuickTableRFID`**: Abstracción hardware MFRC522
  - `read_card_uid()`: solo UID físico (marcar salida empleado)
  - `read_text_from_card()`: UID físico + texto escrito (2FA admin)
  - `clear_and_write_text()`: escribe UID en tarjeta en blanco (modo TI)
  - `cleanup()`: libera GPIO

- **`QuickTableControlAcceso`**: App Tkinter 800×480 fullscreen
  - Teclado numérico táctil
  - 4 modos de operación

### Modos de operación
| Modo | Trigger | Flujo |
|------|---------|-------|
| Marcar Salida | Pantalla principal | Lee UID → POST `/api/asistencia/marcar-salida` |
| 2FA Admin | Selección rol Admin | Código sesión → lee tarjeta (UID+texto) → POST `/Login/Confirmar2FA` |
| TI Card Creation | Modo TI | Obtiene UID pendiente → escribe chip → POST `/api/tarjeta/confirmar` |
| Asignar Empleado | Modo Empleado | Código sesión → lee UID → POST `/api/tarjeta/asignar-empleado` |

### Comunicación con servidor
- UIDs se envían como `Base64(str(decimal_uid))`
- Códigos sesión: 6 dígitos numéricos
- Timeout requests: 15–30 segundos según operación
- Toda respuesta JSON debe manejar `JSONDecodeError`

---

## Directrices de desarrollo

### Tkinter / Threading (CRÍTICO)
- **NUNCA** crear o modificar widgets Tkinter desde hilos secundarios
- Siempre usar `self.root.after(0, lambda: ...)` para updates de UI desde hilos
- Estado compartido entre hilos: usar `threading.Event()`, no booleanos planos
- Al cambiar pantalla: señalizar hilo activo con Event **antes** de destruir widgets

### Manejo de errores
- Nunca `bare except:` — siempre excepciones específicas
- `response.json()` siempre con manejo de `json.JSONDecodeError`
- Errores de conexión mostrar en UI via `root.after`, no silenciar ni solo imprimir

### Constantes — no magic values
- IP/puerto, longitudes de código, timeouts → deben ser constantes nombradas al inicio
- No repetir strings de URL o valores numéricos inline

### Firma visual
- No duplicar `tk.Label(... "By Felipe Garzon" ...).place(...)` inline
- Usar método `_mostrar_firma()` una vez por pantalla

### Validación de IP
- Validar con regex antes de construir URL: `re.compile(r'^(\d{1,3}\.){3}\d{1,3}$')`
- Aplicar en `probar_conexion()` y `conectar_servidor()`

### .NET / C#
- Autorización: validar rol en sesión al inicio de cada acción de controlador
- API endpoints: verificar sesión activa antes de operar
- Nunca exponer mensajes de excepción internos al usuario
- Métricas de tiempo: calcular solo en `FinalizarPedido()`, nunca recalcular historial

---

## Bugs conocidos (pendientes de fix)

| Archivo | Línea aprox. | Bug | Severidad |
|---------|-------------|-----|-----------|
| `NFC_quicktable.py` | 732 | `proceso_marcar_salida` no definido → crash al usar "MARCAR SALIDA" | Crítica |
| `NFC_quicktable.py` | 505 | `self.active_indicator` no definido → crash en teclado config servidor | Crítica |
| `NFC_quicktable.py` | 919, 1036, 1325 | Widgets Tkinter creados desde hilos secundarios → inestabilidad | Alta |
| `NFC_quicktable.py` | `on_config_key_press` | Botón `'.'` no inserta punto (lógica incorrecta) | Media |
| `NFC_quicktable.py` | — | `AdminLTEKeyboard` definida pero nunca usada (código muerto) | Baja |

---

## Deuda de seguridad conocida

**Crítica:**
- Tráfico HTTP sin cifrado en LAN (UIDs, códigos 2FA, navId)
- Sin validación de IP en pantalla config → SSRF / URL injection

**Alta:**
- Sin rate limiting en validación de código sesión (6 dígitos = 1M combinaciones, enumerable)
- Race condition en `self.leyendo_2fa` (bool compartido sin lock entre hilos)
- Datos sensibles (UIDs, códigos, respuestas servidor) impresos con `print()` a stdout

**Media:**
- `bare except:` en `verificar_conexion_servidor()` oculta errores SSL
- `self.session_data` escrito desde hilos sin sincronización
- Clave AES hardcodeada en `CryptoService.cs`
- Password TI default `"12345"` sin forzar cambio en UI

**Contexto:** Red LAN local sin internet. Riesgo principal: acceso físico malicioso + MITM en red local. No agregar nuevos endpoints sin evaluar exposición.

---

## Notas de despliegue

**Servidor .NET:**
- Kestrel, puerto 5000 fijo
- Requiere SQL Server local o en red
- Windows-only (DPAPI, rutas `C:\`)
- Auto-crea usuario TI con password `"12345"` al arrancar

**Raspberry Pi:**
- Python 3.7+, libs: `mfrc522`, `requests`
- MFRC522 por SPI
- Resolución: 800×480 (pantalla 5" táctil)
- Config servidor en `Raspberry/config.json`

**Base de datos:**
- Schema: EF6 Migrations (no borrar `202602111843108_PrimeraNOBORRAR`)
- Sin datos semilla de menú o empleados (excepto usuario TI)
