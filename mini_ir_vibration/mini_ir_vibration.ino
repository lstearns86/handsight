const int irLED = 7; //blue (with 220ohm)
//const int emitter = 4; //(vcc) (don't need)
const int collector = A0; //yellow (with pullup resistor) 
const int vibration = 11; //red
//orange goes to ground
//green goes to ground
//brown goes to ground

const int detectionThreshold = 1010;
const int whiteThreshold = 700;

int lastReading = 1023;
int counter = 0;
int counter2 = 0;

void setup() {
 digitalWrite(collector, HIGH);  // set pullup 
 pinMode(irLED, OUTPUT);
// pinMode(emitter, OUTPUT);
 pinMode(vibration, OUTPUT);
 
 digitalWrite(irLED, HIGH);
// digitalWrite(emitter, HIGH);
 //digitalWrite(vibration, HIGH);
 
 Serial.begin(115200);       // use the serial port 
}

void loop() {
  counter++; counter2++;
  if(counter > 4) { digitalWrite(vibration, LOW); }
  int reading = analogRead(collector);
  if(counter2 >= 5)
  {
    //Serial.print("=");
    Serial.println(reading);
    counter2 = 0;
  }
  
  if(counter > 10 && lastReading < detectionThreshold && reading < detectionThreshold && 
    ((lastReading < whiteThreshold && reading >= whiteThreshold) || (lastReading >= whiteThreshold && reading < whiteThreshold)))
  //if(abs(lastReading - reading) > 200)
  {
    counter = 0;
    digitalWrite(vibration, HIGH);
  }
  
  lastReading = reading;
  delay(10);  // delay to avoid overloading the serial port buffer
}