# Project sponsor: N/A
# Email: N/A
# Creator: Cedar Lake Instruments LLC
# Date: June, 2024
#
# Description:
# Monitors digital input and sends BLE notification
# 
#
# I/O Connections:
# Arduino pin   - I/O description 
#
# Build configuration
# Board: Adafruit CircuitPython Playground Bluefruit

import random
import time
import board
import neopixel
import analogio
import digitalio
import adafruit_ble
from adafruit_ble.advertising.standard import ProvideServicesAdvertisement
from adafruit_ble.uuid import VendorUUID
from adafruit_ble.services import Service
from adafruit_ble.characteristics.int import Uint8Characteristic
from adafruit_ble.characteristics.string import StringCharacteristic
from adafruit_ble.characteristics import Characteristic

#############################################  U S E R  A D J U S T M E N T S #############################
#
# Fraction 0 - 1.0 LED brightness
BRIGHTNESS = 0.7
#
###########################################################################################################

NUM_PIXELS = 10

class SwitchInputService(Service):
    uuid = VendorUUID("c6c7a038-65ca-46d8-853f-d30f6da2ee48")
    buttonStatus = Uint8Characteristic(uuid = VendorUUID("7a91aebe-de90-4135-9a3e-23e3a0b554fd"), 
        properties = Characteristic.READ | Characteristic.NOTIFY,
        )

    def __init__(self, service=None):
        super().__init__(service=service)
        self.connectable = True
        
class ControlService(Service):
    uuid = VendorUUID("69137a96-521e-4f05-b3d2-446d68da158a")
    control = Uint8Characteristic(uuid = VendorUUID("506561bf-2208-4ffb-b0c1-f5e75bec35ad"), 
    properties = Characteristic.WRITE | Characteristic.WRITE_NO_RESPONSE,)
 
    def __init__(self, service=None):
        super().__init__(service=service)
        self.connectable = True
        
class SensorService(Service):
    uuid = VendorUUID("4A98aaaa-1CC4-E7C1-C757-F1267DD021E8")
    sensor = StringCharacteristic(
        uuid=VendorUUID("11f836a0-5da3-4bba-815e-d55d2d1c08bc"),
        properties = Characteristic.READ | Characteristic.NOTIFY,
    )
    def __init__(self, service=None):
        super().__init__(service=service)
        self.connectable = True

# Configure Bluefruit Playground
pixels = neopixel.NeoPixel(board.NEOPIXEL, NUM_PIXELS, brightness=BRIGHTNESS, auto_write=True)

# Get light value and seed random number generator
## Light sensor on A8
lightSensor = analogio.AnalogIn(board.A8)
light = lightSensor.value

# Configure internal red LED
led = digitalio.DigitalInOut(board.D13)
led.switch_to_output()

# Read D5 pushbutton
buttonD5 = digitalio.DigitalInOut(board.D5)
buttonD5.switch_to_input()

# Clear LEDs
pixels.fill((0,0,0))

# Create radio
ble = adafruit_ble.BLERadio()
service = SensorService()
controller = ControlService()
buttonStatus = SwitchInputService()

# Advertising config
myAdvertisement = ProvideServicesAdvertisement(service)
myAdvertisement.connectable = True
ble.name = "Playground"
ble.start_advertising(myAdvertisement)
 
# Show version
print ("PlaygroundDevice v1.0")

# Run sequence
while True: 
    while not ble.connected:
        time.sleep(0.1)
        pass
    print ("Connected")
    while ble.connected:
        service.sensor = str(lightSensor.value) 

        print (controller.control) 
        led.value = True if controller.control == 65 else False

        buttonStatus.buttonStatus = buttonD5.value
        
        time.sleep(0.1)

        
    print ("Disconnected")
    ble.start_advertising(myAdvertisement)
