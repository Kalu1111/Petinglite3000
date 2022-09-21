#include <NeoSWSerial.h>
#include <FastLED.h>

#define seconds()            (millis()/1000)
#define BAUD                  115200 //38400
#define PACKET_FILLER         250
#define CMD_SELECT_RGB        251
#define CMD_SELECT_BRIGHTNESS 252
#define CMD_SELECT_DRAW       253
#define CMD_SELECT_LEDCOUNT   254
#define CMD_END_OF_MSG        255
#define SERIAL_MSG_MAX_SIZE   9
#define LED_COUNT_MAX_SIZE    64
#define OUTPUT_PIN_RED        5


//___________________________________________________________________________________________________
//
//  COMM PROTOCOL INFO:
//
//
//___________________________________________________________________________________________________
//  PC sends (ledAmount/6) packets each containing 3 BGR values to draw 6 LEDs
//
//        1 byte          1 byte      1 byte            9 bytes                  1 byte
//  |_CMD_SELECT_RGB_|_msg_checksum_|_msg_Id_|_RGB_info_each_byte_is_a_pixel_|_CMD_END_OF_MSG_|
//
//___________________________________________________________________________________________________
//  This command is used to tell the arduino to tell fastLED to draw in the new values
//
//        1 byte            1 byte
//  |_CMD_SELECT_DRAW_|_CMD_END_OF_MSG_|
//
//___________________________________________________________________________________________________
//  This is so the arduino can be made aware of how many LEDs are going to be used
//
//          1 byte             1 byte       1 byte         1 byte
//  |_CMD_SELECT_LEDCOUNT_|_msg_checksum_|_ledAmount_|_CMD_END_OF_MSG_|
//
//___________________________________________________________________________________________________
//  Sets the led strip brightness
//
//          1 byte             1 byte          1 byte         1 byte
//  |_CMD_SELECT_BRIGHTNESS_|_msg_checksum_|_brightness_|_CMD_END_OF_MSG_|
//
//___________________________________________________________________________________________________


void(* resetFunc) (void) = 0;

CRGB  leds[LED_COUNT_MAX_SIZE];
//NeoSWSerial BTSerial (3, 2);   //RX-red   TX-white

bool freshReboot = true;
uint16_t secSinceLastDraw = 0;
byte  packet[SERIAL_MSG_MAX_SIZE];
byte  headerInfo[2];  //0-checksum 1-packetId
short packetIdx = -1;
byte  brightness = 100;
byte  ledAmount = 56; //0-RED
byte  checksumCheck = 0;
byte  targetColor = 0;
byte  fadeOutCounter = 0;

unsigned long before = 0;

void setup() {
  //BTSerial.begin(BAUD);
  //BTSerial.setTimeout(1000);
  Serial.begin(BAUD);
  Serial.setTimeout(1000);
  pinMode(3, INPUT_PULLUP);
  pinMode(2, OUTPUT);
  pinMode(5, OUTPUT);
  FastLED.addLeds<WS2813, OUTPUT_PIN_RED, BGR>(leds, ledAmount);
}

void  ReadSerialPacket(byte b) {
  if(freshReboot) freshReboot=false;
  
  if (b == CMD_SELECT_RGB) {
    targetColor = CMD_SELECT_RGB;
    packetIdx = 0;
  } else if (b == CMD_END_OF_MSG) {
    packetIdx = -1;
    if (checksumCheck > 250)
      checksumCheck += 10;
    if (headerInfo[0] == checksumCheck) {

      if (targetColor == CMD_SELECT_RGB) {
        for (byte i = 0, j = 0; i < 3; i++) {
          if (packet[(i * 3)] < PACKET_FILLER) {
            //Every RGB pixel received draws 2 leds to reduce the amount o info that needs to be sent via Bluetooth
            leds[(headerInfo[1] * 6) + i + j] = CRGB(packet[(i * 3)], packet[(i * 3) + 1], packet[(i * 3) + 2]);
            j++;
            leds[(headerInfo[1] * 6) + i + j] = CRGB(packet[(i * 3)], packet[(i * 3) + 1], packet[(i * 3) + 2]);
          }
          else {
            j++;
          }
        }
      }

      else if (targetColor == CMD_SELECT_DRAW) {
        FastLED.show(); //~1.5ms to draw 60px
        secSinceLastDraw = seconds();
        fadeOutCounter = 0;
      }

      else if (targetColor == CMD_SELECT_LEDCOUNT) {
        if(ledAmount != headerInfo[1]){
          ledAmount = headerInfo[1];
          FastLED.addLeds<WS2813, OUTPUT_PIN_RED, BGR>(leds, ledAmount);
          FastLED.setDither(true); 
        }
      }
      
      else if (targetColor == CMD_SELECT_BRIGHTNESS) {
        if(brightness != headerInfo[1]){
          brightness = headerInfo[1];
          FastLED.setBrightness(brightness);
        }
      }
    }
  }  else if (b < CMD_SELECT_RGB)
  {
    switch (packetIdx) {
      case 0:
        headerInfo[0] = b; //checksum
        break;
      case 1:
        headerInfo[1] = b; //packet id
        checksumCheck = b;
        break;
      default:
        packet[packetIdx - 2] = b;
        checksumCheck += b;
        break;
    }

    if (packetIdx < SERIAL_MSG_MAX_SIZE + 1)
      packetIdx++;
  }
  else if (b == CMD_SELECT_DRAW) {
    targetColor = CMD_SELECT_DRAW;
    headerInfo[0] = 0;
    checksumCheck = 0;
    packetIdx = 0;
  }
  else if (b == CMD_SELECT_LEDCOUNT) {
    targetColor = CMD_SELECT_LEDCOUNT;
    packetIdx = 0;
  } 
  else if (b == CMD_SELECT_BRIGHTNESS) {
    targetColor = CMD_SELECT_BRIGHTNESS;
    packetIdx = 0;
  }
}

void loop() {
  while (Serial.available()) {
    ReadSerialPacket(Serial.read());
  }

  if(!freshReboot)
    if (seconds() - secSinceLastDraw > 2){
       FadeOut();
       if(fadeOutCounter>250)
          resetFunc();
    }
}


void FadeOut() {
  fadeToBlackBy(leds, ledAmount, 1);
  FastLED.show();
  fadeOutCounter++;
}
