#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import http.server
import socketserver
import os
import urllib.parse
import html
import mimetypes
import io

class UTF8HTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        # Establecer codificación por defecto
        super().__init__(*args, **kwargs)
    
    def do_GET(self):
        # Decodificar correctamente la URL
        try:
            self.path = urllib.parse.unquote(self.path, encoding='utf-8')
        except UnicodeDecodeError:
            self.path = urllib.parse.unquote(self.path, encoding='latin-1')
        
        return super().do_GET()
    
    def list_directory(self, path):
        try:
            file_list = os.listdir(path)
        except OSError:
            self.send_error(404, "No permission to list directory")
            return None
        
        # Ordenar archivos
        file_list.sort(key=lambda a: a.lower())
        
        # Crear HTML con codificación UTF-8 explícita
        html_content = '''<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <title>Directory listing for {}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        a {{ text-decoration: none; color: #0066cc; }}
        a:hover {{ text-decoration: underline; }}
        .file {{ margin: 5px 0; }}
        .folder {{ font-weight: bold; color: #cc6600; }}
    </style>
</head>
<body>
    <h1>Directory listing for {}</h1>
    <hr>
'''.format(html.escape(self.path, quote=False), html.escape(self.path, quote=False))
        
        for name in file_list:
            fullname = os.path.join(path, name)
            displayname = linkname = name
            
            # Agregar "/" para directorios
            if os.path.isdir(fullname):
                displayname = name + "/"
                linkname = name + "/"
                css_class = "folder"
            else:
                css_class = "file"
            
            # Crear enlace con codificación UTF-8
            encoded_link = urllib.parse.quote(linkname.encode('utf-8'))
            html_content += '<div class="{}"><a href="{}">{}</a></div>\n'.format(
                css_class, encoded_link, html.escape(displayname, quote=False)
            )
        
        html_content += '''
    <hr>
    <small>Python HTTP Server</small>
</body>
</html>
'''
        
        # Convertir a bytes con codificación UTF-8
        encoded_content = html_content.encode('utf-8', 'replace')
        
        self.send_response(200)
        self.send_header("Content-type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(encoded_content)))
        self.end_headers()
        
        return io.BytesIO(encoded_content)
    
    def guess_type(self, path):
        """Adivinar tipo MIME con soporte UTF-8 para archivos de texto"""
        mimetype, encoding = mimetypes.guess_type(path)
        
        # Para archivos de texto, especificar charset UTF-8
        if mimetype and mimetype.startswith('text/'):
            mimetype += '; charset=utf-8'
        
        return mimetype

if __name__ == "__main__":
    PORT = 8000
    
    # Configurar el servidor
    with socketserver.TCPServer(("", PORT), UTF8HTTPRequestHandler) as httpd:
        print(f"Servidor HTTP ejecutándose en http://localhost:{PORT}")
        print("Presiona Ctrl+C para detener")
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nServidor detenido")
