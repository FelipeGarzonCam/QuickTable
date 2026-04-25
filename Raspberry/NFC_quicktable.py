#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
QuickTable Control de Acceso UNIFICADO
Sistema completo de control de asistencia con tarjetas NFC
Incluye: Control de Acceso, Modo Admin, Modo TI y Modo Empleado
Version: UNIFICADO - Sin dependencias externas
"""

import tkinter as tk
from tkinter import messagebox, font
import requests
import threading
import time
import json
import os
import sys
import base64

# ============================================================================
# SECCION 1: DETECCION DE HARDWARE RC522
# ============================================================================
try:
    from mfrc522 import SimpleMFRC522
    import RPi.GPIO as GPIO
    GPIO.setwarnings(False)
    HARDWARE_AVAILABLE = True
    print("Hardware RC522 detectado y disponible")
except ImportError as e:
    HARDWARE_AVAILABLE = False
    SimpleMFRC522 = None
    GPIO = None
    print(f"Hardware RC522 no disponible: {e}")


# ============================================================================
# SECCION 2: CLASE QUICKTABLERFID (Manejo de hardware NFC)
# ============================================================================
class QuickTableRFID:
    """Clase para manejo de tarjetas NFC RC522"""
    
    def __init__(self):
        if not HARDWARE_AVAILABLE:
            raise ImportError("Hardware RC522 no disponible")
        
        self.reader = SimpleMFRC522()
        GPIO.setwarnings(False)
        print("SimpleMFRC522 inicializado correctamente")
    
    def read_card_uid(self, timeout=15):
        """
        Leer solo el UID fisico de la tarjeta (para control de asistencia)
        
        Args:
            timeout (int): Tiempo maximo de espera en segundos
            
        Returns:
            str: UID de la tarjeta en formato hexadecimal o None si no se detecta
        """
        if not HARDWARE_AVAILABLE:
            return None
        
        print(f"Buscando tarjeta (timeout: {timeout}s)...")
        start_time = time.time()
        
        try:
            while (time.time() - start_time) < timeout:
                try:
                    id, text = self.reader.read_no_block()
                    
                    if id:
                        uid_string = f"{id:016X}"
                        print(f"Tarjeta detectada: {uid_string}")
                        return uid_string
                    
                    time.sleep(0.1)
                    
                except Exception as e:
                    if "No card" not in str(e):
                        print(f"Error lectura: {e}")
                    time.sleep(0.5)
            
            print("Timeout - No se detecto tarjeta")
            return None
            
        except Exception as e:
            print(f"Error en read_card_uid: {e}")
            return None
    
    def read_text_from_card(self, timeout=15):
        """
        Leer tanto el UID fisico como el texto escrito en la tarjeta (para admin 2FA)
        
        Args:
            timeout (int): Tiempo maximo de espera en segundos
            
        Returns:
            tuple: (uid_fisico, texto_escrito) o (None, None) si falla
        """
        if not HARDWARE_AVAILABLE:
            return None, None
        
        print(f"Leyendo tarjeta completa por {timeout} segundos...")
        start_time = time.time()
        
        try:
            while (time.time() - start_time) < timeout:
                try:
                    id, text = self.reader.read_no_block()
                    
                    if id:
                        uid_string = f"{id:016X}"
                        text_clean = (text or "").strip()
                        
                        print(f"UID fisico: {uid_string}")
                        print(f"Texto escrito: '{text_clean}'")
                        
                        return uid_string, text_clean
                    
                    time.sleep(0.1)
                    
                except Exception as e:
                    if "No card" not in str(e):
                        print(f"Error leyendo tarjeta completa: {e}")
                    time.sleep(0.5)
            
            print("Timeout - No se pudo leer tarjeta completa")
            return None, None
            
        except Exception as e:
            print(f"Error en read_text_from_card: {e}")
            return None, None
    
    def clear_and_write_text(self, text, timeout=30):
        """
        Borrar y escribir texto en la tarjeta (para crear tarjetas admin)
        
        Args:
            text (str): Texto a escribir
            timeout (int): Tiempo maximo de espera
            
        Returns:
            bool: True si la escritura fue exitosa
        """
        if not HARDWARE_AVAILABLE:
            return False
        
        print(f"Borrando tarjeta y escribiendo: '{text}'")
        
        try:
            write_complete = threading.Event()
            write_success = [False]
            error_msg = [None]
            
            def write_thread():
                try:
                    print("Borrando contenido anterior...")
                    clear_text = " " * 48
                    self.reader.write(clear_text)
                    time.sleep(0.5)
                    
                    print("Escribiendo nuevo contenido...")
                    self.reader.write(text)
                    
                    write_success[0] = True
                    write_complete.set()
                except Exception as e:
                    error_msg[0] = str(e)
                    write_complete.set()
            
            thread = threading.Thread(target=write_thread, daemon=True)
            thread.start()
            
            if write_complete.wait(timeout=timeout):
                if write_success[0]:
                    print("Borrado y escritura completados")
                    return True
                else:
                    print(f"Error: {error_msg[0]}")
                    return False
            else:
                print("Timeout en operacion")
                return False
                
        except Exception as e:
            print(f"Error critico escribiendo: {e}")
            return False
    
    def cleanup(self):
        """Limpiar recursos GPIO"""
        if HARDWARE_AVAILABLE and GPIO:
            try:
                GPIO.cleanup()
                print("GPIO limpiado correctamente")
            except Exception as e:
                print(f"Error limpiando GPIO: {e}")


# ============================================================================
# SECCION 3: CLASE ADMINLTEKEYBOARD (Teclado tactil numerico)
# ============================================================================
class AdminLTEKeyboard:
    """Teclado numerico tactil para entrada de codigos"""
    
    def __init__(self, parent, entry_widget, app_instance):
        self.parent = parent
        self.entry = entry_widget
        self.app = app_instance
        self.create_keyboard()
    
    def create_keyboard(self):
            # Le damos un tamaño exacto de 300x250 pixeles y evitamos que se expanda
            keyboard_frame = tk.Frame(self.parent, bg='#343a40', width=300, height=250)
            keyboard_frame.pack(pady=20)
            keyboard_frame.pack_propagate(False) 
            
            buttons = [
                ['1', '2', '3'],
                ['4', '5', '6'],
                ['7', '8', '9'],
                ['Borrar', '0', 'Entrar']
            ]
            
            for i, row in enumerate(buttons):
                for j, btn_text in enumerate(row):
                    if btn_text == 'Entrar': bg_color = '#007bff'
                    elif btn_text == 'Borrar': bg_color = '#dc3545'
                    else: bg_color = '#6c757d'
                    
                    btn = tk.Button(keyboard_frame, text=btn_text, font=('Arial', 14, 'bold'), 
                                    bg=bg_color, fg='white', border=0,
                                    command=lambda x=btn_text: self.on_key_press(x))
                    # Dibujamos cada boton matematicamente (90x50 pixeles)
                    btn.place(x=j*95 + 10, y=i*60 + 5, width=90, height=50)
    
    def on_key_press(self, key):
        current = self.entry.get()
        
        if key == 'Borrar':
            if current:
                self.entry.delete(len(current)-1, tk.END)
        elif key == 'Entrar':
            if hasattr(self.app, 'validar_codigo_sesion'):
                self.app.validar_codigo_sesion()
        elif key.isdigit():
            if len(current) < 15:
                self.entry.insert(tk.END, key)


# ============================================================================
# SECCION 4: CLASE PRINCIPAL QUICKTABLECONTROLACCESO
# ============================================================================
class QuickTableControlAcceso:
    """Aplicacion principal unificada de control de acceso"""
    
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("QuickTable - Control de Acceso")
        self.root.geometry("800x480")
        self.root.configure(bg='#212529')
        
        # Configurar pantalla completa
        self.root.attributes('-fullscreen', True)
        self.root.bind('<Escape>', self.exit_fullscreen)
        self.root.bind('<F11>', self.toggle_fullscreen)
        
        # Variables de instancia
        self.hardware_available = HARDWARE_AVAILABLE
        self.active_entry_type = 'ip'
        self.session_data = {}
        
        # Inicializar hardware NFC
        self.reader = None
        if self.hardware_available:
            try:
                self.reader = QuickTableRFID()
                print("Hardware RC522 inicializado correctamente")
            except Exception as e:
                print(f"Error inicializando RC522: {e}")
                self.hardware_available = False
        
        # Cargar configuracion
        self.cargar_configuracion()
        
        # Comprobar conexion al servidor antes de mostrar interfaz
        self.comprobar_conexion_inicial()
    
    def exit_fullscreen(self, event=None):
        """Salir de pantalla completa"""
        self.root.attributes('-fullscreen', False)
    
    def toggle_fullscreen(self, event=None):
        """Alternar pantalla completa"""
        current = self.root.attributes('-fullscreen')
        self.root.attributes('-fullscreen', not current)
    
    def cargar_configuracion(self):
        """Cargar configuracion del servidor"""
        try:
            if os.path.exists('config.json'):
                with open('config.json', 'r', encoding='utf-8') as f:
                    config = json.load(f)
                    self.server_url = config.get('server_url', 'http://192.168.1.100:5000')
            else:
                self.server_url = 'http://192.168.1.100:5000'
                self.guardar_configuracion()
        except Exception as e:
            print(f"Error cargando configuracion: {e}")
            self.server_url = 'http://192.168.1.100:5000'
    
    def guardar_configuracion(self):
        """Guardar configuracion actual"""
        try:
            config = {'server_url': self.server_url}
            with open('config.json', 'w', encoding='utf-8') as f:
                json.dump(config, f, indent=2)
        except Exception as e:
            print(f"Error guardando configuracion: {e}")
    
    def comprobar_conexion_inicial(self):
        """Comprobar conexion al servidor al iniciar"""
        if self.verificar_conexion_servidor():
            self.mostrar_pantalla_principal()
        else:
            self.mostrar_configuracion_servidor()
    
    def verificar_conexion_servidor(self):
        """Verificar si hay conexion con el servidor"""
        try:
            response = requests.get(f'{self.server_url}/api/health', timeout=3)
            return response.status_code == 200
        except:
            try:
                response = requests.get(self.server_url, timeout=3)
                return response.status_code in [200, 404, 403]
            except:
                return False
    
    # ========================================================================
    # PANTALLA DE CONFIGURACION DEL SERVIDOR
    # ========================================================================
 
    def mostrar_configuracion_servidor(self):
        """Pantalla TACTIL para configurar servidor - IP (Puerto fijo oculto)"""
        for widget in self.root.winfo_children():
            widget.destroy()
        
        main_frame = tk.Frame(self.root, bg='#212529')
        main_frame.pack(fill='both', expand=True, padx=20, pady=20)
        
        title_frame = tk.Frame(main_frame, bg='#212529')
        title_frame.pack(fill='x', pady=(0, 20))
        
        tk.Label(
            title_frame,
            text="QuickTable - Configuración IP",
            font=('Arial', 24, 'bold'),
            fg='#007bff',
            bg='#212529'
        ).pack()
        
        content_frame = tk.Frame(main_frame, bg='#212529')
        content_frame.pack(fill='both', expand=True)
        
        # COLUMNA IZQUIERDA - FORMULARIOS
        left_frame = tk.Frame(content_frame, bg='#495057', relief='raised', bd=2)
        left_frame.pack(side='left', fill='both', expand=True, padx=(0, 10))       
       
        
        # Estado hardware
        if self.hardware_available and self.reader:
            status_text = "RC522 Conectado"
            status_color = '#28a745'
        else:
            status_text = "RC522 No Disponible"
            status_color = '#dc3545'
        
        tk.Label(
            left_frame,
            text=status_text,
            font=('Arial', 11, 'bold'),
            fg=status_color,
            bg='#495057'
        ).pack(pady=(0, 15))
        
        # Frame para campos
        fields_frame = tk.Frame(left_frame, bg='#495057')
        fields_frame.pack(padx=20, pady=10)
        
        # Campo IP
        tk.Label(fields_frame, text="IP del Servidor:", 
                font=('Arial', 12, 'bold'), fg='#f8f9fa', bg='#495057').pack(anchor='w')
        
        self.server_ip_entry = tk.Entry(fields_frame, font=('Arial', 14), width=20,
                                    justify='center', bg='white', fg='#495057',
                                    relief='solid', bd=1)
        self.server_ip_entry.pack(pady=(3, 10), ipady=4)
        self.server_ip_entry.bind('<FocusIn>', lambda e: self.set_active_entry('ip'))
        
        # PUERTO FIJO - Solo texto indicativo pequeño. 
        tk.Label(fields_frame, text="Puerto: 5000 (Fijo)", 
                font=('Arial', 10, 'bold'), fg='#adb5bd', bg='#495057').pack(anchor='w', pady=(0, 5))
        
        # Mantenemos el Entry internamente para que tu logica de prueba NO falle, pero NO lo dibujamos (.pack)
        self.server_port_entry = tk.Entry(fields_frame)
        self.server_port_entry.insert(0, "5000")
        
        # Cargar valores actuales
        try:
            import urllib.parse
            parsed = urllib.parse.urlparse(self.server_url)
            self.server_ip_entry.insert(0, parsed.hostname or "192.168.1.100")
        except:
            self.server_ip_entry.insert(0, "192.168.1.100")
        
        # Estado conexion
        self.connection_status = tk.Label(left_frame, 
                                        text="Configure servidor para continuar", 
                                        font=('Arial', 10), fg='#adb5bd', bg='#495057',
                                        wraplength=250)
        self.connection_status.pack(pady=10)
        
        # Botones en linea horizontal (INTACTOS)
        button_frame = tk.Frame(left_frame, bg='#495057')
        button_frame.pack(pady=15)
        
        tk.Button(button_frame, text="Probar Conexion", 
                font=('Arial', 11, 'bold'),
                bg='#ffc107', fg='#212529', 
                activebackground='#e0a800', 
                width=14, height=2, border=0, 
                command=self.probar_conexion).pack(side='left', padx=5)
        
        tk.Button(button_frame, text="Conectar", 
                font=('Arial', 11, 'bold'), 
                bg='#28a745', fg='white', 
                activebackground='#218838', 
                width=14, height=2, border=0, 
                command=self.conectar_servidor).pack(side='left', padx=5)
        
        # Boton salir
        tk.Button(left_frame, text="Salir Aplicacion", 
                font=('Arial', 12, 'bold'), 
                bg='#dc3545', fg='white', 
                activebackground='#c82333', 
                width=25, height=2, border=0, 
                command=self.salir_aplicacion).pack(pady=10)
        
        # COLUMNA DERECHA - TECLADO
        right_frame = tk.Frame(content_frame, bg='#212529')
        right_frame.pack(side='right', fill='y')
        
        self.create_shared_keyboard(right_frame)
        
        # Inicializar campo activo
        self.active_entry_type = 'ip'
        self.server_ip_entry.focus()
    
    def set_active_entry(self, entry_type):
        """Establecer campo activo para el teclado"""
        self.active_entry_type = entry_type
    
    def create_shared_keyboard(self, parent):
        """Teclado tactil para configuracion de servidor"""
        keyboard_frame = tk.Frame(parent, bg='#212529')
        keyboard_frame.pack(pady=20)       
            
       
        buttons = [
            ['1', '2', '3'],
            ['4', '5', '6'], 
            ['7', '8', '9'],
            ['Borrar', '0', '.']
        ]
        
        for row in buttons:
            row_frame = tk.Frame(keyboard_frame, bg='#212529')
            row_frame.pack(pady=3)
            
            for btn_text in row:
                if btn_text == 'Borrar':
                    bg_color = "#f73707"
                    width = 9
                elif btn_text == '.':
                    bg_color = '#20c997'
                    width = 9
                else:
                    bg_color = '#6c757d'
                    width = 9
                
                # TECLAS MÁS ALTAS: height cambiado de 1 a 2
                btn = tk.Button(
                    row_frame,
                    text=btn_text,
                    font=('Arial', 16, 'bold'),
                    width=width,
                    height=2, 
                    bg=bg_color,
                    fg='white',
                    border=0,
                    command=lambda x=btn_text: self.on_config_key_press(x)
                )
                btn.pack(side='left', padx=3)
    
    def on_config_key_press(self, key):
        """Manejo de teclas del teclado tactil de configuracion de IP."""
        current = self.server_ip_entry.get()
        if key == 'Borrar':
            if current:
                self.server_ip_entry.delete(len(current) - 1, tk.END)
        elif key == '.':
            if len(current) < 15:
                self.server_ip_entry.insert(tk.END, '.')
        elif key.isdigit():
            if len(current) < 15:
                self.server_ip_entry.insert(tk.END, key)
    
    def probar_conexion(self):
        """Probar conexion con IP y Puerto separados (INTACTO)"""
        ip = self.server_ip_entry.get().strip()
        port = self.server_port_entry.get().strip() or "5000"
        
        if not ip:
            self.connection_status.config(
                text="Ingrese una direccion IP", fg='#dc3545')
            return
        
        test_url = f"http://{ip}:{port}"
        self.connection_status.config(text="Probando conexion...", fg='#ffc107')
        
        threading.Thread(target=self._test_connection_thread, 
                        args=(test_url,), daemon=True).start()
    
    def _test_connection_thread(self, test_url):
        """Hilo para probar conexion (INTACTO)"""
        try:
            print(f"Probando conexion a: {test_url}")
            response = requests.get(f"{test_url}/api/health", timeout=5)
            
            if response.status_code == 200:
                self.root.after(0, lambda: self.connection_status.config(
                    text="Conexion exitosa", fg='#28a745'))
                self.server_url = test_url
                print(f"Servidor OK: {test_url}")
            else:
                response = requests.get(test_url, timeout=5)
                if response.status_code in [200, 404, 403]:
                    self.root.after(0, lambda: self.connection_status.config(
                        text="Servidor encontrado", fg='#28a745'))
                    self.server_url = test_url
                else:
                    self.root.after(0, lambda: self.connection_status.config(
                        text=f"Error servidor (codigo {response.status_code})", 
                        fg='#dc3545'))
        except Exception as e:
            self.root.after(0, lambda: self.connection_status.config(
                text="No se puede conectar", fg='#dc3545'))
            print(f"Error conexion: {e}")
    
    def conectar_servidor(self):
        """Conectar con IP y Puerto separados (INTACTO)"""
        ip = self.server_ip_entry.get().strip()
        port = self.server_port_entry.get().strip() or "5000"
        
        if not ip:
            self.connection_status.config(
                text="Ingrese direccion IP", fg='#dc3545')
            return
        
        self.server_url = f"http://{ip}:{port}"
        self.guardar_configuracion()
        print(f"Conectado a servidor: {self.server_url}")
        self.mostrar_pantalla_principal()

        # FIRMA: By Felipe Garzon
        tk.Label(self.root, 
                 text="By Felipe Garzon", 
                 font=("Arial", 11, "italic bold"), 
                 fg='#555e66', 
                 bg='#212529').place(relx=1.0, rely=1.0, anchor='se', x=-20, y=-20)
   
   # ========================================================================
    # PANTALLA PRINCIPAL (MENU)
    # ========================================================================
    def mostrar_pantalla_principal(self):
        """Pantalla principal compacta, botones centrados y sin cortes"""
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Al compactar el contenido interno, el anchor='center' funcionará perfecto
        main_frame = tk.Frame(self.root, bg='#212529')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        # --- TITULO PRINCIPAL ---
        title_font = font.Font(family="Arial", size=24, weight="bold")
        tk.Label(main_frame, 
                text="QuickTable - Control de Acceso", 
                font=title_font, 
                fg='#ffffff', 
                bg='#212529').pack(pady=(5, 10)) # Márgenes reducidos
        
        # Subtitulo con estado del hardware
        if self.hardware_available and self.reader:
            status_text = "● Hardware RC522 Disponible"
            status_color = '#28a745'
        else:
            status_text = "● Hardware RC522 No disponible"
            status_color = '#dc3545'
        
        tk.Label(main_frame, 
                text=status_text, 
                font=("Arial", 14), 
                fg=status_color, 
                bg='#212529').pack(pady=(0, 20))
        
        # Frame contenedor de botones
        buttons_frame = tk.Frame(main_frame, bg='#212529')
        buttons_frame.pack(pady=10)

        # Dimensiones reducidas para que no empujen lo de abajo
        ANCHO_BTN = 350
        ALTO_BTN = 215 
        font_grande = font.Font(family="Arial", size=22, weight="bold")
        font_pequena = font.Font(family="Arial", size=13)

        # --- BOTÓN 1: MARCAR SALIDA ---
        btn1_container = tk.Frame(buttons_frame, bg='#007bff', width=ANCHO_BTN, height=ALTO_BTN, 
                                 bd=3, relief='raised', cursor="hand2")
        btn1_container.pack_propagate(False) 
        # Un padx pequeño junta los botones en el centro
        btn1_container.pack(side='left', padx=10)
        
        lbl_t1 = tk.Label(btn1_container, text="MARCAR SALIDA", font=font_grande, 
                          bg='#007bff', fg='white')
        lbl_t1.pack(expand=True, pady=(20, 0)) 
        
        lbl_d1 = tk.Label(btn1_container, text="Acerca tu tarjeta NFC\npara registrar tu salida", 
                          font=font_pequena, bg='#007bff', fg='white')
        lbl_d1.pack(expand=True, pady=(0, 20))

        for w in (btn1_container, lbl_t1, lbl_d1):
            w.bind("<Button-1>", lambda e: self.mostrar_marcar_salida())

        # --- BOTÓN 2: MODO ADMINISTRACIÓN ---
        btn2_container = tk.Frame(buttons_frame, bg='#28a745', width=ANCHO_BTN, height=ALTO_BTN, 
                                 bd=3, relief='raised', cursor="hand2")
        btn2_container.pack_propagate(False)
        btn2_container.pack(side='left', padx=10)

        lbl_t2 = tk.Label(btn2_container, text="MODO\nADMINISTRACIÓN", font=font_grande, 
                          bg='#28a745', fg='white', justify='center')
        lbl_t2.pack(expand=True, pady=(20, 0))
        
        lbl_d2 = tk.Label(btn2_container, text="Acceso completo al sistema\npara gestión y configuración", 
                          font=font_pequena, bg='#28a745', fg='white')
        lbl_d2.pack(expand=True, pady=(0, 20))

        for w in (btn2_container, lbl_t2, lbl_d2):
            w.bind("<Button-1>", lambda e: self.mostrar_pantalla_codigo())
        
        # --- PARTE INFERIOR ---
        # Se reduce drásticamente el pady para que no se vaya hasta el fondo
        lower_frame = tk.Frame(main_frame, bg='#212529')
        lower_frame.pack(pady=20) 
        
        btn_style = {"font": ("Arial", 12, "bold"), "fg": "white", "width": 18, "height": 2}
        
        tk.Button(lower_frame, text="Configurar Servidor", bg='#6c757d', 
                  command=self.mostrar_configuracion_servidor, **btn_style).pack(side='left', padx=10)
        
        tk.Button(lower_frame, text="Salir del Sistema", bg='#dc3545', 
                  command=self.salir_aplicacion, **btn_style).pack(side='left', padx=10)       

        # FIRMA: By Felipe Garzon
        tk.Label(self.root, 
                 text="By Felipe Garzon", 
                 font=("Arial", 11, "italic bold"), 
                 fg='#555e66', 
                 bg='#212529').place(relx=1.0, rely=1.0, anchor='se', x=-20, y=-20)
    # ========================================================================
    # PANTALLA MARCAR SALIDA
    # ========================================================================
    def mostrar_marcar_salida(self):
        """Pantalla para marcar salida con tarjeta NFC calculada para 800x480"""
        if not self.hardware_available or not self.reader:
            messagebox.showerror("Error", "Hardware RC522 no disponible")
            return
            
        for widget in self.root.winfo_children():
            widget.destroy()
            
        main_frame = tk.Frame(self.root, bg='#212529', width=800, height=480)
        main_frame.place(x=0, y=0)
        
        # Titulo
        title_font = font.Font(family="Arial", size=24, weight="bold")
        tk.Label(main_frame, text="Marcar Salida", font=title_font, fg='#ffffff', bg='#212529').place(x=0, y=40, width=800, height=50)
        
        # Icono
        icon_font = font.Font(family="Arial", size=48)
        tk.Label(main_frame, text="[ NFC ]", font=icon_font, fg='#007bff', bg='#212529').place(x=0, y=120, width=800, height=70)
        
        # Instrucciones
        instr_font = font.Font(family="Arial", size=18)
        tk.Label(main_frame, text="Acerca tu tarjeta NFC al lector", font=instr_font, fg='#ffffff', bg='#212529').place(x=0, y=210, width=800, height=40)
        
        # Estado (Guardado en self para el hilo)
        self.status_label = tk.Label(main_frame, text="Esperando tarjeta...", font=("Arial", 16), fg='#ffc107', bg='#212529')
        self.status_label.place(x=0, y=270, width=800, height=40)
        
        # Boton volver
        def _volver():
            self.leyendo_salida = False
            self.mostrar_pantalla_principal()

        btn_volver = tk.Button(main_frame, text="Volver al Inicio", font=("Arial", 18, "bold"), bg='#6c757d', fg='white', command=_volver)
        btn_volver.place(x=250, y=360, width=300, height=60)

        self.leyendo_salida = True
        threading.Thread(target=self.proceso_marcar_salida, daemon=True).start()
        # FIRMA: By Felipe Garzon
        tk.Label(self.root, 
                 text="By Felipe Garzon", 
                 font=("Arial", 11, "italic bold"), 
                 fg='#555e66', 
                 bg='#212529').place(relx=1.0, rely=1.0, anchor='se', x=-20, y=-20)
    
    def proceso_marcar_salida(self):
        try:
            while self.leyendo_salida:
                self.root.after(0, lambda: self.status_label.config(
                    text="Esperando tarjeta...", fg='#ffc107'))

                uid_fisico = self.reader.read_card_uid(timeout=20)

                if not self.leyendo_salida:
                    break

                if not uid_fisico:
                    self.root.after(0, lambda: self.status_label.config(
                        text="No se detecto tarjeta. Intenta de nuevo...", fg='#dc3545'))
                    time.sleep(2)
                    continue

                self.root.after(0, lambda: self.status_label.config(
                    text="Registrando salida...", fg='#17a2b8'))

                uid_b64 = base64.b64encode(
                    str(int(uid_fisico, 16)).encode('ascii')
                ).decode('ascii')

                response = requests.post(
                    f"{self.server_url}/api/asistencia/marcar-salida",
                    json={'uid': uid_b64},
                    timeout=10
                )

                if response.status_code == 200:
                    data = response.json()
                    if data.get('success'):
                        self.root.after(0, lambda d=data: self.mostrar_resultado_exitoso(d))
                    else:
                        error = data.get('message', 'Error desconocido')
                        self.root.after(0, lambda e=error: self.status_label.config(
                            text=f"Error: {e}", fg='#dc3545'))
                else:
                    self.root.after(0, lambda c=response.status_code: self.status_label.config(
                        text=f"Error del servidor ({c})", fg='#dc3545'))

                self.leyendo_salida = False
                self.root.after(3000, self.mostrar_pantalla_principal)
                break

        except requests.Timeout:
            if self.leyendo_salida:
                self.root.after(0, lambda: self.status_label.config(
                    text="Sin respuesta del servidor", fg='#dc3545'))
                self.leyendo_salida = False
                self.root.after(3000, self.mostrar_pantalla_principal)
        except requests.ConnectionError:
            if self.leyendo_salida:
                self.root.after(0, lambda: self.status_label.config(
                    text="No se pudo conectar al servidor", fg='#dc3545'))
                self.leyendo_salida = False
                self.root.after(3000, self.mostrar_pantalla_principal)
        except Exception as e:
            print(f"Error marcando salida: {e}")
            if self.leyendo_salida:
                self.root.after(0, lambda err=str(e): self.status_label.config(
                    text=f"Error: {err[:40]}", fg='#dc3545'))
                self.leyendo_salida = False
                self.root.after(3000, self.mostrar_pantalla_principal)

    def mostrar_resultado_exitoso(self, data):
        """Pantalla de confirmacion de salida con todos los datos del empleado."""
        for widget in self.root.winfo_children():
            widget.destroy()

        main_frame = tk.Frame(self.root, bg='#212529', width=800, height=480)
        main_frame.place(x=0, y=0)

        # Banner superior verde
        banner = tk.Frame(main_frame, bg='#28a745', width=800, height=80)
        banner.place(x=0, y=0)
        tk.Label(banner, text="✓  SALIDA REGISTRADA",
                 font=('Arial', 26, 'bold'), fg='white', bg='#28a745').place(
                 relx=0.5, rely=0.5, anchor='center')

        # Datos del empleado
        nombre = data.get('nombre', 'N/A')
        rol = data.get('rol', 'N/A')
        hora_ingreso = data.get('horaIngreso', 'N/A')
        hora_salida = data.get('horaSalida', 'N/A')
        tiempo = data.get('tiempoTrabajado', 'N/A')

        info_frame = tk.Frame(main_frame, bg='#343a40', width=760, height=280)
        info_frame.place(x=20, y=100)

        filas = [
            ("Empleado",       nombre),
            ("Rol",            rol),
            ("Hora de entrada", hora_ingreso),
            ("Hora de salida",  hora_salida),
            ("Tiempo trabajado", tiempo),
        ]

        for i, (etiqueta, valor) in enumerate(filas):
            y_pos = 20 + i * 50
            tk.Label(info_frame, text=f"{etiqueta}:",
                     font=('Arial', 14, 'bold'), fg='#adb5bd', bg='#343a40',
                     anchor='w', width=18).place(x=20, y=y_pos)
            tk.Label(info_frame, text=valor,
                     font=('Arial', 14), fg='#ffffff', bg='#343a40',
                     anchor='w').place(x=260, y=y_pos)

        # Boton volver + auto-regreso
        tk.Button(main_frame, text="Volver al Inicio",
                  font=('Arial', 16, 'bold'), bg='#6c757d', fg='white',
                  width=20, height=2, border=0,
                  command=self.mostrar_pantalla_principal).place(
                  relx=0.5, rely=1.0, anchor='s', y=-20)

        self.root.after(8000, self.mostrar_pantalla_principal)

   # ========================================================================
    # PANTALLA DE CODIGO DE SESION (Modo Admin/TI/Empleado)
    # ========================================================================
    def mostrar_pantalla_codigo(self):
        """Pantalla principal de codigo de sesion - LAYOUT HORIZONTAL 40/60"""
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Contenedor principal fijo a 800x480
        main_container = tk.Frame(self.root, bg='#212529', width=800, height=480)
        main_container.place(x=0, y=0)
        
        # ========== COLUMNA IZQUIERDA (40% = 320px) ==========
        left_frame = tk.Frame(main_container, bg='#343a40', width=320, height=480)
        left_frame.place(x=0, y=0)
        left_frame.pack_propagate(False) # Bloquea el tamaño para que no se expanda
        
        tk.Label(left_frame, text="Codigo de Sesion",
                font=('Arial', 24, 'bold'), # Ajustado para caber en 320px
                fg='white', bg='#343a40').pack(pady=(40, 10))
        
        tk.Label(left_frame, text="Ingrese el codigo de 6 digitos",
                font=('Arial', 12),
                fg='#adb5bd', bg='#343a40').pack(pady=10)
        
        # Campo codigo
        code_frame = tk.Frame(left_frame, bg='#343a40')
        code_frame.pack(pady=20)
        
        self.code_entry = tk.Entry(code_frame, font=('Arial', 28, 'bold'),
                                width=10, justify='center',
                                bg='white', fg='#495057')
        vcmd = (self.root.register(self.validate_code), '%P')
        self.code_entry.config(validate='key', validatecommand=vcmd)
        self.code_entry.pack(pady=10, ipady=8)
        self.code_entry.focus()
        self.code_entry.bind('<Return>', lambda e: self.validar_codigo_sesion())
        
        # Info servidor
        info_frame = tk.Frame(left_frame, bg='#343a40')
        info_frame.pack(pady=20)
        
        tk.Label(info_frame, text=f"Servidor: {self.server_url}",
                font=('Arial', 10), fg='#6c757d', bg='#343a40').pack(pady=10)
        
        # Boton regreso
        tk.Button(info_frame, text="Regresar al Menu Principal",
                font=('Arial', 12, 'bold'), bg='#17a2b8', fg='white',
                activebackground='#138496', width=26, height=2,
                border=0, command=self.mostrar_pantalla_principal).pack(pady=10)
        
        # ========== COLUMNA DERECHA - TECLADO (60% = 480px) ==========
        right_frame = tk.Frame(main_container, bg='#212529', width=480, height=480)
        right_frame.place(x=320, y=0)
        right_frame.pack_propagate(False)
        
        # Teclado numerico centrado
        keyboard_frame = tk.Frame(right_frame, bg='#212529')
        keyboard_frame.pack(expand=True)
        
        buttons = [
            ['1', '2', '3'],
            ['4', '5', '6'],
            ['7', '8', '9'],
            ['Borrar', '0', 'Entrar']
        ]
        
        for row in buttons:
            row_frame = tk.Frame(keyboard_frame, bg='#212529')
            row_frame.pack(pady=6)
            
            for btn_text in row:
                if btn_text == 'Entrar':
                    bg_color = '#007bff'
                elif btn_text == 'Borrar':
                    bg_color = '#dc3545'
                else:
                    bg_color = '#6c757d'
                
                btn = tk.Button(
                    row_frame,
                    text=btn_text,
                    font=('Arial', 20, 'bold'),
                    width=6,   # Reducido proporcionalmente para no desbordar los 480px
                    height=2,  # Reducido proporcionalmente
                    bg=bg_color,
                    fg='white',
                    border=0,
                    command=lambda x=btn_text: self.on_session_key_press(x)
                )
                btn.pack(side='left', padx=6)

    def on_session_key_press(self, key):
        """Manejo de teclas para pantalla de codigo de sesion"""
        current = self.code_entry.get()
        
        if key == 'Borrar':
            if current:
                self.code_entry.delete(len(current)-1, tk.END)
        elif key == 'Entrar':
            self.validar_codigo_sesion()
        elif key.isdigit():
            if len(current) < 6:
                self.code_entry.insert(tk.END, key)

    
    def validate_code(self, value):
        return len(value) <= 6 and (value.isdigit() or value == "")
    
    def validar_codigo_sesion(self):
        codigo = self.code_entry.get().strip()
        
        if len(codigo) != 6:
            messagebox.showerror("Error", "Debe ingresar un codigo de 6 digitos")
            return
        
        print(f"Validando codigo: {codigo}")
        threading.Thread(target=self._validar_sesion_thread, 
                        args=(codigo,), daemon=True).start()
    
    def _validar_sesion_thread(self, codigo):
        try:
            print("Intentando validacion TI...")
            response = requests.post(
                f"{self.server_url}/api/tarjeta/validar-sesion", 
                data={'sessionCode': codigo}, 
                timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                print(f"Respuesta TI: {data}")
                if data.get('valid') and data.get('role') == 'TI':
                    print("Sesion TI valida")
                    self.session_data = data
                    self.root.after(0, self.mostrar_modo_ti)
                    return
            elif response.status_code == 404:
                print("No hay tarjetas pendientes para TI")
            
            print("Intentando validacion Admin...")
            response = requests.post(
                f"{self.server_url}/api/tarjeta/validar-sesion-admin", 
                data={'sessionCode': codigo}, 
                timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                print(f"Respuesta Admin: {data}")
                if data.get('valid') and data.get('role') == 'Admin':
                    print("Sesion Admin valida")
                    self.session_data = data
                    self.root.after(0, self.mostrar_modo_admin)
                    return
            
            print("Intentando validacion empleado...")
            response = requests.post(
                f"{self.server_url}/api/tarjeta/validar-sesion-empleado", 
                data={'sessionCode': codigo}, 
                timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                print(f"Respuesta Empleado: {data}")
                if data.get('valid') and data.get('tipo') == 'tarjeta-empleado':
                    print("Sesion Empleado valida")
                    self.session_data = data
                    self.root.after(0, self.mostrar_modo_empleado)
                    return
            
            print("Codigo de sesion invalido")
            self.root.after(0, lambda: messagebox.showerror(
                "Error", "Codigo invalido o no hay sesiones activas"))
            
        except Exception as e:
            print(f"Error validando sesion: {e}")
            self.root.after(0, lambda: messagebox.showerror(
                "Error", f"Error de conexion: {str(e)}"))
            
            # FIRMA: By Felipe Garzon
        tk.Label(self.root, 
                 text="By Felipe Garzon", 
                 font=("Arial", 11, "italic bold"), 
                 fg='#555e66', 
                 bg='#212529').place(relx=1.0, rely=1.0, anchor='se', x=-20, y=-20)
            
    # ========================================================================
    # MODO EMPLEADO (Asignar tarjeta a empleado)
    # ========================================================================
    def mostrar_modo_empleado(self):
        """Modo para asignar tarjetas a empleados"""
        for widget in self.root.winfo_children():
            widget.destroy()
        
        main_frame = tk.Frame(self.root, bg='#343a40')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        # Header
        header_frame = tk.Frame(main_frame, bg='#28a745', width=500)
        header_frame.pack(fill='x', pady=(0, 20))
        
        tk.Label(header_frame, text="Asignar Tarjeta de Empleado", 
                font=('Arial', 18, 'bold'), 
                fg='white', bg='#28a745').pack(pady=15)
        
        # Info empleado
        info_frame = tk.Frame(main_frame, bg='#495057', relief='raised', bd=1)
        info_frame.pack(fill='x', pady=10, padx=20)
        
        empleado_nombre = self.session_data.get('nombre', 'N/A')
        empleado_rol = self.session_data.get('rolEmpleado', 'N/A')
        empleado_id = self.session_data.get('empleadoId', 'N/A')
        
        tk.Label(info_frame, text=f"Empleado: {empleado_nombre}", 
                font=('Arial', 12, 'bold'), 
                fg='#f8f9fa', bg='#495057').pack(pady=5)
        
        tk.Label(info_frame, text=f"Rol: {empleado_rol} | ID: {empleado_id}", 
                font=('Arial', 10), 
                fg='#adb5bd', bg='#495057').pack(pady=5)
        
        # Estado
        self.empleado_status_label = tk.Label(main_frame, 
                                            text="Acerca la nueva tarjeta NFC al lector...", 
                                            font=('Arial', 14), fg='#28a745', bg='#343a40')
        self.empleado_status_label.pack(pady=30)
        
        # Botones
        button_frame = tk.Frame(main_frame, bg='#343a40')
        button_frame.pack(pady=20)
        
        tk.Button(button_frame, text="Asignar Tarjeta", 
                 font=('Arial', 12, 'bold'), bg='#007bff', fg='white', 
                 activebackground='#0056b3', width=18, height=2, 
                 border=0, command=self.asignar_tarjeta_empleado).pack(side='left', padx=10)
        
        tk.Button(button_frame, text="Regresar", 
                 font=('Arial', 11), bg='#17a2b8', fg='white', 
                 activebackground='#138496', width=15, height=2, 
                 border=0, command=self.mostrar_pantalla_principal).pack(side='right', padx=10)
    
    def asignar_tarjeta_empleado(self):
        if not HARDWARE_AVAILABLE or not self.reader:
            messagebox.showerror("Error", "Hardware RC522 no disponible")
            return
        
        threading.Thread(target=self._asignar_tarjeta_empleado_thread, daemon=True).start()
    
    def _asignar_tarjeta_empleado_thread(self):
        try:
            self.root.after(0, lambda: self.empleado_status_label.config(
                text="Leyendo tarjeta...", fg='#ffc107'))
            
            uid_fisico = self.reader.read_card_uid(timeout=20)
            
            if not uid_fisico:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    "No se pudo leer la tarjeta"))
                return
            
            self.root.after(0, lambda: self.empleado_status_label.config(
                text="Enviando al servidor...", fg='#17a2b8'))
            
            empleado_id = self.session_data.get('empleadoId')
            response = requests.post(
                f"{self.server_url}/api/tarjeta/asignar-empleado",
                json={
                    'empleadoId': empleado_id,
                    'uid': uid_fisico
                },
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    mensaje = f"Tarjeta asignada exitosamente\n\n"
                    mensaje += f"Empleado: {data.get('nombre', 'N/A')}\n"
                    mensaje += f"Rol: {data.get('rol', 'N/A')}"
                    
                    self.root.after(0, lambda: self.empleado_status_label.config(
                        text="Tarjeta asignada correctamente", fg='#28a745'))
                    
                    self.root.after(0, lambda: messagebox.showinfo("Exito", mensaje))
                    
                    self.root.after(3000, self.mostrar_pantalla_principal)
                else:
                    self.root.after(0, lambda: messagebox.showerror("Error", 
                        data.get('message', 'Error desconocido')))
            else:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    f"Error del servidor: {response.status_code}"))
        except Exception as e:
            print(f"Error asignando tarjeta empleado: {e}")
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))

            # FIRMA: By Felipe Garzon
        tk.Label(self.root, 
                 text="By Felipe Garzon", 
                 font=("Arial", 11, "italic bold"), 
                 fg='#555e66', 
                 bg='#212529').place(relx=1.0, rely=1.0, anchor='se', x=-20, y=-20)
    
    # ========================================================================
    # MODO TI (Escribir tarjetas admin)
    # ========================================================================
    def mostrar_modo_ti(self):
        for widget in self.root.winfo_children():
            widget.destroy()
        
        main_frame = tk.Frame(self.root, bg='#343a40')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        # Header
        header_frame = tk.Frame(main_frame, bg='#17a2b8', width=500)
        header_frame.pack(fill='x', pady=(0, 20))
        
        tk.Label(header_frame, text="Modo TI - Escribir Tarjetas", 
                font=('Arial', 18, 'bold'), 
                fg='white', bg='#17a2b8').pack(pady=15)
        
        # Info
        info_frame = tk.Frame(main_frame, bg='#495057', relief='raised', bd=1)
        info_frame.pack(fill='x', pady=10, padx=20)
        
        uid_real = self.session_data.get('uid', 'ERROR-NO-UID')
        tk.Label(info_frame, text=f"UID: {uid_real}", 
                font=('Arial', 11, 'bold'), 
                fg='#ffc107', bg='#495057').pack(pady=10)
        
        admin_nombre = self.session_data.get('adminNombre', 'N/A')
        tk.Label(info_frame, text=f"Admin: {admin_nombre}", 
                font=('Arial', 11), 
                fg='#f8f9fa', bg='#495057').pack(pady=5)
        
        # Estado
        self.ti_status_label = tk.Label(main_frame, 
                                      text="Acerca la tarjeta al lector...", 
                                      font=('Arial', 14), fg='#17a2b8', bg='#343a40')
        self.ti_status_label.pack(pady=30)
        
        # Botones
        button_frame = tk.Frame(main_frame, bg='#343a40')
        button_frame.pack(pady=20)
        
        self.write_button = tk.Button(button_frame, text="Limpiar y Escribir", 
                 font=('Arial', 12, 'bold'), bg='#007bff', fg='white', 
                 activebackground='#0056b3', width=18, height=2, 
                 border=0, command=self.escribir_tarjeta_limpia)
        self.write_button.pack(side='left', padx=10)
        
        self.verify_button = tk.Button(button_frame, text="Verificar y Confirmar", 
                 font=('Arial', 12, 'bold'), bg='#28a745', fg='white', 
                 activebackground='#218838', width=18, height=2, 
                 border=0, state='disabled', command=self.verificar_y_confirmar)
        self.verify_button.pack(side='left', padx=10)
        
        tk.Button(main_frame, text="Regresar", 
                 font=('Arial', 11), bg='#17a2b8', fg='white', 
                 activebackground='#138496', width=15, height=2, 
                 border=0, command=self.mostrar_pantalla_principal).pack(pady=10)
    
    def escribir_tarjeta_limpia(self):
        self.write_button.config(state='disabled', text='Escribiendo...')
        threading.Thread(target=self._escribir_tarjeta_real, daemon=True).start()
    
    def _escribir_tarjeta_real(self):
        if not HARDWARE_AVAILABLE or not self.reader:
            self.root.after(0, lambda: messagebox.showerror(
                "Error", "Hardware RC522 no disponible"))
            self.root.after(0, lambda: self.write_button.config(
                state='normal', text='Limpiar y Escribir'))
            return
        
        try:
            uid_real = self.session_data.get('uid', 'ERROR')
            print(f"Escribiendo UID REAL: {uid_real}")
            
            self.root.after(0, lambda: self.ti_status_label.config(
                text="Escribiendo UID...", fg='#ffc107'))
            
            if self.reader.clear_and_write_text(uid_real, timeout=30):
                print("Escritura completada")
                self.root.after(0, lambda: self.ti_status_label.config(
                    text="Tarjeta escrita exitosamente", fg='#28a745'))
                self.root.after(0, lambda: self.verify_button.config(state='normal'))
                self.root.after(0, lambda: self.write_button.config(
                    state='normal', text='Escribir Nueva Tarjeta'))
            else:
                print("Error escribiendo")
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    "Error escribiendo tarjeta"))
                self.root.after(0, lambda: self.ti_status_label.config(
                    text="Error en escritura", fg='#dc3545'))
                self.root.after(0, lambda: self.write_button.config(
                    state='normal', text='Limpiar y Escribir'))
                
        except Exception as e:
            print(f"Error escribiendo: {e}")
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))
            self.root.after(0, lambda: self.write_button.config(
                state='normal', text='Limpiar y Escribir'))
    
    def verificar_y_confirmar(self):
        self.verify_button.config(state='disabled', text='Verificando...')
        threading.Thread(target=self._verificar_real, daemon=True).start()
    
    def _verificar_real(self):
        if not HARDWARE_AVAILABLE or not self.reader:
            self.root.after(0, lambda: messagebox.showerror(
                "Error", "Hardware RC522 no disponible"))
            return
        
        try:
            uid_esperado = self.session_data.get('uid', 'ERROR')
            
            self.root.after(0, lambda: self.ti_status_label.config(
                text="Verificando...", fg='#007bff'))
            
            uid_leido, texto_leido = self.reader.read_text_from_card(timeout=15)
            
            if uid_leido and texto_leido:
                print(f"Comparando: '{uid_esperado}' vs '{texto_leido}'")
                
                if texto_leido.strip() == uid_esperado.strip():
                    print("Verificacion exitosa")
                    
                    self.root.after(0, lambda: self.ti_status_label.config(
                        text="Confirmando con servidor...", fg='#17a2b8'))
                    
                    response = requests.post(f"{self.server_url}/api/tarjeta/confirmar", 
                                           data={'uidLeido': uid_leido}, timeout=10)
                    
                    if response.status_code == 200:
                        print("PROCESO COMPLETADO")
                        self.root.after(0, lambda: self.ti_status_label.config(
                            text="COMPLETADO - Tarjeta activada", fg='#28a745'))
                        self.root.after(3000, self.mostrar_pantalla_principal)
                    else:
                        self.root.after(0, lambda: messagebox.showerror("Error", 
                            f"Error servidor: {response.status_code}"))
                else:
                    self.root.after(0, lambda: messagebox.showerror("Error", 
                        "UIDs no coinciden"))
            else:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    "No se pudo leer la tarjeta"))
                
        except Exception as e:
            print(f"Error verificando: {e}")
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))
        finally:
            self.root.after(0, lambda: self.verify_button.config(
                state='normal', text='Verificar y Confirmar'))
            # FIRMA: By Felipe Garzon
        tk.Label(self.root, 
                 text="By Felipe Garzon", 
                 font=("Arial", 11, "italic bold"), 
                 fg='#555e66', 
                 bg='#212529').place(relx=1.0, rely=1.0, anchor='se', x=-20, y=-20)
    
    # ========================================================================
    # MODO ADMIN (Autenticacion 2FA)
    # ========================================================================
    def mostrar_modo_admin(self):
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Variable para controlar el bucle de lectura
        self.leyendo_2fa = True
        
        main_frame = tk.Frame(self.root, bg='#343a40')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        tk.Label(main_frame, text="Modo Admin - Autenticacion 2FA", 
                font=('Arial', 18, 'bold'), 
                fg='white', bg='#28a745').pack(pady=20)
        
        nav_id = self.session_data.get('navId', 'N/A')
        admin_nombre = self.session_data.get('adminNombre', 'N/A')
        
        tk.Label(main_frame, text=f"Admin: {admin_nombre}", 
                font=('Arial', 12), fg='#f8f9fa', bg='#343a40').pack(pady=5)
        
        tk.Label(main_frame, text=f"NavID: {str(nav_id)[:8]}...", 
                font=('Arial', 10), fg='#adb5bd', bg='#343a40').pack(pady=5)
        
        # Etiqueta dinamica para reemplazar los Messagebox
        self.admin_status_label = tk.Label(main_frame, 
                                         text="Acerca su tarjeta de administrador...", 
                                         font=('Arial', 14), fg='#28a745', bg='#343a40')
        self.admin_status_label.pack(pady=30)
        
        # Funcion interna para detener la lectura si se presiona el boton
        def regresar():
            self.leyendo_2fa = False
            self.mostrar_pantalla_principal()

        tk.Button(main_frame, text="Regresar", 
                 font=('Arial', 11), bg='#17a2b8', fg='white', 
                 width=15, height=2, 
                 command=regresar).pack(pady=20)
        
        threading.Thread(target=self.proceso_admin, daemon=True).start()
    
    def proceso_admin(self):
        if not HARDWARE_AVAILABLE or not self.reader:
            self.root.after(0, lambda: self.admin_status_label.config(
                text="Error: Hardware RC522 no disponible", fg='#dc3545'))
            return
        
        try:
            while self.leyendo_2fa:
                # 1. LIMPIAR VARIABLES DE MEMORIA EN CADA CICLO
                uid_fisico = None
                texto_leido = None
                
                self.root.after(0, lambda: self.admin_status_label.config(
                    text="Acerca su tarjeta de administrador...", fg='#28a745'))

                uid_fisico, texto_leido = self.reader.read_text_from_card(timeout=25)
                
                if not self.leyendo_2fa:
                    break

                if not uid_fisico or not texto_leido:
                    continue
                
                self.root.after(0, lambda: self.admin_status_label.config(
                    text=f"Verificando tarjeta...", fg='#ffc107'))
                
                response = requests.post(f"{self.server_url}/Login/Confirmar2FA", 
                                    data={
                                        'navId': self.session_data.get('navId', ''),
                                        'uidFisico': uid_fisico,
                                        'textoEscrito': texto_leido
                                    }, timeout=10)
                
                if response.status_code == 200:
                    self.root.after(0, lambda: self.admin_status_label.config(
                        text="Autenticacion 2FA exitosa", fg='#28a745'))
                    self.leyendo_2fa = False 
                    self.root.after(3000, self.mostrar_pantalla_principal)
                    break
                else:
                    # 1. Capturamos el error
                    error_msg = response.text.strip()
                    print(f"Rechazo del servidor. Codigo: {response.status_code} - Motivo: {error_msg}")
                    
                    if len(error_msg) > 35:
                        error_msg = error_msg[:35] + "..." 
                        
                    # 2. Mostramos el error en rojo en la pantalla
                    self.root.after(0, lambda msg=error_msg: self.admin_status_label.config(
                        text=f"Error: {msg}", fg='#dc3545'))
                    
                    # 3. Esperamos 3 segundos para leer el error
                    time.sleep(3)
                    
                    # 4. MATAMOS LA SESIÓN Y REGRESAMOS AL MENÚ PRINCIPAL
                    self.leyendo_2fa = False 
                    self.root.after(0, self.mostrar_pantalla_principal)
                    break
                
        except Exception as e:
            if self.leyendo_2fa:
                self.root.after(0, lambda err=str(e): self.admin_status_label.config(
                    text=f"Excepción: {err[:30]}", fg='#dc3545'))
                print(f"Excepción 2FA: {e}")
                time.sleep(3)
                
                # También regresamos al menú si hay una excepción (ej. se cae la red)
                self.leyendo_2fa = False
                self.root.after(0, self.mostrar_pantalla_principal)

        # FIRMA: By Felipe Garzon
        self.root.after(0, lambda: tk.Label(self.root, 
                 text="By Felipe Garzon", 
                 font=("Arial", 11, "italic bold"), 
                 fg='#555e66', 
                 bg='#212529').place(relx=1.0, rely=1.0, anchor='se', x=-20, y=-20))
            
    # ========================================================================
    # FUNCIONES DE CONTROL
    # ========================================================================
    def salir_aplicacion(self):
        """Salir de la aplicacion"""
        if messagebox.askyesno("Salir", "Estas seguro de que quieres salir?"):
            if self.reader:
                self.reader.cleanup()
            self.root.destroy()
            sys.exit(0)
    
    def run(self):
        """Ejecutar la aplicacion"""
        try:
            print("=" * 60)
            print("QuickTable Control de Acceso UNIFICADO")
            print(f"Servidor: {self.server_url}")
            print(f"Hardware RC522: {'Disponible' if self.hardware_available and self.reader else 'No disponible'}")
            print("ESC = Salir pantalla completa, F11 = Alternar pantalla completa")
            print("=" * 60)
            
            self.root.mainloop()
        except Exception as e:
            print(f"Error en aplicacion: {e}")
        finally:
            if self.reader:
                self.reader.cleanup()
            print("Aplicacion cerrada")

# ============================================================================
# SECCION 5: PUNTO DE ENTRADA
# ============================================================================
if __name__ == "__main__":
    try:
        app = QuickTableControlAcceso()
        app.run()
    except KeyboardInterrupt:
        print("Aplicacion interrumpida por usuario")
        sys.exit(0)
    except Exception as e:
        print(f"Error fatal: {e}")
        sys.exit(1)   
        