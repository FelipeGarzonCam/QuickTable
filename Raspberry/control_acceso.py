#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
QuickTable Control de Acceso
Sistema de control de asistencia con tarjetas NFC
Version: COMPLETO TACTIL - IP y Puerto separados
"""

import tkinter as tk
from tkinter import messagebox, font
import subprocess
import sys
import os
import threading
import time
import json
import requests
from datetime import datetime

# Importar la clase QuickTableRFID existente
try:
    from quicktablerfid import QuickTableRFID
    HARDWARE_AVAILABLE = True
except ImportError:
    print("Hardware RC522 no disponible")
    HARDWARE_AVAILABLE = False
    QuickTableRFID = None

class QuickTableControlAcceso:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("QuickTable - Control de Acceso")
        self.root.geometry("800x600")
        self.root.configure(bg='#212529')
        
        # Configurar para pantalla completa
        self.root.attributes('-fullscreen', True)
        self.root.bind('<Escape>', self.exit_fullscreen)
        self.root.bind('<F11>', self.toggle_fullscreen)
        
        # Variable de instancia para hardware
        self.hardware_available = HARDWARE_AVAILABLE
        
        # Variable para campo activo del teclado
        self.active_entry_type = 'ip'
        
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
                # Intentar ping basico al servidor
                response = requests.get(self.server_url, timeout=3)
                return response.status_code in [200, 404, 403]
            except:
                return False
    
    def mostrar_configuracion_servidor(self):
        """Pantalla TACTIL para configurar servidor - IP y Puerto separados"""
        # Limpiar ventana
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Frame principal horizontal
        main_frame = tk.Frame(self.root, bg='#212529')
        main_frame.pack(fill='both', expand=True, padx=20, pady=20)
        
        # Titulo centrado
        title_frame = tk.Frame(main_frame, bg='#212529')
        title_frame.pack(fill='x', pady=(0, 20))
        
        tk.Label(
            title_frame,
            text="QuickTable - Configuracion",
            font=('Arial', 24, 'bold'),
            fg='#007bff',
            bg='#212529'
        ).pack()
        
        # Frame contenedor horizontal  
        content_frame = tk.Frame(main_frame, bg='#212529')
        content_frame.pack(fill='both', expand=True)
        
        # COLUMNA IZQUIERDA - FORMULARIOS
        left_frame = tk.Frame(content_frame, bg='#495057', relief='raised', bd=2)
        left_frame.pack(side='left', fill='both', expand=True, padx=(0, 10))
        
        tk.Label(
            left_frame,
            text="Configuracion del Servidor",
            font=('Arial', 16, 'bold'),
            fg='white',
            bg='#495057'
        ).pack(pady=15)
        
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
        
        # Campo Puerto
        tk.Label(fields_frame, text="Puerto:", 
                font=('Arial', 12, 'bold'), fg='#f8f9fa', bg='#495057').pack(anchor='w')
        
        self.server_port_entry = tk.Entry(fields_frame, font=('Arial', 14), width=20,
                                        justify='center', bg='white', fg='#495057',
                                        relief='solid', bd=1)
        self.server_port_entry.pack(pady=(3, 15), ipady=4)
        self.server_port_entry.bind('<FocusIn>', lambda e: self.set_active_entry('port'))
        
        # Cargar valores actuales
        try:
            import urllib.parse
            parsed = urllib.parse.urlparse(self.server_url)
            self.server_ip_entry.insert(0, parsed.hostname or "192.168.1.100")
            self.server_port_entry.insert(0, str(parsed.port) if parsed.port else "5000")
        except:
            self.server_ip_entry.insert(0, "192.168.1.100")
            self.server_port_entry.insert(0, "5000")
        
        # Estado conexion
        self.connection_status = tk.Label(left_frame, 
                                        text="Configure servidor para continuar", 
                                        font=('Arial', 10), fg='#adb5bd', bg='#495057',
                                        wraplength=250)
        self.connection_status.pack(pady=10)
        
        # Botones en linea horizontal
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
        """Teclado tactil copiado EXACTO de raspberrynfcapp"""
        keyboard_frame = tk.Frame(parent, bg='#212529')
        keyboard_frame.pack(pady=20)
        
        tk.Label(keyboard_frame, text="Teclado Numerico", 
                font=('Arial', 14, 'bold'), 
                fg='white', bg='#212529').pack(pady=(0, 10))
        
        # Indicador de campo activo
        self.active_indicator = tk.Label(keyboard_frame, 
                                    text="Editando: IP", 
                                    font=('Arial', 11, 'bold'), 
                                    fg='#17a2b8', bg='#212529')
        self.active_indicator.pack(pady=(0, 8))
        
        buttons = [
            ['1', '2', '3'],
            ['4', '5', '6'], 
            ['7', '8', '9'],
            ['Borrar', '0', 'Punto'],
            ['IP', 'Puerto', 'Limpiar']
        ]
        
        for row in buttons:
            row_frame = tk.Frame(keyboard_frame, bg='#212529')
            row_frame.pack(pady=2)
            
            for btn_text in row:
                if btn_text == 'IP':
                    bg_color = '#17a2b8'
                    width = 9
                elif btn_text == 'Puerto':
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
        """Manejo de teclas del teclado tactil"""
        # Obtener campo activo
        if self.active_entry_type == 'ip':
            current_entry = self.server_ip_entry
            self.active_indicator.config(text="Editando: IP", fg='#17a2b8')
        else:
            current_entry = self.server_port_entry  
            self.active_indicator.config(text="Editando: Puerto", fg='#6f42c1')
        
        current = current_entry.get()
        
        if key == 'IP':
            self.active_entry_type = 'ip'
            self.server_ip_entry.focus()
            self.server_ip_entry.icursor(tk.END)
            self.active_indicator.config(text="Editando: IP", fg='#17a2b8')
        elif key == 'Puerto':
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
            if len(current) < 15 and '.' not in current[-3:]:
                current_entry.insert(tk.END, '.')
        elif key.isdigit():
            if len(current) < 15:
                current_entry.insert(tk.END, key)
    
    def probar_conexion(self):
        """Probar conexion con IP y Puerto separados"""
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
        """Hilo para probar conexion"""
        try:
            print(f"Probando conexion a: {test_url}")
            response = requests.get(f"{test_url}/api/health", timeout=5)
            
            if response.status_code == 200:
                self.root.after(0, lambda: self.connection_status.config(
                    text="Conexion exitosa", fg='#28a745'))
                self.server_url = test_url
                print(f"Servidor OK: {test_url}")
            else:
                # Intentar ping basico
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
        """Conectar con IP y Puerto separados"""
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
    
    def mostrar_pantalla_principal(self):
        """Pantalla principal con dos opciones grandes"""
        # Limpiar ventana
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Frame principal centrado
        main_frame = tk.Frame(self.root, bg='#212529')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        # Titulo principal
        title_font = font.Font(family="Arial", size=28, weight="bold")
        tk.Label(main_frame, 
                text="QuickTable Control de Acceso", 
                font=title_font, 
                fg='#ffffff', 
                bg='#212529').pack(pady=30)
        
        # Subtitulo con estado del hardware
        subtitle_font = font.Font(family="Arial", size=14)
        if self.hardware_available and self.reader:
            status_text = "Hardware RC522 Disponible"
            status_color = '#28a745'
        else:
            status_text = "Hardware RC522 No disponible"
            status_color = '#dc3545'
        
        tk.Label(main_frame, 
                text=status_text, 
                font=subtitle_font, 
                fg=status_color, 
                bg='#212529').pack(pady=10)
        
        # Frame para botones principales
        buttons_frame = tk.Frame(main_frame, bg='#212529')
        buttons_frame.pack(pady=40)
        
        # Boton 1: Marcar Salida
        btn_font = font.Font(family="Arial", size=18, weight="bold")
        btn_asistencia = tk.Button(buttons_frame,
                                  text="MARCAR SALIDA\n\nAcerca tu tarjeta NFC\npara registrar tu salida",
                                  font=btn_font,
                                  bg='#007bff',
                                  fg='white',
                                  width=25,
                                  height=8,
                                  relief='raised',
                                  bd=3,
                                  command=self.mostrar_marcar_salida)
        btn_asistencia.pack(side='left', padx=20)
        
        # Boton 2: Modo Administracion
        btn_admin = tk.Button(buttons_frame,
                             text="MODO ADMINISTRACION\n\nAcceso completo al sistema\npara gestion y configuracion",
                             font=btn_font,
                             bg='#28a745',
                             fg='white',
                             width=25,
                             height=8,
                             relief='raised',
                             bd=3,
                             command=self.mostrar_modo_admin)
        btn_admin.pack(side='right', padx=20)
        
        # Frame para opciones adicionales
        options_frame = tk.Frame(main_frame, bg='#212529')
        options_frame.pack(pady=20)
        
        # Boton configurar servidor
        btn_config = tk.Button(options_frame,
                              text="Configurar Servidor",
                              font=("Arial", 14, "bold"),
                              bg='#6c757d',
                              fg='white',
                              width=20,
                              height=2,
                              command=self.mostrar_configuracion_servidor)
        btn_config.pack(side='left', padx=10)
        
        # Boton salir
        btn_salir = tk.Button(options_frame,
                             text="Salir",
                             font=("Arial", 14, "bold"),
                             bg='#dc3545',
                             fg='white',
                             width=20,
                             height=2,
                             command=self.salir_aplicacion)
        btn_salir.pack(side='right', padx=10)
        
        # Informacion del servidor
        info_font = font.Font(family="Arial", size=12)
        tk.Label(main_frame,
                text=f"Servidor: {self.server_url}",
                font=info_font,
                fg='#6c757d',
                bg='#212529').pack(pady=10)
    
    def mostrar_marcar_salida(self):
        """Pantalla para marcar salida con tarjeta NFC"""
        if not self.hardware_available or not self.reader:
            messagebox.showerror("Error", "Hardware RC522 no disponible")
            return
        
        # Limpiar ventana
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Frame principal
        main_frame = tk.Frame(self.root, bg='#212529')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        # Titulo
        title_font = font.Font(family="Arial", size=24, weight="bold")
        tk.Label(main_frame,
                text="Marcar Salida",
                font=title_font,
                fg='#ffffff',
                bg='#212529').pack(pady=30)
        
        # Icono (representado con texto)
        icon_font = font.Font(family="Arial", size=48)
        tk.Label(main_frame,
                text="[ NFC ]",
                font=icon_font,
                fg='#007bff',
                bg='#212529').pack(pady=20)
        
        # Instrucciones
        instr_font = font.Font(family="Arial", size=18)
        tk.Label(main_frame,
                text="Acerca tu tarjeta NFC al lector",
                font=instr_font,
                fg='#ffffff',
                bg='#212529').pack(pady=10)
        
        # Estado
        self.status_label = tk.Label(main_frame,
                                   text="Esperando tarjeta...",
                                   font=("Arial", 16),
                                   fg='#ffc107',
                                   bg='#212529')
        self.status_label.pack(pady=20)
        
        # Boton volver - MAS GRANDE Y TACTIL
        tk.Button(main_frame,
                 text="Volver al Inicio",
                 font=("Arial", 18, "bold"),
                 bg='#6c757d',
                 fg='white',
                 width=25,
                 height=3,
                 command=self.mostrar_pantalla_principal).pack(pady=30)
        
        # Iniciar proceso de lectura en hilo separado
        threading.Thread(target=self.proceso_marcar_salida, daemon=True).start()
    
    def proceso_marcar_salida(self):
        """Proceso para leer tarjeta y marcar salida"""
        if not self.hardware_available or not self.reader:
            self.root.after(0, lambda: messagebox.showerror("Error", "Hardware RC522 no disponible"))
            return
        
        try:
            self.root.after(0, lambda: self.status_label.config(text="Acerca tu tarjeta...", fg='#28a745'))
            
            # Leer UID de la tarjeta
            uid_fisico = self.reader.read_card_uid(timeout=30)
            
            if not uid_fisico:
                self.root.after(0, lambda: self.status_label.config(text="No se detectó tarjeta", fg='#dc3545'))
                return
            
            self.root.after(0, lambda: self.status_label.config(text="Procesando...", fg='#ffc107'))
            
            # Encriptar el UID
            uid_encriptado = self.encriptar_uid(uid_fisico)
            
            # Enviar al servidor
            response = requests.post(
                f'{self.server_url}/api/asistencia/marcar-salida',
                json={'uid': uid_encriptado},
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                if data['success']:
                    mensaje = f"Salida Registrada\n\n"
                    mensaje += f"Empleado: {data.get('nombre', 'N/A')}\n"
                    mensaje += f"Rol: {data.get('rol', 'N/A')}\n"
                    mensaje += f"Hora Ingreso: {data.get('horaIngreso', 'N/A')}\n"
                    mensaje += f"Hora Salida: {data.get('horaSalida', 'N/A')}\n"
                    mensaje += f"Tiempo Trabajado: {data.get('tiempoTrabajado', 'N/A')}"
                    
                    self.root.after(0, lambda: self.mostrar_resultado_exitoso(mensaje))
                else:
                    self.root.after(0, lambda: self.status_label.config(text=data.get('message', 'Error'), fg='#dc3545'))
            else:
                error_msg = f"Error del servidor: {response.status_code}"
                self.root.after(0, lambda: self.status_label.config(text=error_msg, fg='#dc3545'))
                
        except Exception as e:
            self.root.after(0, lambda: messagebox.showerror("Error", f"Error: {str(e)}"))
    
    def encriptar_uid(self, uid):
        """Encriptar UID usando el mismo metodo que el backend"""
        try:
            from crypto_helper import encrypt_uid
            return encrypt_uid(uid)
        except ImportError:
            print("crypto_helper no disponible, usando Base64 basico")
            import base64
            return base64.b64encode(uid.encode()).decode()
    
    def mostrar_resultado_exitoso(self, mensaje):
        """Mostrar resultado exitoso"""
        # Limpiar ventana
        for widget in self.root.winfo_children():
            widget.destroy()
        
        # Frame principal
        main_frame = tk.Frame(self.root, bg='#212529')
        main_frame.place(relx=0.5, rely=0.5, anchor='center')
        
        # Titulo exitoso
        title_font = font.Font(family="Arial", size=24, weight="bold")
        tk.Label(main_frame,
                text="Salida Registrada Exitosamente",
                font=title_font,
                fg='#28a745',
                bg='#212529').pack(pady=30)
        
        # Informacion detallada
        info_font = font.Font(family="Arial", size=16)
        tk.Label(main_frame,
                text=mensaje,
                font=info_font,
                fg='#ffffff',
                bg='#212529',
                justify='center').pack(pady=20)
        
        # Boton para regresar inmediatamente - MAS GRANDE
        tk.Button(main_frame,
                 text="Regresar al Inicio",
                 font=("Arial", 20, "bold"),
                 bg='#007bff',
                 fg='white',
                 width=25,
                 height=3,
                 command=self.mostrar_pantalla_principal).pack(pady=30)
        
        # Auto-retorno despues de 10 segundos
        countdown_label = tk.Label(main_frame,
                                  text="Regresando automaticamente en 10 segundos...",
                                  font=("Arial", 14),
                                  fg='#6c757d',
                                  bg='#212529')
        countdown_label.pack(pady=20)
        
        # Iniciar countdown
        self.countdown_timer(countdown_label, 10)
    
    def countdown_timer(self, label, seconds):
        """Timer para regresar automaticamente"""
        if seconds > 0:
            label.config(text=f"Regresando automaticamente en {seconds} segundos...")
            self.root.after(1000, lambda: self.countdown_timer(label, seconds - 1))
        else:
            self.mostrar_pantalla_principal()
    
    def mostrar_modo_admin(self):
        """Ejecutar el sistema admin completo existente"""
        try:
            # Ocultar ventana actual (NO destruir)
            self.root.withdraw()
            
            # Crear variable de entorno para indicar que debe regresar aqui
            env = os.environ.copy()
            env['QUICKTABLE_FULLSCREEN'] = '1'
            env['QUICKTABLE_RETURN_TO_MENU'] = '1'  # NUEVA VARIABLE
            
            # Ejecutar raspberrynfcapp en proceso separado
            process = subprocess.Popen([sys.executable, 'raspberrynfcapp.py'], 
                                     env=env,
                                     stdin=subprocess.PIPE,
                                     stdout=subprocess.PIPE,
                                     stderr=subprocess.PIPE)
            
            # Monitorear el proceso en hilo separado
            threading.Thread(target=self.monitorear_proceso_admin, args=(process,), daemon=True).start()
            
        except FileNotFoundError:
            messagebox.showerror("Error", "Sistema admin no encontrado (raspberrynfcapp.py)")
            self.root.deiconify()
        except Exception as e:
            messagebox.showerror("Error", f"Error ejecutando modo admin: {e}")
            self.root.deiconify()
    
    def monitorear_proceso_admin(self, process):
        """Monitorear el proceso del modo admin"""
        try:
            return_code = process.wait()  # Esperar a que termine
            print(f"raspberrynfcapp termino con codigo: {return_code}")
            
            # Siempre volver a mostrar la ventana principal
            self.root.after(0, self.volver_del_admin)
        except Exception as e:
            print(f"Error monitoreando proceso admin: {e}")
            self.root.after(0, self.volver_del_admin)
    
    def volver_del_admin(self):
        """Volver del modo admin a la pantalla principal"""
        self.root.deiconify()  # Mostrar la ventana
        self.root.lift()       # Traer al frente
        self.root.focus_force() # Forzar foco
        self.mostrar_pantalla_principal()
    
    def salir_aplicacion(self):
        """Salir de la aplicacion"""
        if messagebox.askyesno("Salir", "¿Estas seguro de que quieres salir?"):
            if self.reader:
                self.reader.cleanup()
            self.root.destroy()
            sys.exit(0)
    
    def run(self):
        """Ejecutar la aplicacion"""
        try:
            print("=== QuickTable Control de Acceso ===")
            print(f"Servidor: {self.server_url}")
            print(f"Hardware RC522: {'Disponible' if self.hardware_available and self.reader else 'No disponible'}")
            print("ESC = Salir pantalla completa, F11 = Alternar pantalla completa")
            print("=" * 50)
            
            self.root.mainloop()
        except Exception as e:
            print(f"Error en aplicacion: {e}")
        finally:
            if self.reader:
                self.reader.cleanup()
            print("Aplicacion cerrada")

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
