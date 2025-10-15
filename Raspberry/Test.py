python3 -c "
from mfrc522 import SimpleMFRC522
import RPi.GPIO as GPIO
GPIO.setwarnings(False)
reader = SimpleMFRC522()
print('Acerca una tarjeta por 5 segundos...')
try:
    id, text = reader.read_no_block()
    if id:
        print(f'UID: {id:016X}')
        print(f'Texto: {text}')
    else:
        print('No se detectó tarjeta')
except Exception as e:
    print(f'Error: {e}')
finally:
    GPIO.cleanup()
"
