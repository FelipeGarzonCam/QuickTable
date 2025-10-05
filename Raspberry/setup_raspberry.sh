#!/bin/bash
echo "=== Configuración QuickTable NFC ==="

# 1. Actualizar sistema
echo "📦 Actualizando sistema..."
sudo apt update && sudo apt upgrade -y

# 2. Instalar dependencias del sistema
echo "🔧 Instalando dependencias..."
sudo apt install python3-pip python3-venv python3-spidev python3-rpi.gpio git -y

# 3. Habilitar SPI
echo "🔌 Habilitando SPI..."
sudo raspi-config nonint do_spi 0

# 4. Crear entorno virtual
echo "🐍 Creando entorno virtual..."
python3 -m venv ~/quicktable_env
source ~/quicktable_env/bin/activate

# 5. Instalar MFRC522
echo "📡 Instalando librería MFRC522..."
pip install requests
git clone https://github.com/pimylifeup/MFRC522-python.git
cd MFRC522-python
python setup.py install
cd ..

# 6. Test rápido
echo "✅ Probando instalación..."
python -c "from mfrc522 import SimpleMFRC522; print('✅ MFRC522 instalado correctamente')"

echo "🎉 ¡Configuración completada!"
echo "Para usar la aplicación:"
echo "1. Activa el entorno: source ~/quicktable_env/bin/activate"
echo "2. Ejecuta: python raspberry_nfc_final.py"
echo "3. Para salir: deactivate"
