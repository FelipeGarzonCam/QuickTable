#!/usr/bin/env python3
"""
QuickTable NFC - Sistema completo táctil
"""

import tkinter as tk
from tkinter import messagebox
import requests
import threading
import time
import json
import os

# Detectar hardware
try:
    from mfrc522 import SimpleMFRC522
    import RPi.GPIO as GPIO
    GPIO.setwarnings(False)
    HARDWARE_AVAILABLE = True
    print("Hardware RC522 detectado")
except ImportError as e:
    HARDWARE_AVAILABLE = False
    print(f"Hardware RC522 no disponible: {e}")

# Wrapper RC522
class QuickTableRFID:
    def __init__(self):
        if not HARDWARE_AVAILABLE:
            raise Exception("Hardware RC522 no disponible")
        
        self.reader = SimpleMFRC522()
        print("SimpleMFRC522 inicializado")
    
    def read_card_uid(self, timeout=15):
        print(f"Buscando tarjeta (timeout: {timeout}s)...")
        start_time = time.time()
        
        while (time.time() - start_time) < timeout:
            try:
                id, text = self.reader.read_no_block()
                if id:
                    uid_string = f"{id:016X}"
                    print(f"Tarjeta detectada: {uid_string}")
                    return uid_string
            except Exception as e:
                if "No card" not in str(e):
                    print(f"Error lectura: {e}")
            time.sleep(0.1)
        
        print("Timeout - No hay tarjeta")
        return None
    
    def clear_and_write_text(self, text, timeout=25):
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
                print("Timeout en operación")
                return False
                
        except Exception as e:
            print(f"Error crítico: {e}")
            return False
    
    def read_text_from_card(self, timeout=10):
        print("Leyendo contenido...")
        start_time = time.time()
        
        while (time.time() - start_time) < timeout:
            try:
                id, text = self.reader.read_no_block()
                if id:
                    uid_string = f"{id:016X}"
                    text = (text or "").strip()
                    print(f"Leído - UID: {uid_string}, Texto: '{text}'")
                    return uid_string, text
            except Exception as e:
                if "No card" not in str(e):
                    print(f"Error lectura: {e}")
            time.sleep(0.1)
        
        print("Timeout leyendo tarjeta")
        return None, None
    
    def cleanup(self):
        try:
            GPIO.cleanup()
            print("GPIO limpiado")
        except:
            pass

# Teclado numérico táctil
class AdminLTEKeyboard:
    def __init__(self, parent, entry_widget, app_instance):
        self.parent = parent
        self.entry = entry_widget
        self.app = app_instance
        self.create_keyboard()
    
    def create_keyboard(self):
        keyboard_frame = tk.Frame(self.parent, bg='#343a40')
        keyboard_frame.pack(pady=20)
        
        buttons = [
            ['1', '2', '3'],
            ['4', '5', '6'],
            ['7', '8', '9'],
            ['Borrar', '0', 'Entrar']
        ]
        
        for row in buttons:
            row_frame = tk.Frame(keyboard_frame, bg='#343a40')
            row_frame.pack(pady=3)
            
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
                    font=('Arial', 12, 'bold'),
                    width=6,
                    height=2,
                    bg=bg_color,
                    fg='white',
                    border=0,
                    command=lambda x=btn_text: self.on_key_press(x)
                )
                btn.pack(side='left', padx=3)
    
    def on_key_press(self, key):
        current = self.entry.get()
        
        if key == 'Borrar':
            if current:
                self.entry.delete(len(current)-1, tk.END)
        elif key == 'Entrar':
            if hasattr(self.app, 'validar_codigo_sesion'):
                self.app.validar_codigo_sesion()
            elif hasattr(self.app, 'probar_conexion'):
                self.app.probar_conexion()
        elif key.isdigit():
            if len(current) < 15:  # Limitar longitud
                self.entry.insert(tk.END, key)

# Aplicación principal
class QuickTableNFCApp:
    def __init__(self):
        self.server_url = ""
        self.session_data = {}
        self.root = None
        
        # Inicializar RFID
        self.reader = None
        if HARDWARE_AVAILABLE:
            try:
                self.reader = QuickTableRFID()
                print("RFID inicializado correctamente")
            except Exception as e:
                print(f"Error inicializando RFID: {e}")
                self.reader = None
    
    def setup_window(self):
        self.root = tk.Tk()
        self.root.title("QuickTable NFC")
        self.root.geometry("800x600")
        self.root.resizable(False, False)
        self.root.configure(bg='#343a40')
        self.root.eval('tk::PlaceWindow . center')
        self.root.bind('<Escape>', lambda e: self.salir_aplicacion())
        self.root.protocol("WM_DELETE_WINDOW", self.salir_aplicacion)
    
    def salir_aplicacion(self):
        print("Cerrando aplicación...")
        if self.reader:
            self.reader.cleanup()
        self.root.quit()
        self.root.destroy()
        import sys
        sys.exit(0)
    #CONFIGURACION DEL SERVIDOR CAMBIAR
    def mostrar_configuracion_servidor(self):
        if not self.root:
            self.setup_window()
        
        # Limpiar ventana actual (NO crear nueva)
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Frame principal horizontal
        main_frame = tk.Frame(self.root, bg='#343a40')
        main_frame.pack(fill='both', expand=True, padx=20, pady=20)
        
        # Título centrado
        title_frame = tk.Frame(main_frame, bg='#343a40')
        title_frame.pack(fill='x', pady=(0, 20))
        
        tk.Label(
            title_frame,
            text="QuickTable NFC - Configuración",
            font=('Arial', 24, 'bold'),
            fg='#007bff',
            bg='#343a40'
        ).pack()
        
        # Frame contenedor horizontal  
        content_frame = tk.Frame(main_frame, bg='#343a40')
        content_frame.pack(fill='both', expand=True)
        
        # ===== COLUMNA IZQUIERDA - FORMULARIOS =====
        left_frame = tk.Frame(content_frame, bg='#495057', relief='raised', bd=2)
        left_frame.pack(side='left', fill='both', expand=True, padx=(0, 10))
        
        tk.Label(
            left_frame,
            text="Configuración del Servidor",
            font=('Arial', 16, 'bold'),
            fg='white',
            bg='#495057'
        ).pack(pady=15)
        
        # Estado hardware
        if HARDWARE_AVAILABLE and self.reader:
            status_text = "✓ RC522 Conectado"
            status_color = '#28a745'
        else:
            status_text = "✗ RC522 No Disponible"
            status_color = '#dc3545'
        
        tk.Label(
            left_frame,
            text=status_text,
            font=('Arial', 11, 'bold'),
            fg=status_color,
            bg='#495057'
        ).pack(pady=(0, 15))
        
        # Frame para campos (más compacto)
        fields_frame = tk.Frame(left_frame, bg='#495057')
        fields_frame.pack(padx=20, pady=10)
        
        # Campo IP (más pequeño)
        tk.Label(fields_frame, text="IP del Servidor:", 
                font=('Arial', 12, 'bold'), fg='#f8f9fa', bg='#495057').pack(anchor='w')
        
        self.server_ip_entry = tk.Entry(fields_frame, font=('Arial', 14), width=20,
                                    justify='center', bg='white', fg='#495057',
                                    relief='solid', bd=1)
        self.server_ip_entry.pack(pady=(3, 10), ipady=4)
        self.server_ip_entry.insert(0, "192.168.101.10")
        self.server_ip_entry.bind('<FocusIn>', lambda e: self.set_active_entry('ip'))
        
        # Campo Puerto (más pequeño)
        tk.Label(fields_frame, text="Puerto:", 
                font=('Arial', 12, 'bold'), fg='#f8f9fa', bg='#495057').pack(anchor='w')
        
        self.server_port_entry = tk.Entry(fields_frame, font=('Arial', 14), width=20,
                                        justify='center', bg='white', fg='#495057',
                                        relief='solid', bd=1)
        self.server_port_entry.pack(pady=(3, 15), ipady=4)
        self.server_port_entry.insert(0, "5000")
        self.server_port_entry.bind('<FocusIn>', lambda e: self.set_active_entry('port'))
        
        # Estado conexión
        self.connection_status = tk.Label(left_frame, 
                                        text="Configure servidor para continuar", 
                                        font=('Arial', 10), fg='#adb5bd', bg='#495057',
                                        wraplength=250)
        self.connection_status.pack(pady=10)
        
        # Botones en línea horizontal (mismo tamaño)
        button_frame = tk.Frame(left_frame, bg='#495057')
        button_frame.pack(pady=15)
        
        tk.Button(button_frame, text="Probar Conexión", 
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
        
        # ===== COLUMNA DERECHA - TECLADO =====
        right_frame = tk.Frame(content_frame, bg='#343a40')
        right_frame.pack(side='right', fill='y')
        
        self.create_shared_keyboard(right_frame)
        
        # Inicializar campo activo
        self.active_entry_type = 'ip'
        self.server_ip_entry.focus()

    def set_active_entry(self, entry_type):
        self.active_entry_type = entry_type

    def create_shared_keyboard(self, parent):
        keyboard_frame = tk.Frame(parent, bg='#343a40')
        keyboard_frame.pack(pady=20)
        
        tk.Label(keyboard_frame, text="Teclado Numérico", 
                font=('Arial', 14, 'bold'), 
                fg='white', bg='#343a40').pack(pady=(0, 10))
        
        # Indicador de campo activo
        self.active_indicator = tk.Label(keyboard_frame, 
                                    text="Editando: IP", 
                                    font=('Arial', 11, 'bold'), 
                                    fg='#17a2b8', bg='#343a40')
        self.active_indicator.pack(pady=(0, 8))
        
        buttons = [
            ['1', '2', '3'],
            ['4', '5', '6'], 
            ['7', '8', '9'],
            ['Borrar', '0', 'Punto'],
            ['← IP', 'Puerto →', 'Limpiar']
        ]
        
        for row in buttons:
            row_frame = tk.Frame(keyboard_frame, bg='#343a40')
            row_frame.pack(pady=2)
            
            for btn_text in row:
                if btn_text == '← IP':
                    bg_color = '#17a2b8'
                    width = 9
                elif btn_text == 'Puerto →':
                    bg_color = '#6f42c1'  
                    width = 9
                elif btn_text == 'Limpiar':
                    bg_color = '#dc3545'
                    width = 9
                elif btn_text == 'Borrar':
                    bg_color = '#fd7e14'
                    width = 9
                elif btn_text == 'Punto':
                    bg_color = '#20c997'
                    width = 9
                else:
                    bg_color = '#6c757d'
                    width = 9
                
                btn = tk.Button(
                    row_frame,
                    text=btn_text,
                    font=('Arial', 10, 'bold'),
                    width=width,
                    height=2,
                    bg=bg_color,
                    fg='white',
                    border=0,
                    command=lambda x=btn_text: self.on_config_key_press(x)
                )
                btn.pack(side='left', padx=1)

    def on_config_key_press(self, key):
        # Obtener campo activo
        if self.active_entry_type == 'ip':
            current_entry = self.server_ip_entry
            self.active_indicator.config(text="Editando: IP", fg='#17a2b8')
        else:
            current_entry = self.server_port_entry  
            self.active_indicator.config(text="Editando: Puerto", fg='#6f42c1')
        
        current = current_entry.get()
        
        if key == '← IP':
            self.active_entry_type = 'ip'
            self.server_ip_entry.focus()
            self.server_ip_entry.icursor(tk.END)
            self.active_indicator.config(text="Editando: IP", fg='#17a2b8')
        elif key == 'Puerto →':
            self.active_entry_type = 'port'
            self.server_port_entry.focus()
            self.server_port_entry.icursor(tk.END)
            self.active_indicator.config(text="Editando: Puerto", fg='#6f42c1')
        elif key == 'Limpiar':
            current_entry.delete(0, tk.END)
        elif key == 'Borrar':
            if current:
                current_entry.delete(len(current)-1, tk.END)
        elif key == 'Punto':
            if len(current) < 15 and '.' not in current[-3:]:  # Evitar puntos consecutivos
                current_entry.insert(tk.END, '.')
        elif key.isdigit():
            if len(current) < 15:
                current_entry.insert(tk.END, key)

    # También actualizar el método conectar_servidor para usar la misma ventana:
    def conectar_servidor(self):
        ip = self.server_ip_entry.get().strip()
        port = self.server_port_entry.get().strip() or "5000"
        
        if not ip:
            self.connection_status.config(
                text="Ingrese dirección IP", fg='#dc3545')
            return
        
        self.server_url = f"http://{ip}:{port}"
        self.guardar_configuracion()
        print(f"Conectado a servidor: {self.server_url}")
        
        # Cambiar a pantalla de código EN LA MISMA VENTANA
        self.mostrar_pantalla_codigo()

    
    def probar_conexion(self):
        ip = self.server_ip_entry.get().strip()
        port = self.server_port_entry.get().strip() or "5000"
        
        if not ip:
            self.connection_status.config(
                text="Ingrese una dirección IP", fg='#dc3545')
            return
        
        test_url = f"http://{ip}:{port}"
        self.connection_status.config(text="Probando conexión...", fg='#ffc107')
        
        threading.Thread(target=self._test_connection_thread, 
                        args=(test_url,), daemon=True).start()
    
    def _test_connection_thread(self, test_url):
        try:
            print(f"Probando conexión a: {test_url}")
            response = requests.get(f"{test_url}/api/tarjeta/pendientes", timeout=5)
            
            if response.status_code == 200:
                self.root.after(0, lambda: self.connection_status.config(
                    text="Conexión exitosa", fg='#28a745'))
                self.server_url = test_url
                print(f"Servidor OK: {test_url}")
            else:
                self.root.after(0, lambda: self.connection_status.config(
                    text=f"Error servidor (código {response.status_code})", 
                    fg='#dc3545'))
        except Exception as e:
            self.root.after(0, lambda: self.connection_status.config(
                text="No se puede conectar", fg='#dc3545'))
            print(f"Error conexión: {e}")
    
    def conectar_servidor(self):
        ip = self.server_ip_entry.get().strip()
        port = self.server_port_entry.get().strip() or "5000"
        
        if not ip:
            self.connection_status.config(
                text="Ingrese dirección IP", fg='#dc3545')
            return
        
        self.server_url = f"http://{ip}:{port}"
        self.guardar_configuracion()
        print(f"Conectado a servidor: {self.server_url}")
        self.mostrar_pantalla_codigo()
    
    def guardar_configuracion(self):
        try:
            config = {"server_url": self.server_url}
            with open('config.json', 'w') as f:
                json.dump(config, f, indent=4)
            print("Configuración guardada")
        except Exception as e:
            print(f"Error guardando configuración: {e}")
    
    def cargar_configuracion_previa(self):
        try:
            if os.path.exists('config.json'):
                with open('config.json', 'r') as f:
                    config = json.load(f)
                    url = config.get('server_url', '')
                    if url:
                        print(f"Configuración cargada: {url}")
                        return url
        except Exception as e:
            print(f"Error cargando configuración: {e}")
        return ''
    
    def mostrar_pantalla_codigo(self):
        for widget in self.root.winfo_children():
            widget.destroy()
        
        main_frame = tk.Frame(self.root, bg='#343a40')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        tk.Label(main_frame, text="Código de Sesión", 
                font=('Arial', 24, 'bold'), 
                fg='white', bg='#343a40').pack(pady=(0, 20))
        
        tk.Label(main_frame, text="Ingrese el código de 6 dígitos", 
                font=('Arial', 12), 
                fg='#adb5bd', bg='#343a40').pack(pady=10)
        
        # Campo código
        code_frame = tk.Frame(main_frame, bg='#343a40')
        code_frame.pack(pady=20)
        
        self.code_entry = tk.Entry(code_frame, font=('Arial', 22, 'bold'), 
                                  width=8, justify='center', 
                                  bg='white', fg='#495057')
        
        vcmd = (self.root.register(self.validate_code), '%P')
        self.code_entry.config(validate='key', validatecommand=vcmd)
        self.code_entry.pack(pady=10)
        self.code_entry.focus()
        self.code_entry.bind('<Return>', lambda e: self.validar_codigo_sesion())
        
        # Teclado
        self.keyboard = AdminLTEKeyboard(main_frame, self.code_entry, self)
        
        # Info servidor
        tk.Label(main_frame, text=f"Servidor: {self.server_url}", 
                font=('Arial', 9), fg='#6c757d', bg='#343a40').pack(pady=(20, 10))
        
        tk.Button(main_frame, text="Cambiar Servidor", 
                 font=('Arial', 10), bg='#6c757d', fg='white', 
                 activebackground='#5a6268', width=15, height=1, 
                 border=0, command=self.mostrar_configuracion_servidor).pack(pady=10)
    
    def validate_code(self, value):
        return len(value) <= 6 and (value.isdigit() or value == "")
    
    def validar_codigo_sesion(self):
        codigo = self.code_entry.get().strip()
        
        if len(codigo) != 6:
            messagebox.showerror("Error", "Debe ingresar un código de 6 dígitos")
            return
        
        print(f"Validando código: {codigo}")
        threading.Thread(target=self._validar_sesion_thread, 
                        args=(codigo,), daemon=True).start()
    
    def _validar_sesion_thread(self, codigo):
        try:
            print("Intentando validación TI...")
            response = requests.post(
                f"{self.server_url}/api/tarjeta/validar-sesion", 
                data={'sessionCode': codigo}, 
                timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                print(f"Respuesta TI: {data}")
                if data.get('valid') and data.get('role') == 'TI':
                    print("Sesión TI válida")
                    self.session_data = data
                    self.root.after(0, self.mostrar_modo_ti)
                    return
            elif response.status_code == 404:
                print("No hay tarjetas pendientes para TI")
            
            print("Intentando validación Admin...")
            response = requests.post(
                f"{self.server_url}/api/tarjeta/validar-sesion-admin", 
                data={'sessionCode': codigo}, 
                timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                print(f"Respuesta Admin: {data}")
                if data.get('valid') and data.get('role') == 'Admin':
                    print("Sesión Admin válida")
                    self.session_data = data
                    self.root.after(0, self.mostrar_modo_admin)
                    return
            
            print("Código de sesión inválido")
            self.root.after(0, lambda: messagebox.showerror(
                "Error", "Código inválido o no hay tarjetas pendientes"))
            
        except Exception as e:
            print(f"Error validando sesión: {e}")
            self.root.after(0, lambda: messagebox.showerror(
                "Error", f"Error de conexión: {str(e)}"))
    
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
        
        # Info con UID REAL
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
                                      text="Acerca la tarjeta MIFARE al lector...", 
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
        
        tk.Button(main_frame, text="Volver al Inicio", 
                 font=('Arial', 11), bg='#6c757d', fg='white', 
                 activebackground='#5a6268', width=15, height=2, 
                 border=0, command=self.mostrar_pantalla_codigo).pack(pady=10)
    
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
                    print("Verificación exitosa")
                    
                    self.root.after(0, lambda: self.ti_status_label.config(
                        text="Confirmando con servidor...", fg='#17a2b8'))
                    
                    response = requests.post(f"{self.server_url}/api/tarjeta/confirmar", 
                                           data={'uidLeido': uid_leido}, timeout=10)
                    
                    print(f"Confirmación: Status {response.status_code}")
                    
                    if response.status_code == 200:
                        print("PROCESO COMPLETADO")
                        self.root.after(0, lambda: self.ti_status_label.config(
                            text="COMPLETADO - Tarjeta activada", fg='#28a745'))
                        self.root.after(0, lambda: self.verify_button.config(
                            state='disabled', text='Verificar y Confirmar'))
                        self.root.after(3000, self.mostrar_pantalla_codigo)
                    else:
                        print(f"Error servidor: {response.status_code}")
                        self.root.after(0, lambda: messagebox.showerror("Error", 
                            f"Error servidor: {response.status_code}"))
                else:
                    print("UIDs no coinciden")
                    self.root.after(0, lambda: messagebox.showerror("Error", 
                        f"UIDs no coinciden"))
            else:
                print("No se pudo leer tarjeta")
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    "No se pudo leer la tarjeta"))
                
        except Exception as e:
            print(f"Error verificando: {e}")
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))
        finally:
            self.root.after(0, lambda: self.verify_button.config(
                state='normal', text='Verificar y Confirmar'))
    
    def mostrar_modo_admin(self):
        for widget in self.root.winfo_children():
            widget.destroy()
        
        main_frame = tk.Frame(self.root, bg='#343a40')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        tk.Label(main_frame, text="Modo Admin - Autenticación 2FA", 
                font=('Arial', 18, 'bold'), 
                fg='white', bg='#28a745').pack(pady=20)
        
        nav_id = self.session_data.get('navId', 'N/A')
        admin_nombre = self.session_data.get('adminNombre', 'N/A')
        
        tk.Label(main_frame, text=f"Admin: {admin_nombre}", 
                font=('Arial', 12), fg='#f8f9fa', bg='#343a40').pack(pady=5)
        
        tk.Label(main_frame, text=f"NavID: {str(nav_id)[:8]}...", 
                font=('Arial', 10), fg='#adb5bd', bg='#343a40').pack(pady=5)
        
        self.admin_status_label = tk.Label(main_frame, 
                                         text="Acerca su tarjeta de administrador...", 
                                         font=('Arial', 14), fg='#28a745', bg='#343a40')
        self.admin_status_label.pack(pady=30)
        
        tk.Button(main_frame, text="Volver al Inicio", 
                 font=('Arial', 11), bg='#6c757d', fg='white', 
                 width=15, height=2, 
                 command=self.mostrar_pantalla_codigo).pack(pady=20)
        
        threading.Thread(target=self.proceso_admin, daemon=True).start()
    
    def proceso_admin(self):
        if not HARDWARE_AVAILABLE or not self.reader:
            self.root.after(0, lambda: messagebox.showerror(
                "Error", "Hardware RC522 no disponible"))
            return
        
        try:
            # LEER AMBOS: UID físico Y texto escrito
            uid_fisico, texto_leido = self.reader.read_text_from_card(timeout=25)
            
            if not uid_fisico or not texto_leido:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    "No se pudo leer la tarjeta"))
                return
            
            self.root.after(0, lambda: self.admin_status_label.config(
                text=f"Verificando tarjeta...", fg='#ffc107'))
            
            # ENVIAR AMBOS valores al servidor
            response = requests.post(f"{self.server_url}/Login/Confirmar2FA", 
                                data={
                                    'navId': self.session_data.get('navId', ''),
                                    'uidFisico': uid_fisico,        # UID del chip
                                    'textoEscrito': texto_leido     # UID que escribimos
                                }, timeout=10)
            
            if response.status_code == 200:
                self.root.after(0, lambda: self.admin_status_label.config(
                    text="Autenticación 2FA exitosa", fg='#28a745'))
                self.root.after(3000, self.mostrar_pantalla_codigo)
            else:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    f"Tarjeta no autorizada: {response.text}"))
                
        except Exception as e:
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))

    
    def run(self):
        print("=" * 60)
        print("QuickTable NFC - Sistema Táctil")
        print(f"Hardware RC522: {'Disponible' if HARDWARE_AVAILABLE and self.reader else 'No disponible'}")
        print("=" * 60)
        
        servidor_previo = self.cargar_configuracion_previa()
        if servidor_previo:
            self.server_url = servidor_previo
            self.setup_window()
            self.mostrar_pantalla_codigo()
        else:
            self.mostrar_configuracion_servidor()
        
        try:
            self.root.mainloop()
        finally:
            if self.reader:
                self.reader.cleanup()
            print("Aplicación cerrada")

if __name__ == '__main__':
    try:
        app = QuickTableNFCApp()
        app.run()
    except Exception as e:
        print(f"Error fatal: {e}")
        import sys
        sys.exit(1)
