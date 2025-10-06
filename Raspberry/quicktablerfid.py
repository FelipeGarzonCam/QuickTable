#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
QuickTable RFID - Módulo compartido para manejo de tarjetas NFC
Clase extraída del sistema existente para reutilización
"""

try:
    from mfrc522 import SimpleMFRC522
    import RPi.GPIO as GPIO
    HARDWARE_AVAILABLE = True
except ImportError:
    print("Librerías de RC522 no disponibles")
    HARDWARE_AVAILABLE = False
    SimpleMFRC522 = None
    GPIO = None

import time

class QuickTableRFID:
    """Clase para manejo de tarjetas NFC RC522"""
    
    def __init__(self):
        if not HARDWARE_AVAILABLE:
            raise ImportError("Hardware RC522 no disponible")
        
        self.reader = SimpleMFRC522()
        GPIO.setwarnings(False)
        print("RC522 inicializado correctamente")
    
    def read_card_uid(self, timeout=15):
        """
        Leer solo el UID físico de la tarjeta (para control de asistencia)
        
        Args:
            timeout (int): Tiempo máximo de espera en segundos
            
        Returns:
            str: UID de la tarjeta o None si no se detecta
        """
        if not HARDWARE_AVAILABLE:
            return None
        
        print(f"Esperando tarjeta por {timeout} segundos...")
        start_time = time.time()
        
        try:
            while time.time() - start_time < timeout:
                try:
                    # Intentar leer la tarjeta
                    id, text = self.reader.read_no_block()
                    
                    if id:
                        uid_str = str(id)
                        print(f"UID detectado: {uid_str}")
                        return uid_str
                    
                    time.sleep(0.1)  # Pequeña pausa para no saturar
                    
                except Exception as e:
                    print(f"Error leyendo tarjeta: {e}")
                    time.sleep(0.5)
            
            print("Timeout: No se detectó tarjeta")
            return None
            
        except Exception as e:
            print(f"Error en read_card_uid: {e}")
            return None
    
    def read_text_from_card(self, timeout=15):
        """
        Leer tanto el UID físico como el texto escrito en la tarjeta (para admin 2FA)
        
        Args:
            timeout (int): Tiempo máximo de espera en segundos
            
        Returns:
            tuple: (uid_fisico, texto_escrito) o (None, None) si falla
        """
        if not HARDWARE_AVAILABLE:
            return None, None
        
        print(f"Leyendo tarjeta completa por {timeout} segundos...")
        start_time = time.time()
        
        try:
            while time.time() - start_time < timeout:
                try:
                    # Leer tarjeta completa
                    id, text = self.reader.read_no_block()
                    
                    if id:
                        uid_str = str(id)
                        text_clean = text.strip() if text else ""
                        
                        print(f"UID físico: {uid_str}")
                        print(f"Texto escrito: {text_clean[:20]}..." if len(text_clean) > 20 else f"Texto escrito: {text_clean}")
                        
                        return uid_str, text_clean
                    
                    time.sleep(0.1)
                    
                except Exception as e:
                    print(f"Error leyendo tarjeta completa: {e}")
                    time.sleep(0.5)
            
            print("Timeout: No se pudo leer tarjeta completa")
            return None, None
            
        except Exception as e:
            print(f"Error en read_text_from_card: {e}")
            return None, None
    
    def clear_and_write_text(self, text, timeout=30):
        """
        Borrar y escribir texto en la tarjeta (para crear tarjetas admin)
        
        Args:
            text (str): Texto a escribir
            timeout (int): Tiempo máximo de espera
            
        Returns:
            bool: True si la escritura fue exitosa
        """
        if not HARDWARE_AVAILABLE:
            return False
        
        print(f"Escribiendo texto en tarjeta: '{text[:50]}...'")
        
        try:
            # La librería SimpleMFRC522 limpia automáticamente antes de escribir
            self.reader.write(text)
            print("✓ Escritura completada exitosamente")
            return True
            
        except Exception as e:
            print(f"✗ Error escribiendo en tarjeta: {e}")
            return False
    
    def cleanup(self):
        """Limpiar recursos GPIO"""
        if HARDWARE_AVAILABLE and GPIO:
            try:
                GPIO.cleanup()
                print("GPIO limpiado correctamente")
            except Exception as e:
                print(f"Error limpiando GPIO: {e}")

# Función de prueba
if __name__ == "__main__":
    try:
        print("=== Prueba QuickTableRFID ===")
        
        if not HARDWARE_AVAILABLE:
            print("Hardware no disponible para pruebas")
            exit(1)
        
        rfid = QuickTableRFID()
        
        print("\n1. Probando lectura de UID únicamente:")
        print("Acerca una tarjeta...")
        uid = rfid.read_card_uid(timeout=10)
        if uid:
            print(f"✓ UID leído: {uid}")
        else:
            print("✗ No se pudo leer UID")
        
        print("\n2. Probando lectura completa (UID + texto):")
        print("Acerca una tarjeta...")
        uid, text = rfid.read_text_from_card(timeout=10)
        if uid:
            print(f"✓ UID físico: {uid}")
            print(f"✓ Texto: {text}")
        else:
            print("✗ No se pudo leer tarjeta completa")
        
        rfid.cleanup()
        
    except Exception as e:
        print(f"Error en prueba: {e}")
