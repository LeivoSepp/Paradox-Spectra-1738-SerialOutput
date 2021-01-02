using System;
using System.IO.Ports;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace ParadoxSpectra1738SerialOutput
{
    //Raspberry PI Com Port: /dev/ttyAMA0
    //Paradox Spectra 1738 Serial Port
    class Program
    {
        static SerialPort _serialPort;
        static void Main(string[] args)
        {
            // Get a list of serial port names
            string[] ports = SerialPort.GetPortNames();
            Console.WriteLine("The following serial ports are found:");
            foreach (string port in ports)
            {
                Console.WriteLine(port);
            }

            string ComPort = "/dev/ttyAMA0";
            int baudrate = 9600;
            Console.WriteLine($"serial: {ComPort} {baudrate}");
            _serialPort = new SerialPort(ComPort, baudrate);
            try
            {
                _serialPort.Open();
                ReadMessages();
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{ex}");
            }
        }
        public static List<Event> events = new List<Event>
        {
            new Event(){Data = "00", EventCategory = Category.ZONE, EventName = "Zone Closed"},
            new Event(){Data = "04", EventCategory = Category.ZONE, EventName = "Zone Open"},
            new Event(){Data = "08", EventCategory = Category.STATUS, EventName = "Status"},
            new Event(){Data = "34", EventCategory = Category.USER, EventName = "Arming"},
            new Event(){Data = "3C", EventCategory = Category.USER, EventName = "Disarming"},
            new Event(){Data = "40", EventCategory = Category.USER, EventName = "Disarming after Alarm"},
            new Event(){Data = "50", EventCategory = Category.ZONE, EventName = "Zone in Alarm"},
            new Event(){Data = "58", EventCategory = Category.ZONE, EventName = "Zone Alarm restore"},
            new Event(){Data = "70", EventCategory = Category.TROUBLE, EventName = "Trouble fail"},
            new Event(){Data = "74", EventCategory = Category.TROUBLE, EventName = "Trouble back to normal"}
        };
        public static List<Status> statuses = new List<Status>
        {
            new Status(){Data = "01", StatusMessage = "Zones open"},
            new Status(){Data = "11", StatusMessage = "Zones closed"},
            new Status(){Data = "21", StatusMessage = "Alarm21/Bell"},
            new Status(){Data = "41", StatusMessage = "Alarm41/Bell"},
            new Status(){Data = "51", StatusMessage = "Alarm occurred during arm"},
            new Status(){Data = "61", StatusMessage = "ArmCode61"},
            new Status(){Data = "71", StatusMessage = "ArmCode71"},
            new Status(){Data = "91", StatusMessage = "Disarmed"},
            new Status(){Data = "A1", StatusMessage = "Armed"},
            new Status(){Data = "B1", StatusMessage = "Entry delay started"},
        };
        public static List<Trouble> troubles = new List<Trouble>
        {
            new Trouble(){Data = "21", TroubleName = "Battery"},
            new Trouble(){Data = "51", TroubleName = "Bell"}
        };
        public static List<Zone> zones = new List<Zone> {
            new Zone(){Data = "11", ZoneName = "DOOR", IsZoneOpen=false},
            new Zone(){Data = "21", ZoneName = "ENTRY/PIANO", IsZoneOpen=false},
            new Zone(){Data = "31", ZoneName = "LIVING ROOM", IsZoneOpen=false},
            new Zone(){Data = "41", ZoneName = "OFFICE", IsZoneOpen=false},
            new Zone(){Data = "51", ZoneName = "HALL", IsZoneOpen=false},
            new Zone(){Data = "61", ZoneName = "BEDROOM", IsZoneOpen=false},
            new Zone(){Data = "71", ZoneName = "FIRE", IsZoneOpen=false},
            new Zone(){Data = "81", ZoneName = "TECHNO", IsZoneOpen=false}
            };
        public static void ReadMessages()
        {
            byte[] DataStream = new byte[4];
            byte index = 0;
            while (true)
            {
                try
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
                    int msb = DataStream[2];
                    int lsb = DataStream[3];

                    //thats a clock, nice reverse engineering from octal logic
                    int hour = msb / 8;
                    int minute = msb % 8 * 16 + lsb / 16;

                    TimeSpan time = new TimeSpan(hour, minute, 0);
                    DateTime dateTime = DateTime.Now.Date.Add(time);
                    Console.Write($"{dateTime:t} ");

                    for (int i = 0; i < DataStream.Length; i++)
                    {
                        Console.Write($"{DataStream[i]:X2} ");
                    }

                    string EventID = DataStream[0].ToString("X2");
                    string Event = events.Where(x => x.Data == EventID).Select(x => x.EventName).DefaultIfEmpty($"NoName {EventID}").First();
                    int EventCategory = events.Where(x => x.Data == EventID).Select(x => x.EventCategory).DefaultIfEmpty(DataStream[0]).First();

                    string MessageID = DataStream[1].ToString("X2");
                    string Message = MessageID;

                    bool isZoneAction = EventCategory == Category.ZONE;
                    bool isUserAction = EventCategory == Category.USER;
                    bool isTrouble = EventCategory == Category.TROUBLE;
                    bool isStatus = EventCategory == Category.STATUS;

                    if (!isStatus)
                        Console.Write($" {Event}");

                    if (isZoneAction)
                    {
                        //save the IRState into zone's list
                        bool IsZoneOpen = false;
                        if (EventID == "04") IsZoneOpen = true;
                        zones.Where(x => x.Data == MessageID).Select(x => { x.IsZoneOpen = IsZoneOpen; return x; }).ToList();
                        Message = zones.Where(x => x.Data == MessageID).Select(x => $"{x.ZoneName} {(x.IsZoneOpen ? "Open" : "Closed")}").DefaultIfEmpty($"NoName {MessageID}").First();
                    }
                    if (isStatus)
                        Message = statuses.Where(x => x.Data == MessageID).Select(x => x.StatusMessage).DefaultIfEmpty($"NoName {MessageID}").First();

                    if (isTrouble)
                        Message = troubles.Where(x => x.Data == MessageID).Select(x => x.TroubleName).DefaultIfEmpty($"NoName {MessageID}").First();

                    if (isUserAction)
                        Message = $"User:{MessageID}";

                    Console.Write($" {Message}");
                    //Console.Write($" msg:{Convert.ToString(inData[1], 2)}");

                    Console.WriteLine();
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"{ex}");
                }
            }
        }
    }
    class Event
    {
        public string Data { get; set; }
        public string EventName { get; set; }
        public int EventCategory { get; set; }
    }
    class Category
    {
        public const int ZONE = 1;
        public const int STATUS = 2;
        public const int TROUBLE = 3;
        public const int USER = 4;
    }
    class Zone
    {
        public string Data { get; set; }
        public string ZoneName { get; set; }
        public bool IsZoneOpen { get; set; }
    }
    class Trouble
    {
        public string Data { get; set; }
        public string TroubleName { get; set; }
    }
    class Status
    {
        public string Data { get; set; }
        public string StatusMessage { get; set; }
    }
}
