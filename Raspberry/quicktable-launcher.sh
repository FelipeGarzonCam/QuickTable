#!/bin/bash

# Cambiar al directorio del proyecto
cd /home/pi/QuickTable || exit 1

# Activar el entorno virtual
source quicktable-env/bin/activate

# Ejecutar el script de Python
python3 NFC_quicktable.py

# Desactivar entorno al cerrar
deactivate

# Mantener la terminal abierta en caso de error
read -p "Presiona Enter para cerrar..."
