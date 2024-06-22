#include "BLEServer.h"

//#define CHARACTERISTIC_UUID        "beb5483e-36e1-4688-b7f5-ea07361b26a8"

const char* SPRAYER_SERVICE_UUID = "4A98aaaa-1CC4-E7C1-C757-F1267DD021E8"; //4A98aaaa-1CC4-E7C1-C757-F1267DD021E8
const char* SERVER_NAME = "Mosquito";

BLECharacteristic *pCharacteristic = 0;
BLEServer *pServer;
bool deviceConnected = false;
bool oldDeviceConnected = false;

class MyServerCallbacks: public BLEServerCallbacks 
{
    void onConnect(BLEServer* pServer) 
    {
       Serial.println("Connected");
       deviceConnected = true;
    };

    void onDisconnect(BLEServer* pServer) 
    {
       Serial.println("Disconnected");
       deviceConnected = false;
    }
};

void setupBLE() 
{
  Serial.println("Configuring BLE server");

  BLEDevice::init(SERVER_NAME);
  pServer = BLEDevice::createServer();

  pServer->setCallbacks(new MyServerCallbacks());
  
  BLEService *pService = pServer->createService(SPRAYER_SERVICE_UUID);
  pCharacteristic = pService->createCharacteristic( SPRAYER_SERVICE_UUID, BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY );
  // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
  // Create a BLE Descriptor
  pCharacteristic->addDescriptor(new BLEDescriptor(BLEUUID((uint16_t) 0x2901)));
  
  pCharacteristic->setValue("-");
  pService->start();

  Serial.println("BLE Server starting");
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SPRAYER_SERVICE_UUID);
  pAdvertising->setScanResponse(true);
  pAdvertising->setMinPreferred(0x06);  // functions that help with iPhone connections issue
  pAdvertising->setMinPreferred(0x12);
  BLEDevice::startAdvertising();
  Serial.println("BLE Server advertising");
}

bool updateBLE(const char* status) 
{
    if (deviceConnected)
    {
      pCharacteristic->setValue(status);
      pCharacteristic->notify();
    }
    // disconnecting
    if (!deviceConnected && oldDeviceConnected) 
    {
        // give the bluetooth stack the chance to get things ready
        delay(500); 
        // restart advertising
        pServer->startAdvertising(); 
        Serial.println("Restart advertising");
        oldDeviceConnected = deviceConnected;
    }
    // connecting
    if (deviceConnected && !oldDeviceConnected) 
    {
        // do stuff here on connecting
        oldDeviceConnected = deviceConnected;
    }  
    return deviceConnected;
}
