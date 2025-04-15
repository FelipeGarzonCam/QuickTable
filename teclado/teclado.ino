#include <ESP8266WiFi.h>
#include <Keypad.h>
#include <ESP8266HTTPClient.h>
#include <Wire.h>                 // Librería para comunicación I2C
#include "SSD1306Wire.h"         // Librería para el display
#include <ArduinoJson.h>

// Configuración del teclado matricial
const byte FILAS = 4; // Cuatro filas
const byte COLUMNAS = 4; // Cuatro columnas

char teclas[FILAS][COLUMNAS] = {
  {'1', '2', '3', 'A'},
  {'4', '5', '6', 'B'},
  {'7', '8', '9', 'C'},
  {'*', '0', '#', 'D'}
};

byte pinesFilas[FILAS] = {4, 0, 2, 14};
byte pinesColumnas[COLUMNAS] = {12, 13, 15, 5};

Keypad teclado = Keypad(makeKeymap(teclas), pinesFilas, pinesColumnas, FILAS, COLUMNAS);

// Credenciales WiFi
const char* ssid = "FCAM_2.4";
const char* password = "Flor1000725202";

// Dirección del servidor
const char* serverUrl = "http://192.168.101.11:5000";

// Variables para el manejo del pedido y login
enum Mode { INGRESAR_CLIENTID, LOGIN, COCINA };
Mode currentMode = INGRESAR_CLIENTID;

// Client ID para asociar con el navegador
String clientId = "";

// Enumeración para los campos en modo LOGIN
enum Field { NONE, USUARIO, CONTRASENA };
Field selectedField = NONE;

// Clase para manejar la pantalla OLED
class MyOLEDDisplay {
  private:
    SSD1306Wire display;
    int cursorY;
    const int maxLines;
  
  public:
    MyOLEDDisplay(uint8_t i2c_address, uint8_t sda, uint8_t scl, int max_display_lines = 4)
      : display(i2c_address, sda, scl), cursorY(0), maxLines(max_display_lines) {}
    
    void initDisplay() {
      display.init();                     // Inicializa el display
      display.flipScreenVertically();     // Rota la orientación del display
      display.clear();                    // Limpia cualquier impresión previa en el display 
      display.setFont(ArialMT_Plain_16); // Establece una fuente legible
      display.setTextAlignment(TEXT_ALIGN_LEFT);
      display.display();
      cursorY = 0;
    }
    
    void clearDisplay() {
      display.clear();
      cursorY = 0;
    }
    
    void println(String text) {
      clearDisplay();
      if (cursorY >= maxLines * 6) { // Asumiendo altura de fuente de 9px
        scrollUp();
      }
      display.drawString(0, cursorY, text);
      display.display();
      cursorY += 10;
    }
    
    void print(String text) {
      // Implementar si se desea manejar impresión continua
      // Por simplicidad, se usa println
      println(text);
    }
    
  private:
    void scrollUp() {
      // Simplemente limpia la pantalla y resetea el cursor
      clearDisplay();
    }
};

// Inicializa la pantalla OLED con la dirección I2C y los pines SDA y SCL
const int SDA_PIN = 3; 
const int SCL_PIN = 1;
MyOLEDDisplay oled(0x3c, SDA_PIN, SCL_PIN);

void setup() {
  Serial.begin(115200);
  oled.initDisplay();
  oled.print("Iniciando...");

  // Conectar a la red WiFi
  WiFi.begin(ssid, password);
  
  oled.print("Conectando a\nWiFi");
  Serial.print("Conectando a WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);    
    Serial.print(".");
  }
  oled.println("\nConectado a la red WiFi");
  Serial.println("\nConectado a la red WiFi");
  delay(300);  
 
  oled.print("Ingrese Client ID\ny luego marque\n'D' para continuar");    
}

void loop() {
  char tecla = teclado.getKey(); // Leer la tecla presionada

  if (tecla) {
    // Corregir manualmente las teclas problemáticas
    if (tecla == '1' && digitalRead(pinesFilas[0]) == LOW && digitalRead(pinesColumnas[2]) == LOW) {
      tecla = '3';
    }
    if (tecla == '*' && digitalRead(pinesFilas[3]) == LOW && digitalRead(pinesColumnas[2]) == LOW) {
      tecla = '#';
    }

    // Mostrar la tecla presionada 
    String teclaStr = String(tecla);
    Serial.print("Tecla presionada: ");
    Serial.println(teclaStr);
    // oled.println("Tecla presionada: " + teclaStr);

    // Manejar la tecla según el modo actual
    manejarTecla(tecla);
  }
}

// Función para manejar la tecla presionada
void manejarTecla(char tecla) {
  if (WiFi.status() != WL_CONNECTED) {   
    oled.println("WiFi no está conectado!");
    return;
  }

  if (currentMode == INGRESAR_CLIENTID) {
    manejarTeclaClientId(tecla);
  } else if (currentMode == LOGIN) {
    manejarTeclaLogin(tecla);
  } else if (currentMode == COCINA) {
    manejarTeclaCocina(tecla);
  }
}

// Funciones para ingresar el clientId
void manejarTeclaClientId(char tecla) {
  if (isDigit(tecla)) {
    // Agregar dígito al clientId
    if (clientId.length() < 6) { // Limitar a 6 dígitos
      clientId += tecla;
      Serial.print("clientId actual: ");
      Serial.println(clientId);
      oled.print("ClientID Actual:\n" + clientId); 
      if (clientId.length() == 6){
      oled.print("Client ID: " + clientId + "\n'D' para\ncontinuar");      
      }     
    } else {
      Serial.println("Client ID ya tiene 6 dígitos.");
      oled.println("Client ID ya tiene 6 dígitos.");
      // Opcional: Indicar al usuario que presione 'D' para confirmar
    }
  } else if (tecla == 'D') {
    // Confirmar el clientId ingresado
    if (clientId.length() == 6) { 
      currentMode = LOGIN; // Cambiar al modo LOGIN     
      oled.println("Cambiando al\nmodo\nLOGIN");
      delay(500); 
      
      oled.println("'A'=Nombre\n'B'=Contraseña\n'C'=Borrar |'D'=Ok");  
    } else {
      Serial.println("El clientId debe tener 6 dígitos. Inténtelo de nuevo.");
      oled.println("El clientId debe tener 6 dígitos.\nInténtelo de nuevo.");
      clientId = ""; // Reiniciar el clientId
    }
  } else if (tecla == 'C') {
    // Borrar el último dígito ingresado
    if (clientId.length() > 0) {
      clientId.remove(clientId.length() - 1);
      Serial.print("clientID actual: ");
      Serial.println(clientId);
      oled.println("ClientId Actual:\n" + clientId);
    }
  } else {
    Serial.println("Tecla no válida en modo INGRESAR_CLIENTID");
    oled.println("Tecla no válida");
  }
}

// Funciones para el modo LOGIN
void manejarTeclaLogin(char tecla) {
  switch (tecla) {
    case 'A':
      selectedField = USUARIO;
      Serial.println("Campo de usuario seleccionado");
      oled.println("Campo 'Nombre'\nseleccionado");
      seleccionarCampoUsuario();
      break;
    case 'B':
      selectedField = CONTRASENA;
      Serial.println("Campo de contraseña seleccionado");
      oled.println("Campo\n'contraseña'\nseleccionado");
      seleccionarCampoContrasena();
      break;
    case 'C':
      if (selectedField != NONE) {
        borrarCampoSeleccionado();
        Serial.println("Contenido del campo borrado");
        oled.println("Contenido del\ncampo borrado");
      } else {
        Serial.println("No hay campo seleccionado para borrar");
        oled.println("No hay campo\nseleccionado\npara borrar");
      }
      break;
    case 'D':
      iniciarSesion();
      break;
    default:
      if (isDigit(tecla)) {
        if (selectedField != NONE) {
          enviarCaracterCampoSeleccionado(tecla);
          Serial.print("Enviando dígito '");
          Serial.print(tecla);
          Serial.println("' al campo seleccionado");         
        } else {
          Serial.println("No hay campo seleccionado para ingresar texto");
          oled.println("No hay campo\nseleccionado\npara ingresar texto");
        }
      } else {
        Serial.println("Tecla no asignada en modo LOGIN");
        oled.println("Tecla no asignada\nen modo LOGIN");
      }
      break;
  }
}

// Funciones para el modo COCINA
void manejarTeclaCocina(char tecla) {
  switch (tecla) {
    case 'A':
      abrirModalPedido();
      break;
    case 'B':
      cerrarModalPedido();
      break;
    case 'C':
      borrarContenidoModal();
      break;
    case 'D':
      marcarPedidoModalComoListo();
      break;
    case '*':
      cerrarSesion();
      break;
    default:
      if (isDigit(tecla)) {
        ingresarDigitoEnModal(tecla);
      } else {
        Serial.println("Tecla no asignada en modo COCINA");
        oled.println("Tecla no asignada\nen modo COCINA");
      }
      break;
  }
}

void cerrarSesion() {
  if (enviarPeticion("/Login/CerrarSesion")) {
    currentMode = INGRESAR_CLIENTID;
    clientId = "";
    oled.println("Sesión cerrada.\nIngrese Client ID\ny luego marque\n'D' para continuar");
  } else {
    oled.println("Error al cerrar\nsesión.");
  }
}


// Funciones para interactuar con el servidor en modo LOGIN
void seleccionarCampoUsuario() {
  enviarPeticion("/Login/SeleccionarCampoUsuario");
}

void seleccionarCampoContrasena() {
  enviarPeticion("/Login/SeleccionarCampoContrasena");
}

void borrarCampoSeleccionado() {
  if (selectedField == USUARIO) {
    enviarPeticion("/Login/BorrarCampoUsuario");
  } else if (selectedField == CONTRASENA) {
    enviarPeticion("/Login/BorrarCampoContrasena");
  }
}

void enviarCaracterCampoSeleccionado(char c) {
  String ruta = selectedField == USUARIO
                    ? "/Login/IngresarCaracterUsuario"
                    : "/Login/IngresarCaracterContrasena";
  enviarPeticionPOST(ruta, "caracter=" + String(c));
}

void iniciarSesion() {
  if (enviarPeticion("/Login/IniciarSesion")) {
    Serial.println("Intentando iniciar sesión...");
    oled.println("Intentando iniciar\nsesión...");
    // verifica si el inicio de sesión fue exitoso
    bool loginSuccess = false;
    int retries = 10;
    while (retries-- > 0) {
      delay(1000); // Esperar 1 segundo
      int loginStatus = checkLoginStatus();
      if (loginStatus == 1) {
        // Inicio de sesión exitoso
        currentMode = COCINA;
        Serial.println("Sesión iniciada. Cambiando a modo COCINA...");
        oled.println("Sesión iniciada.\nModo COCINA.");
        loginSuccess = true;
        break;
      } else if (loginStatus == 0) {
        // Inicio de sesión fallido
        Serial.println("Error de inicio de sesión.");
        oled.println("Error de inicio\nde sesión.");
        break;
      } else {
        // Aún pendiente, continuar consultando
        Serial.println("Esperando resultado de inicio de sesión...");
        oled.println("Esperando resultado\ninicio de sesión...");
      }
    }
    if (!loginSuccess) {
      // Regresar al modo LOGIN
      currentMode = LOGIN;
    }
  }
}

int checkLoginStatus() {
  HTTPClient http;
  WiFiClient client;
  String url = String(serverUrl) + "/Login/CheckLoginStatus?clientId=" + clientId;
  http.begin(client, url);

  int httpCode = http.GET();
  if (httpCode > 0) {
    String payload = http.getString();
    Serial.print("Respuesta de CheckLoginStatus: ");
    Serial.println(payload);

    // Analizar la respuesta JSON
    DynamicJsonDocument doc(1024);
    DeserializationError error = deserializeJson(doc, payload);
    if (!error) {
      bool success = doc["success"];
      bool pending = doc["pending"];
      if (success) {
        http.end();
        return 1; // Inicio de sesión exitoso
      } else if (!pending) {
        http.end();
        return 0; // Inicio de sesión fallido
      }
      // Si está pendiente, continuar
    } else {
      Serial.println("Error al parsear JSON.");
    }
  } else {
    Serial.print("Error en la petición GET: ");
    Serial.println(httpCode);
  }
  http.end();
  return -1; // Pendiente o error
}


// Funciones para interactuar con el servidor en modo COCINA
void abrirModalPedido() {
  enviarPeticion("/Cocina/AbrirModalPedido");
}

void cerrarModalPedido() {
  enviarPeticion("/Cocina/CerrarModalPedido");
}

void borrarContenidoModal() {
  enviarPeticion("/Cocina/BorrarContenidoModal");
}

void ingresarDigitoEnModal(char c) {
  enviarPeticionPOST("/Cocina/IngresarDigitoModal", "digito=" + String(c));
}

void marcarPedidoModalComoListo() {
  enviarPeticion("/Cocina/MarcarPedidoModalComoListo");
}

// Funciones auxiliares para realizar solicitudes HTTP
bool enviarPeticion(String ruta) {
  HTTPClient http;
  WiFiClient client;
  String url = String(serverUrl) + ruta + "?clientId=" + clientId;
  http.begin(client, url);

  int httpCode = http.GET();
  http.end();

  if (httpCode > 0) {
    Serial.print("Petición GET exitosa: ");
    Serial.println(httpCode);
   // oled.println("GET: " + String(httpCode));
    return true;
  } else {
    Serial.print("Error en la petición GET: ");
    Serial.println(httpCode);
    //oled.println("Error GET: " + String(httpCode));
    return false;
  }
}

bool enviarPeticionPOST(String ruta, String data) {
  HTTPClient http;
  WiFiClient client;
  String url = String(serverUrl) + ruta;
  http.begin(client, url);
  http.addHeader("Content-Type", "application/x-www-form-urlencoded");

  // Agregar clientId al data
  data += "&clientId=" + clientId;

  int httpCode = http.POST(data);
  http.end();

  if (httpCode > 0) {
    Serial.print("Petición POST exitosa: ");
    Serial.println(httpCode);
    //oled.println("POST: " + String(httpCode));
    return true;
  } else {
    Serial.print("Error en la petición POST: ");
    Serial.println(httpCode);
    //oled.println("Error POST: " + String(httpCode));
    return false;
  }
}
