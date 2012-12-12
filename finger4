const int irLED = 7; //blue (with 220ohm) (all 4 on same pin)
//const int emitter = 4; //(vcc) (don't need)
const int collectors[] = {A0,A1,A2,A3}; //yellow (with pullup resistor) 
const int vibrations[] = {3,5,6,9}; //red
//orange goes to ground
//green goes to ground
//brown goes to ground

const int detectionThreshold = 940;
const int whiteThreshold = 600;

int lastReadings[] = {1023, 1023, 1023, 1023};
int counters[] = {0, 0, 0, 0};
int black_mode_counters[] = {0, 0, 0, 0};
int counter2 = 0;

const int numFingers = 4;

//mapping from finger bits to characters
const char mapping[]="# aroslyepxmhgujtndcwbq?ifkz.v^<";
int backspace_initial_delay=200;
int backspace_repeat_delay=50;
int backspace_time=0;
int backspace_repeat=0;
int been_on=0;
int off_count=0;

//variables for distance sensors
/*
 * Define the pins you want to use as trigger and echo.
 */

#define ECHOPIN1 2              // Pin to receive echo pulse
#define TRIGPIN1 4              // Pin to send trigger pulse
#define ECHOPIN2 10             // Pin to receive echo pulse
#define TRIGPIN2 11             // Pin to send trigger pulse

int triggerPullDown = 2;        // in microseconds
int pingWidth = 10;             // in microseconds
int maxDist = 200;               // in cm
int timeout = maxDist * 2 * 29; // in microseconds
// timeout derivation from user JamesHappy on arduino.cc forum: http://arduino.cc/forum/index.php?topic=55119.0



void setup() {
  for(int i=0; i<numFingers; i++) {
    digitalWrite(collectors[i], HIGH);  // set pullup 
    pinMode(vibrations[i], OUTPUT);
   }
 
   pinMode(irLED, OUTPUT);
   digitalWrite(irLED, HIGH);
 
   Serial.begin(115200);       // use the serial port, this baud rate required for bluetooth (default bluetooth anyways)
   
   //setup distance sensors
   pinMode(ECHOPIN1, INPUT);
   pinMode(TRIGPIN1, OUTPUT);
   pinMode(ECHOPIN2, INPUT);
   pinMode(TRIGPIN2, OUTPUT);
}


void distance_loop() {
  // Send a ping on 1
  digitalWrite(TRIGPIN1, LOW);
  delayMicroseconds(triggerPullDown);
  digitalWrite(TRIGPIN1, HIGH);
  delayMicroseconds(pingWidth);
  digitalWrite(TRIGPIN1, LOW);
  
  // Listen for echo and compute distance
  float distance1 = pulseIn(ECHOPIN1, HIGH, timeout);
  distance1= distance1/58; // convert to centimeters
  if(distance1 <= 0) distance1 = maxDist;
  
  // Send a ping on 2
  digitalWrite(TRIGPIN2, LOW);
  delayMicroseconds(triggerPullDown);
  digitalWrite(TRIGPIN2, HIGH);
  delayMicroseconds(pingWidth);
  digitalWrite(TRIGPIN2, LOW);
  
  // Listen for echo and compute distance
  float distance2 = pulseIn(ECHOPIN2, HIGH, timeout);
  distance2= distance2/58; // convert to centimeters
  if(distance2 <= 0) distance2 = maxDist;
  
  //vibrate (1.5 meters = no vibration, 10cm = full intensity)
  int vibration1=(distance1-10)/150*256;
  vibration1=vibration1>255 ? 255 : vibration1;
  vibration1=vibration1<0 ? 0 : vibration1;
  int vibration2=(distance2-10)/150*256;
  vibration2=vibration2>255 ? 255 : vibration2;
  vibration2=vibration2<0 ? 0 : vibration2;
  analogWrite(vibrations[0], 255-vibration1);
  analogWrite(vibrations[3], 255-vibration2);
  
  // Write distance readings to serial
  Serial.print("=");
  Serial.print(distance1);
  Serial.print(",");
  Serial.print(distance2);
  Serial.println("");
  
  // Wait for residual echos to dissipate
  delay(50);
}

void loop() {
  //for now, just call distance_loop
  distance_loop();
  return;
  
  counter2++;
  for(int i=0; i<numFingers; i++) {
    counters[i]++;
    if(counters[i] > 40) {
      digitalWrite(vibrations[i], LOW);
    }
  }
  int readings[numFingers];
  for(int i=0; i<numFingers; i++) {
   readings[i] = analogRead(collectors[i]);
  }
  
  if(counter2 >= 50) {
    for(int i=0; i<numFingers-1; i++) {
      Serial.print(readings[i]);
      Serial.print(",");
    }
    Serial.println(readings[numFingers-1]);
    counter2 = 0;
  }
  
  //theshold crossing mode
 /* for(int i=0; i<numFingers; i++) {
    if(counters[i] > 10 && lastReadings[i] < detectionThreshold && readings[i] < detectionThreshold && 
    ((lastReadings[i] < whiteThreshold && readings[i] >= whiteThreshold) || (lastReadings[i] >= whiteThreshold && readings[i] < whiteThreshold))) {
      digitalWrite(vibrations[i], HIGH);
      counters[i] = 0;
    }
    lastReadings[i] = readings[i];
   }*/
   
   
   //vibrate on black mode
   /*for(int i=0; i<numFingers; i++) {
     digitalWrite(vibrations[i], readings[i]<detectionThreshold && readings[i]>whiteThreshold);
   }*/
   
   //vibrate on black mode (with minimum vibration time)
   for(int i=0; i<numFingers; i++) {
     if(readings[i]<detectionThreshold && readings[i]>whiteThreshold) {
       black_mode_counters[i]=90;
     }
     if(black_mode_counters[i]>0) {
       digitalWrite(vibrations[i], HIGH);
       black_mode_counters[i]--;
     }
     else {
       digitalWrite(vibrations[i], LOW);
     }
   }
   
   //massage mode
 /*  for(int i=0; i<numFingers; i++) {
     digitalWrite(vibrations[i], readings[i]<detectionThreshold);
   }*/
   
   //typing mode
 /*  int currently_on=0;
   int keys_down=0;
   for(int i=0; i<numFingers; i++) {
     int button_down = readings[i]<detectionThreshold;
     digitalWrite(vibrations[i], button_down);
     currently_on=currently_on<<1 | button_down;
     keys_down += button_down;
   }
   if(been_on && !currently_on) {
     char outkey=mapping[been_on];
     Serial.write(outkey);
     been_on = 0;
   }
   been_on|=currently_on;*/
   
   //analog mode
//   for(int i=0; i<numFingers; i++) {
//     if(readings[i]<detectionThreshold && readings[i]>70) {
//       analogWrite(vibrations[i], readings[i]/4);
//     } else {
//       digitalWrite(vibrations[i], 0);
//     }
//   }
  
  delay(1);  // delay to avoid overloading the serial port buffer
}
