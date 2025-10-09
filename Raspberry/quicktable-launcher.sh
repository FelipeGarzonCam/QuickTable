#!/bin/bash

# Cambiar al directorio del proyecto
cd /home/pi/QuickTable/Raspberry || cd /home/pi/QuickTable

# Activar el entorno virtual con el nombre correcto
source quicktable-env/bin/activate

# Ejecutar el script de Python correcto
python3 control_acceso.py

# Mantener la terminal abierta en caso de error
read -p "Presiona Enter para cerrar..."
