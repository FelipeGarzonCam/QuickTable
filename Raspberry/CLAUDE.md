# QuickTable — Contexto del Proyecto para Claude

## Qué es este proyecto

Sistema de control de asistencia con tarjetas NFC para una tesis universitaria (Universidad Santo Tomás). El sistema corre en una **Raspberry Pi** con un lector RC522 conectado por SPI. La Pi se comunica con un servidor web externo vía HTTP (actualmente inseguro — ver deuda técnica).

Interfaz gráfica construida con **Tkinter** en modo pantalla completa (800×480px), diseñada para pantallas táctiles pequeñas sin teclado físico. El usuario interactúa con teclados virtuales integrados en la UI.

---

## Arquitectura del sistema

```
[Servidor Web .NET/HTTP]  ←──────────────────────────────────────
         ↑                        requests (HTTP, sin TLS)
         │
[Raspberry Pi — NFC_quicktable.py]
   ├── QuickTableRFID        → maneja el hardware RC522 (SPI)
   ├── QuickTableControlAcceso  → app Tkinter principal
   └── AdminLTEKeyboard      → teclado numérico (DEFINIDO, NO USADO)
```

### Flujos principales
1. **Marcar Salida** → leer UID físico de tarjeta → POST al servidor
2. **Modo Empleado** → código sesión 6 dígitos → leer tarjeta NFC → asignar UID a empleado
3. **Modo TI** → código sesión → escribir UID en tarjeta → verificar escritura → confirmar con servidor
4. **Modo Admin (2FA)** → código sesión → leer tarjeta NFC (UID físico + texto escrito) → POST `/Login/Confirmar2FA`

### Endpoints que consume
| Endpoint | Método | Uso |
|---|---|---|
| `/api/health` | GET | Verificar conexión |
| `/api/tarjeta/validar-sesion` | POST | Validar sesión rol TI |
| `/api/tarjeta/validar-sesion-admin` | POST | Validar sesión rol Admin |
| `/api/tarjeta/validar-sesion-empleado` | POST | Validar sesión rol Empleado |
| `/api/tarjeta/asignar-empleado` | POST | Asignar tarjeta a empleado |
| `/api/tarjeta/confirmar` | POST | Confirmar tarjeta TI escrita |
| `/Login/Confirmar2FA` | POST | Autenticación 2FA Admin |

### Configuración
- `config.json` en el mismo directorio del script: `{ "server_url": "http://IP:5000" }`
- Puerto fijo: 5000 (oculto en UI pero configurable internamente)
- El widget de puerto existe en memoria aunque no se renderiza

---

## Dependencias

| Paquete | Propósito | Disponibilidad |
|---|---|---|
| `tkinter` | UI gráfica | Sistema Python |
| `requests` | HTTP al servidor | pip |
| `mfrc522` | Lector RC522 NFC | pip (solo Raspberry Pi) |
| `RPi.GPIO` | GPIO de la Pi | pip (solo Raspberry Pi) |

El código usa detección de hardware en runtime (`HARDWARE_AVAILABLE`). Si las librerías de Pi no están disponibles, la app corre en modo degradado sin NFC.

---

## Deuda técnica conocida (resultado de auditoría)

### Bugs críticos — deben resolverse antes de cualquier deploy
- **`proceso_marcar_salida` no existe**: método referenciado en línea 732 pero nunca definido. Crash garantizado al usar "MARCAR SALIDA".
- **`self.active_indicator` no existe**: referenciado en `on_config_key_press()` pero nunca creado. Crash al usar teclado táctil en config de servidor.
- **Tkinter multi-hilo**: labels de firma creados desde hilos secundarios (líneas 919, 1036, 1325). Tkinter no es thread-safe — usar siempre `root.after()`.

### Bugs funcionales confirmados
- **Botón `'.'` no funciona**: el texto del botón es `'.'` pero `on_config_key_press` busca `key == 'Punto'`. No se puede ingresar una IP válida con el teclado táctil.
- **`AdminLTEKeyboard`** está definida (líneas 197–242) pero nunca instanciada — código muerto.
- **Race condition** en `self.leyendo_2fa`: booleano compartido entre hilos sin `threading.Event` ni lock.
- **Thread leak**: hilos de lectura NFC continúan después de cambiar de pantalla.

### Deuda de seguridad
- Todo el tráfico va por HTTP sin cifrado (UIDs, códigos 2FA, datos de empleados).
- Sin validación de formato de IP ingresada (riesgo SSRF / URL injection).
- Sin rate limiting en validación de código de sesión (enumerable por fuerza bruta).
- `self.session_data` escrito desde hilos sin lock.
- Datos sensibles impresos a stdout con `print()` en producción.
- `bare except:` en `verificar_conexion_servidor()` oculta errores de seguridad.

### Deuda de calidad
- Teclado numérico implementado 3 veces (código duplicado).
- Magic strings/numbers: IP default `192.168.1.100`, puerto `5000`, longitud código `6`, timeouts.
- Label "By Felipe Garzon" repetida 6+ veces; debe ser un método `_mostrar_firma()`.
- `config.json` con ruta relativa al CWD, no al directorio del script.
- Mezcla de camelCase y snake_case en nombres de variables.

---

## Directrices de desarrollo para este proyecto

### Reglas de threading (crítico para Tkinter)
- **Nunca crear o modificar widgets desde un hilo secundario**. Siempre usar `self.root.after(0, lambda: ...)`.
- Para estado compartido entre hilos usar `threading.Event()` (no booleanos planos).
- Al cambiar de pantalla, señalizar al hilo activo para que se detenga antes de destruir widgets.
- Los hilos de lectura NFC deben chequear una señal de cancelación en cada iteración.

```python
# Correcto
self._stop_event = threading.Event()
self.root.after(0, lambda: self.label.config(text="..."))

# Incorrecto
self.running = True          # sin lock
self.label.config(text="...") # desde hilo secundario
```

### Manejo de errores
- Nunca usar `bare except:` — capturar siempre excepciones específicas (`requests.Timeout`, `requests.ConnectionError`, `json.JSONDecodeError`).
- Llamadas a `response.json()` siempre dentro de un bloque que maneje `json.JSONDecodeError`.
- Los errores de conexión deben mostrarse en la UI (via `root.after`), no silenciarse.

### Seguridad
- No agregar nuevos endpoints HTTP sin evaluar si el dato transmitido es sensible.
- No expandir el uso de `print()` para datos de autenticación, UIDs o tokens.
- Toda IP/URL ingresada por el usuario debe validarse con regex antes de usarse:
  ```python
  import re
  IP_RE = re.compile(r'^(\d{1,3}\.){3}\d{1,3}$')
  ```
- Si se migra a HTTPS, forzar `verify=True` explícitamente en cada llamada `requests`.

### Estructura de pantallas
- Cada pantalla destruye todos los widgets con `for w in self.root.winfo_children(): w.destroy()`.
- Antes de destruir, señalizar cualquier hilo activo que use widgets de esa pantalla.
- La firma "By Felipe Garzon" debe mostrarse con un método compartido `_mostrar_firma()`, no inline.

### Constantes — usar siempre, no magic values
Cuando se agreguen o modifiquen features, respetar/actualizar estas constantes (pendientes de centralizar):
```python
DEFAULT_SERVER_URL   = 'http://192.168.1.100:5000'
DEFAULT_PORT         = 5000
SESSION_CODE_LENGTH  = 6
MAX_IP_INPUT_LENGTH  = 15
NFC_READ_TIMEOUT     = 15   # segundos
NFC_WRITE_TIMEOUT    = 30
NFC_VERIFY_TIMEOUT   = 15
```

### UI Táctil (800×480)
- Diseño fijo para pantalla táctil de 800×480px en Raspberry Pi.
- Botones táctiles: mínimo 60px de alto, 90px de ancho para ser usables con dedos.
- No usar `pack()` y `place()` mezclados en el mismo contenedor.
- Los teclados numéricos para IP y para códigos de sesión tienen layouts distintos (el de IP incluye `'.'`; el de código solo dígitos + Borrar + Entrar).

### Convenciones de código
- Variables y métodos: **snake_case** (PEP 8).
- Parámetros JSON hacia el servidor: respetar el contrato existente (el servidor espera camelCase como `sessionCode`, `empleadoId`, `uidFisico`).
- No agregar comentarios que describan QUÉ hace el código; solo comentar el PORQUÉ cuando no es obvio.
- No dejar marcas de desarrollo en el código (`# INTACTO`, `# FIRMA:`, etc.).

---

## Archivos del proyecto

| Archivo | Propósito |
|---|---|
| `NFC_quicktable.py` | Aplicación principal (único archivo de lógica) |
| `config.json` | URL del servidor (persistencia entre reinicios) |
| `serverPython.py` | Servidor HTTP simple para servir archivos estáticos |
| `setup_raspberry.sh` | Script de instalación de dependencias en Raspberry Pi |
| `start_quicktable.sh` | Script de arranque de la aplicación |
| `quicktable-launcher.sh` | Lanzador alternativo |
| `QuickTableNFC.desktop` | Acceso directo del escritorio para la Pi |
| `test_rc522.py` | Script de diagnóstico del hardware RC522 |

---

## Entorno de ejecución destino

- **Hardware**: Raspberry Pi (cualquier modelo con SPI) + lector MFRC522 RC522
- **OS**: Raspberry Pi OS (Debian-based)
- **Python**: 3.11+ (hay pyc de 3.11 y 3.12 en `__pycache__`)
- **Display**: Pantalla táctil 800×480, modo fullscreen
- **Red**: LAN local, sin acceso a internet requerido
- **Servidor**: aplicación .NET en otra máquina de la misma red
