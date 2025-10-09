#!/bin/bash
# Script de inicio para QuickTable Control de Acceso

echo "=== QuickTable Control de Acceso ==="
echo "Iniciando sistema..."

# Verificar si el entorno virtual existe
if [ -d "quicktableenv" ]; then
    echo "Activando entorno virtual..."
    source quicktableenv/bin/activate
fi

# Verificar archivos necesarios
if [ ! -f "control_acceso.py" ]; then
    echo "Error: control_acceso.py no encontrado"
    exit 1
fi

if [ ! -f "quicktablerfid.py" ]; then
    echo "Error: quicktablerfid.py no encontrado"
    exit 1
fi

# Ejecutar aplicación
echo "Iniciando QuickTable Control de Acceso..."
python3 control_acceso.py

echo "Aplicación cerrada"
