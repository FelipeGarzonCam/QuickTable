#!/bin/bash

echo "====================================="
echo "QuickTable NFC - Instalacion Completa"
echo "====================================="

# 1. Actualizar el sistema
echo ""
echo "Paso 1: Actualizando el sistema..."
sudo apt update && sudo apt upgrade -y

# 2. Instalar dependencias del sistema
echo ""
echo "Paso 2: Instalando dependencias del sistema..."
sudo apt install -y python3-pip python3-venv python3-dev python3-tk git

# 3. Instalar librerias para SPI
echo ""
echo "Paso 3: Instalando librerias SPI..."
sudo apt install -y python3-spidev python3-rpi.gpio

# 4. Habilitar interfaz SPI
echo ""
echo "Paso 4: Habilitando interfaz SPI..."
sudo raspi-config nonint do_spi 0

# 5. Crear directorio del proyecto
echo ""
echo "Paso 5: Creando directorio del proyecto..."
cd ~
mkdir -p QuickTable
cd QuickTable

# 6. Crear entorno virtual
echo ""
echo "Paso 6: Creando entorno virtual..."
python3 -m venv quicktable-env

# 7. Activar entorno virtual e instalar paquetes Python
echo ""
echo "Paso 7: Instalando paquetes Python..."
source quicktable-env/bin/activate

pip install --upgrade pip
pip install requests

# 8. Instalar libreria MFRC522
echo ""
echo "Paso 8: Instalando libreria MFRC522..."
pip install mfrc522

# Si falla, intentar con el repositorio
if [ $? -ne 0 ]; then
    echo "Intentando instalacion alternativa de MFRC522..."
    cd ~
    git clone https://github.com/pimylifeup/MFRC522-python.git
    cd MFRC522-python
    python3 setup.py install
    cd ~/QuickTable
fi

# 9. Verificar instalacion
echo ""
echo "Paso 9: Verificando instalacion..."
python3 -c "import tkinter; print('Tkinter: OK')"
python3 -c "import requests; print('Requests: OK')"
python3 -c "from mfrc522 import SimpleMFRC522; print('MFRC522: OK')"

echo ""
echo "====================================="
echo "Instalacion completada exitosamente"
echo "====================================="
echo ""
echo "Pasos siguientes:"
echo "1. Copia el archivo NFC_quicktable.py a ~/QuickTable/"
echo "2. Ejecuta: cd ~/QuickTable"
echo "3. Crea el acceso directo con el siguiente comando"
echo ""

deactivate
