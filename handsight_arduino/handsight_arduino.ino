#include <EEPROM.h>

// pin layout for each finger:
// blue: transistor or vcc (with resistor)
// green: ground
// yellow: collector
// orange: ground
// red: vibration motor
// brown: ground

// pin numbers
const int irLED = 7; // wire connected to transistor/220 ohm resistor (all on same pin)
const int collectors[] = {A0,A1,A2,A3}; // (with pullup resistors)
const int vibrations[] = {3,5,6,9};
const int modeLED = 13; // flash the LED on this pin to indicate mode
boolean modeLEDon = false;

// thresholds for each finger
const int detectionThreshold[] = {940,940,940,940};
const int whiteThreshold[] = {600,600,600,600};

// timers (in milliseconds)
int lastVibration[] = {0,0,0,0};

// modes
const int MODE_EDGES = 0;
const int MODE_BLACK = 1;
const int MODE_GRAYSCALE = 2;
const int MODE_NAVIGATION = 3;
const int MODE_TYPING = 4;
const int MODE_MASSAGE = 5;
const int numModes = 6;
int mode = MODE_EDGES;
int modeLightStart = 0;

// miscellaneous
const int numFingers = 4;
const int numUltrasonic = 2;
int readings[] = {1023, 1023, 1023, 1023, 1023, 1023};
int lastReadings[] = {1023, 1023, 1023, 1023, 1023, 1023};
const int serialRate = 100; // in milliseconds
int lastSerialWrite = 0;

// edge detection
boolean vibrating[] = {false, false, false, false};
const int vibrationDuration = 80;
const int vibrationDelay = 50; // time between vibrations

// typing
// mapping from finger bits to characters
const char mapping[]="# aroslyepxmhgujtndcwbq?ifkz.v^<";
int backspace_initial_delay=200;
int backspace_repeat_delay=50;
int backspace_time=0;
int backspace_repeat=0;
int been_on=0;
int off_count=0;
char textBuffer[16]; // for holding the input text between serial writes

void setup() 
{
  mode = EEPROM.read(0); // retrieve the most recent mode from non-volatile memory
 
  if(mode >= numModes)
  {
    mode = 0;
    EEPROM.write(0, 0);
  } 
  
  for(int i=0; i<numFingers; i++) 
  {
    digitalWrite(collectors[i], HIGH);  // set pullup 
    pinMode(vibrations[i], OUTPUT);
   }
   
   pinMode(irLED, OUTPUT);
   digitalWrite(irLED, HIGH);
   digitalWrite(modeLED, LOW);
   
   for(int i = 0; i<16; i++) textBuffer[i] = 0;
 
   Serial.begin(115200); // use the serial port, this baud rate required for bluetooth (default bluetooth anyways)
}

void serialRead()
{
  // read all commands
  while(Serial.available() > 0)
  {
    byte command = Serial.read();
    switch(command)
    {
      case MODE_EDGES: mode = MODE_EDGES; break;
      case MODE_BLACK: mode = MODE_BLACK; break;
      case MODE_GRAYSCALE: mode = MODE_GRAYSCALE; break;
      case MODE_NAVIGATION: mode = MODE_NAVIGATION; break;
      case MODE_TYPING: mode = MODE_TYPING; break;
      case MODE_MASSAGE: mode = MODE_MASSAGE; break;
      // TODO add commands for programming bluetooth
      default: break; // do nothing if we don't recognize the command
    }
    
    // turn off the vibration motors for now
    for(int i = 0; i < numFingers; i++)
    {
      digitalWrite(vibrations[i], LOW);
      vibrating[i] = false;
    }
    
    // save the current mode when we power off
    EEPROM.write(0, mode);
  }
}

void serialWrite()
{
  int now = millis();
  if(now - lastSerialWrite > serialRate)
  {
    Serial.print(mode);
    Serial.print(":");
    for(int i = 0; i < numFingers + numUltrasonic; i++)
    {
      Serial.print(readings[i]);
      Serial.print(",");
    }
    Serial.print("|");
    for(int i = 0; i < 16; i++) // consume the waiting text buffer
    {
      if(textBuffer[i] == 0) break;
      Serial.print(textBuffer[i]);
      textBuffer[i] = 0;
    }
    Serial.println("");
    lastSerialWrite = now;
  }
}

// flash the built in LED a certain number of times to indicate mode
void flashMode()
{
  int now = millis();
  int onDuration = 1000 / (numModes * 2);
  int index = ((now - modeLightStart) % 1000) / onDuration;
  boolean on = index % 2 == 0 && index / 2 <= mode;
  if(on && !modeLEDon) { digitalWrite(modeLED, HIGH); modeLEDon = true; }
  else if(!on && modeLEDon) { digitalWrite(modeLED, LOW); modeLEDon = false; }
  if(now - modeLightStart > 1000) modeLightStart = now;
}

void edgeLoop()
{
  int now = millis();

  // check whether it's time to turn the vibration off
  for(int i = 0; i < numFingers; i++)
    if(vibrating[i] && now - lastVibration[i] > vibrationDuration)
    {
      digitalWrite(vibrations[i], LOW);
      vibrating[i] = false;
    }
      
  // get the current readings
  for(int i = 0; i < numFingers; i++)
    readings[i] = analogRead(collectors[i]);
    
  // check whether we should turn on the vibration
  for(int i = 0; i < numFingers; i++)
    if(!vibrating[i] && now - lastVibration[i] > vibrationDelay && 
       ((lastReadings[i] < whiteThreshold[i] && readings[i] >= whiteThreshold[i]) ||
        (lastReadings[i] >= whiteThreshold[i] && readings[i] < whiteThreshold[i])))
    {
      digitalWrite(vibrations[i], HIGH);
      lastVibration[i] = millis();
      vibrating[i] = true;
    }
    
  // update the previous readings with the current
  for(int i = 0; i < numFingers; i++)
    lastReadings[i] = readings[i];
}

void blackLoop()
{
  int now = millis();

  // get the current readings
  for(int i = 0; i < numFingers; i++)
    readings[i] = analogRead(collectors[i]);
    
  // vibrate on black, with minimum vibration time
  for(int i = 0; i < numFingers; i++)
  {
    if(readings[i] < detectionThreshold[i] && readings[i] >= whiteThreshold[i])
    {
      digitalWrite(vibrations[i], HIGH);
      vibrating[i] = true;
      lastVibration[i] = now;
    }
    else if(now - lastVibration[i] > vibrationDuration)
    {
      digitalWrite(vibrations[i], LOW);
      vibrating[i] = false;
    }
  }
}

void grayscaleLoop()
{
  int now = millis();

  // get the current readings
  for(int i = 0; i < numFingers; i++)
    readings[i] = analogRead(collectors[i]);
    
  // vibrate strongest on black, lowest on white
  for(int i = 0; i < numFingers; i++)
  {
    if(readings[i] < detectionThreshold[i])
    {
      analogWrite(vibrations[i], (int)(255.0 * readings[i] / detectionThreshold[i]));
      vibrating[i] = true;
    }
    else
    {
      analogWrite(vibrations[i], 0);
      vibrating[i] = false;
    }
  }
}

void massageLoop()
{
  // get the current readings
  for(int i = 0; i < numFingers; i++)
    readings[i] = analogRead(collectors[i]);
  
  for(int i = 0; i < numFingers; i++)
  {
    if(readings[i] < detectionThreshold[i] && !vibrating[i])
    {
      digitalWrite(vibrations[i], HIGH);
      vibrating[i] = true;
    }
    else if(readings[i] >= detectionThreshold[i] && vibrating[i])
    {
      digitalWrite(vibrations[i], LOW);
      vibrating[i] = false;
    }
  }
}

void loop() {
  serialRead();
  flashMode();
  switch(mode)
  {
    case MODE_EDGES: edgeLoop(); break;
    case MODE_BLACK: blackLoop(); break;
    case MODE_MASSAGE: massageLoop(); break;
    default: break;
  }
  serialWrite();
}