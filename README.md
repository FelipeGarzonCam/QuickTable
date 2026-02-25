
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


# Guia Completa: Raspberry Pi + Lector NFC QuickTable

## Hardware necesario

- Raspberry Pi 3 o 4
- Modulo RFID-RC522
- Pantalla tactil LCD 5"
- Tarjetas NFC Mifare
- La Raspberry y el servidor QuickTable deben estar en la **misma red local**

***

## 1. Conexion del MFRC522

| MFRC522 | GPIO Raspberry | Pin fisico |
| :-- | :-- | :-- |
| SDA | GPIO 8 (CE0) | 24 |
| SCK | GPIO 11 | 23 |
| MOSI | GPIO 10 | 19 |
| MISO | GPIO 9 | 21 |
| RST | GPIO 25 | 22 |
| GND | GND | 6 |
| VCC | **3.3V** | 1 |

> Nunca conectar VCC a 5V, dana el modulo.

***

## 2. Preparar el sistema

```bash
sudo apt update && sudo apt upgrade -y
```


***

## 3. Habilitar SPI

```bash
sudo raspi-config
# Interface Options > SPI > Yes
sudo reboot
```

Verificar que quedo activo:

```bash
ls /dev/spi*
# Debe mostrar: /dev/spidev0.0  /dev/spidev0.1
```


***

## 4. Instalar dependencias del sistema

```bash
sudo apt install python3 python3-pip python3-tk python3-venv git -y
```


***

## 5. El codigo Esta en el repositorio

cd QuickTableTI/Raspberry

***

## 6. Crear el entorno virtual e instalar librerias

```bash
python3 -m venv venv
source venv/bin/activate
pip install mfrc522 requests
```


***

## 7. Configurar la IP del servidor QuickTable

En el archivo de configuracion del script, reemplazar con la IP real del PC donde corre la app .NET:

```bash
# En el PC servidor (Windows), ejecutar:
ipconfig
# Buscar la IP bajo "Adaptador Wi-Fi" o "Ethernet"
```

Luego editar el script con esa IP:

```bash
nano ~/QuickTableTI/Raspberry/lectorNFC.py
# Cambiar la linea: SERVIDOR_URL = "http://192.168.X.X:5000"
```


***

## 8. Verificar que todo funciona

```bash
source ~/QuickTableTI/Raspberry/venv/bin/activate
python3 -c "import mfrc522; import requests; import tkinter; print('OK')"
```

Si imprime `OK`, el entorno esta listo.

***

## 9. Autoarranque al encender la Raspberry

```bash
mkdir -p ~/.config/autostart
nano ~/.config/autostart/quicktable.desktop
```

Pegar este contenido:

```ini
[Desktop Entry]
Type=Application
Name=QuickTable NFC
Exec=/home/pi/QuickTableTI/Raspberry/venv/bin/python3 /home/pi/QuickTableTI/Raspberry/lectorNFC.py
X-GNOME-Autostart-enabled=true
```


***

## 10. Ejecutar manualmente (para pruebas)

```bash
source ~/QuickTableTI/Raspberry/venv/bin/activate
python3 ~/QuickTableTI/Raspberry/lectorNFC.py
```


***

> Asegurarse de que el servidor QuickTable (.NET) este corriendo antes de encender la Raspberry, de lo contrario el script no podra conectarse a la API en `http://{IP}:5000/api/asistencia/marcar-salida`.


