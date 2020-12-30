# Paradox-Spectra-1738-SerialOutput
Reverse engineering. Paradox Spectra 1738 Serial Output reading from Raspberry PI.

## Paradox Spectra 1738 serial output
Spectra 1738 serial output is 4 bytes. Look at the tables below.

- **Byte 1** is an event.
- **Byte 2** has a different messages like zone number, user number, status message, trouble info.
- **Byte 3, Byte 4** are representing clock.

## Connect Paradox serial output to RaspberryPI
Connect Raspberry PI serial input to Paradox serial output.</br> 
As there is no information how to send commands to Paradox, only two wires needed: ground and data. 
Data will be transmitted from Tx pin (transmit) and this needs to be connected to Raspberry Rx pin (receive).</br>
Paradox Tx output is 5V but Raspberry Rx is only 3,3V.</br>
**DO NOT CONNECT Tx directly to Rx, this will damage your Raspberry!**</br>
Usually recommendation is to use special 5v to 3,3v converter. As I do not have this and the current is extremely small then simple voltage divider with two resistors is also good to go.

![Spectra Layout](Readme/SpectraLayout.png)

## Read serial messages in Raspberry
This is my very first project to deal with a COM-port and serial messages. I start at the beginning of
creating a foreach loop to see is there any COM-ports presented in my Raspberry.
#### Figure out Raspberry COM port
This foreach loop lists all COM-ports presented in Raspberry.
```C
string[] ports = SerialPort.GetPortNames();
Console.WriteLine("The following serial ports are found:");
foreach (string port in ports)
{
    Console.WriteLine(port);
}
// The following serial ports are found:
// /dev/ttyAMA0
```
#### Create and open COM port

```C
string ComPort = "/dev/ttyAMA0";
int baudrate = 9600;
Console.WriteLine($"serial: {ComPort} {baudrate}");
_serialPort = new SerialPort(ComPort, baudrate);
_serialPort.Open();
```
#### Read serial messages 
This loop is reading exactly 4 bytes messages. Data stream will be saved into byte array DataStream.
Now the rest of the work is simple just read these individual bytes and do some smart decisions.
```c
byte[] DataStream = new byte[4];
byte index = 0;
while (true)
{
     //Spectra message output is always 4 bytes
    if (_serialPort.BytesToRead < 4)
    {
        index = 0;
        while (index < 4)
        {
            DataStream[index++] = (byte)_serialPort.ReadByte();
        }
    }
}
```
#### Byte 1
Byte 1 is representing an events and they are categorized. 
* Zones: zone open, closes, alarms in zone.
* Statuses: all kind of messages related arming, disarming etc.
* Users: which user code has been used for arming/disarming.
* Troubles: some trouble messages, havent seen any.

```c
string EventID = DataStream[0].ToString("X2");
string Event = events.Where(x => x.Data == EventID).Select(x => x.Name).DefaultIfEmpty($"NoName {EventID}").First();
int EventCategory = events.Where(x => x.Data == EventID).Select(x => x.Category).DefaultIfEmpty(DataStream[0]).First();

bool isZoneAction = EventCategory == Category.SENSOR;
bool isUserAction = EventCategory == Category.USER;
bool isTrouble = EventCategory == Category.TROUBLE;
bool isStatus = EventCategory == Category.STATUS;
```
#### Byte 2
Byte 2 are messages.</br>
Based on the categories the correct message is displayed.
```c
if (!isStatus)
    Console.Write($" {Event}");

if (isZoneAction)
    Message = sensors.Where(x => x.Data == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();

if (isStatus)
    Message = statuses.Where(x => x.Data == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();

if (isTrouble)
    Message = troubles.Where(x => x.Data == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();

if (isUserAction)
    Message = $"User:{MessageID}";

Console.Write($" {Message}");
```
Following is the output of this program.
</br>
![Serial Output](Readme/SerialOutput.png)
#### Paradox clock from bytes 3 and 4
This was pretty nice reverse engineering task to figure out how the clock is working. 
This is completely useful as it reads just the time reported by Paradox panel (24h).
To solve this clock challenge I built the clock generator in different project.
During that I realized that the clock is based on octal numeric system. Huhh, do you know what it is?
The numbers are going up only to 7 and after that comes 10. 
>1,2,3,4,5,6,7,10,11,12,13,14,15,16,17 ...

The final solution is genius and has just two lines of code with little mathematics. 

```c#
int msb = inData[2];
int lsb = inData[3];

//thats a clock, nice reverse engineering from octal logic
int hour = msb / 8;
int minute = msb % 8 * 16 + lsb / 16;

TimeSpan time = new TimeSpan(hour, minute, 0);
DateTime dateTime = DateTime.Now.Date.Add(time);
Console.Write($"{dateTime:t} ");
```
## Paradox serial output messages explained

<table>
    <tr>
                 <td colspan=2><b>Byte 1</b></ td> 
                 <td colspan=2><b>Byte 2</b></ td> 
                 <td><b>Byte 3</b></ td> 
                 <td><b>Byte 4</b></ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Event</ td>  
                 <td>Hex</ td>    
                 <td>Message</ td>  
                 <td>Hex</ td>    
                 <td>Hex</ td>  
    </tr>
    <tr>
                 <td>0x00</ td>    
                 <td>Zone Closed</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
                 <td rowspan=12>Time</ td>    
                 <td rowspan=12>Time</ td>  
    </tr>
    <tr>
                 <td>0x04</ td>    
                 <td>Zone Open</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
    </tr>
    <tr>
                 <td>0x08</ td>    
                 <td>Status</ td>  
                 <td></ td>    
                 <td>Statuses table</ td>  
    </tr>
    <tr>
                 <td>0x34</ td>    
                 <td>Arming</ td>  
                 <td></ td>    
                 <td>User #</ td>  
    </tr>
    <tr>
                 <td>0x3C</ td>    
                 <td>Disarming</ td>  
                 <td></ td>    
                 <td>User #</ td>  
    </tr>
    <tr>
                 <td>0x40</ td>    
                 <td>Disarming after Aarm</ td>  
                 <td></ td>    
                 <td>User #</ td>  
    </tr>
    <tr>
                 <td>0x14</ td>    
                 <td>Unknown</ td>  
                 <td></ td>    
                 <td></ td>  
    </tr>
    <tr>
                 <td>0x50</ td>    
                 <td>Zone in Alarm</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
    </tr>
    <tr>
                 <td>0x58</ td>    
                 <td>Zone Alarm restore</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
    </tr>
    <tr>
                 <td>0x70</ td>    
                 <td>Trouble fail</ td>  
                 <td></ td>    
                 <td>Troubles table </ td>  
    </tr>
    <tr>
                 <td>0x74</ td>    
                 <td>Trouble fail restore</ td>  
                 <td></ td>    
                 <td>Troubles table</ td>  
    </tr>
</table>

<table>
    <tr>
                 <td colspan=2><b>**STATUSES**</b> Byte 2</ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Status</ td>  
    </tr>
    <tr>
                 <td>0x01</ td>    
                 <td>Zones Open</ td>  
    </tr>
    <tr>
                 <td>0x11</ td>    
                 <td>Zones Closed</ td>  
    </tr>
    <tr>
                 <td>0x21</ td>    
                 <td>Alarm/Bell?</ td>  
    </tr>
    <tr>
                 <td>0x41</ td>    
                 <td>Alarm/Bell?</ td>  
    </tr>
    <tr>
                 <td>0x51</ td>    
                 <td>Alarm occurred during arm</ td>  
    </tr>
    <tr>
                 <td>0x61</ td>    
                 <td>Arm Code entered</ td>  
    </tr>
    <tr>
                 <td>0x71</ td>    
                 <td>Arm Code entered</ td>  
    </tr>
    <tr>
                 <td>0x91</ td>    
                 <td>Disarmed</ td>  
    </tr>
    <tr>
                 <td>0xA1</ td>    
                 <td>Armed</ td>  
    </tr>
    <tr>
                 <td>0xB1</ td>    
                 <td>Entry delay started</ td>  
    </tr>
</table>

<table>
    <tr>
                 <td colspan=2><b>**TROUBLES**</b> Byte 2</ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Trouble</ td>  
    </tr>
    <tr>
                 <td>0x21</ td>    
                 <td>Battery</ td>  
    </tr>
    <tr>
                 <td>0x51</ td>    
                 <td>Bell</ td>  
    </tr>
</table>

<table>
    <tr>
                 <td colspan=2><b>**Zones**</b> Byte 2</ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Zone</ td>  
    </tr>
    <tr>
                 <td>0x11</ td>    
                 <td>Zone 1</ td>  
    </tr>
    <tr>
                 <td>0x21</ td>    
                 <td>Zone 2</ td>  
    </tr>
    <tr>
                 <td>0x31</ td>    
                 <td>Zone 3</ td>  
    </tr>
    <tr>
                 <td>0x41</ td>    
                 <td>Zone 4</ td>  
    </tr>
    <tr>
                 <td>0x51</ td>    
                 <td>Zone 5</ td>  
    </tr>
    <tr>
                 <td>0x61</ td>    
                 <td>Zone 6</ td>  
    </tr>
    <tr>
                 <td>0x71</ td>    
                 <td>Zone 7</ td>  
    </tr>
    <tr>
                 <td>0x81</ td>    
                 <td>Zone 8</ td>  
    </tr>
</table>