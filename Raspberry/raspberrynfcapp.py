#!/usr/bin/env python3
"""
QuickTable NFC - Sistema completo tactil
Version: SIMPLIFICADO - Sin config servidor, con boton regreso
"""

import tkinter as tk
from tkinter import messagebox
import requests
import threading
import time
import json
import os
import sys

# SIEMPRE en pantalla completa cuando se ejecuta desde control_acceso.py
FULLSCREEN_MODE = True

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
                print("Timeout en operacion")
                return False
                
        except Exception as e:
            print(f"Error critico: {e}")
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
                    print(f"Leido - UID: {uid_string}, Texto: '{text}'")
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

# Teclado numerico tactil
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
        elif key.isdigit():
            if len(current) < 15:
                self.entry.insert(tk.END, key)

# Aplicacion principal
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
        """Configurar ventana principal SOLO UNA VEZ"""
        if self.root is None:
            self.root = tk.Tk()
            self.root.title("QuickTable NFC")
            
            # PANTALLA COMPLETA SIEMPRE
            self.root.attributes('-fullscreen', True)
            self.root.bind('<Escape>', lambda e: self.root.attributes('-fullscreen', False))
            self.root.bind('<F11>', lambda e: self.root.attributes('-fullscreen', not self.root.attributes('-fullscreen')))
            
            self.root.resizable(False, False)
            self.root.configure(bg='#343a40')
            self.root.bind('<Escape>', self.on_escape_key)
            self.root.protocol("WM_DELETE_WINDOW", self.salir_aplicacion)
            
            print("Iniciando en pantalla completa")
    
    def on_escape_key(self, event=None):
        """Manejar tecla Escape - solo salir de pantalla completa"""
        if self.root.attributes('-fullscreen'):
            self.root.attributes('-fullscreen', False)
    
    def salir_aplicacion(self):
        """Salir de la aplicacion completamente"""
        print("Cerrando aplicacion...")
        if self.reader:
            self.reader.cleanup()
        if self.root:
            self.root.quit()
            self.root.destroy()
        sys.exit(0)
    
    def regresar_control_acceso(self):
        """Regresar al menu principal (control_acceso.py)"""
        print("Regresando al control de acceso...")
        if self.reader:
            self.reader.cleanup()
        if self.root:
            self.root.destroy()
        # Salir limpio para que control_acceso.py detecte el regreso
        sys.exit(0)
    
    def cargar_configuracion_previa(self):
        """Cargar configuracion del servidor desde config.json"""
        try:
            if os.path.exists('config.json'):
                with open('config.json', 'r') as f:
                    config = json.load(f)
                    url = config.get('server_url', '')
                    if url:
                        print(f"Configuracion cargada: {url}")
                        return url
        except Exception as e:
            print(f"Error cargando configuracion: {e}")
        return 'http://192.168.1.100:5000'  # Default
    
    def mostrar_pantalla_codigo(self):
        """Pantalla principal de codigo de sesion"""
        self.setup_window()  # Asegurar ventana existe
        
        for widget in self.root.winfo_children():
            widget.destroy()
        
        main_frame = tk.Frame(self.root, bg='#343a40')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        tk.Label(main_frame, text="Codigo de Sesion", 
                font=('Arial', 24, 'bold'), 
                fg='white', bg='#343a40').pack(pady=(0, 20))
        
        tk.Label(main_frame, text="Ingrese el codigo de 6 digitos", 
                font=('Arial', 12), 
                fg='#adb5bd', bg='#343a40').pack(pady=10)
        
        # Campo codigo
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
        
        # Info servidor y botones
        info_frame = tk.Frame(main_frame, bg='#343a40')
        info_frame.pack(pady=(20, 0))
        
        tk.Label(info_frame, text=f"Servidor: {self.server_url}", 
                font=('Arial', 9), fg='#6c757d', bg='#343a40').pack(pady=(0, 10))
        
        # BOTON REGRESO A CONTROL DE ACCESO
        tk.Button(info_frame, text="Regresar al Control de Acceso", 
                 font=('Arial', 14, 'bold'), bg='#17a2b8', fg='white', 
                 activebackground='#138496', width=25, height=2, 
                 border=0, command=self.regresar_control_acceso).pack(pady=10)
    
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
                 border=0, command=self.regresar_control_acceso).pack(side='right', padx=10)
    
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
                    
                    self.root.after(3000, self.regresar_control_acceso)
                else:
                    self.root.after(0, lambda: messagebox.showerror("Error", 
                        data.get('message', 'Error desconocido')))
            else:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    f"Error del servidor: {response.status_code}"))
        except Exception as e:
            print(f"Error asignando tarjeta empleado: {e}")
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))
    
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
        
        tk.Button(main_frame, text="Regresar", 
                 font=('Arial', 11), bg='#17a2b8', fg='white', 
                 activebackground='#138496', width=15, height=2, 
                 border=0, command=self.regresar_control_acceso).pack(pady=10)
    
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
                        self.root.after(3000, self.regresar_control_acceso)
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
    
    def mostrar_modo_admin(self):
        for widget in self.root.winfo_children():
            widget.destroy()
        
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
        
        self.admin_status_label = tk.Label(main_frame, 
                                         text="Acerca su tarjeta de administrador...", 
                                         font=('Arial', 14), fg='#28a745', bg='#343a40')
        self.admin_status_label.pack(pady=30)
        
        tk.Button(main_frame, text="Regresar", 
                 font=('Arial', 11), bg='#17a2b8', fg='white', 
                 width=15, height=2, 
                 command=self.regresar_control_acceso).pack(pady=20)
        
        threading.Thread(target=self.proceso_admin, daemon=True).start()
    
    def proceso_admin(self):
        if not HARDWARE_AVAILABLE or not self.reader:
            self.root.after(0, lambda: messagebox.showerror(
                "Error", "Hardware RC522 no disponible"))
            return
        
        try:
            uid_fisico, texto_leido = self.reader.read_text_from_card(timeout=25)
            
            if not uid_fisico or not texto_leido:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    "No se pudo leer la tarjeta"))
                return
            
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
                self.root.after(3000, self.regresar_control_acceso)
            else:
                self.root.after(0, lambda: messagebox.showerror("Error", 
                    f"Tarjeta no autorizada: {response.text}"))
                
        except Exception as e:
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))
    
    def run(self):
        print("=" * 60)
        print("QuickTable NFC - Sistema Tactil Simplificado")
        print(f"Hardware RC522: {'Disponible' if HARDWARE_AVAILABLE and self.reader else 'No disponible'}")
        print("PANTALLA COMPLETA AUTOMATICA")
        print("=" * 60)
        
        # Cargar servidor desde config.json
        self.server_url = self.cargar_configuracion_previa()
        self.mostrar_pantalla_codigo()
        
        try:
            self.root.mainloop()
        finally:
            if self.reader:
                self.reader.cleanup()
            print("Aplicacion cerrada")

if __name__ == '__main__':
    try:
        app = QuickTableNFCApp()
        app.run()
    except KeyboardInterrupt:
        print("Aplicacion interrumpida por usuario")
        sys.exit(0)
    except Exception as e:
        print(f"Error fatal: {e}")
        sys.exit(1)
