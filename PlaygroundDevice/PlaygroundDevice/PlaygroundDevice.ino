// Based on Adafruit CircuitPlayground Bluefruit with nRF52840
#include "BLEServer.h"

const int BUTTON_PIN = 4;

void setup() 
{
  // Setup BLE
  setupBLE();

  pinMode(BUTTON_PIN, INPUT);
}

void loop() 
{
    bool bleConnected = updateBLE( digitalRead(BUTTON_PIN) ? "1" : "0");

}
