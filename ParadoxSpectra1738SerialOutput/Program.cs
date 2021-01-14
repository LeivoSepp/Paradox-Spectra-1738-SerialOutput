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
            new Event(){Byte1 = "00", EventCategory = Category.ZONE, EventName = "Zone Closed"},
            new Event(){Byte1 = "04", EventCategory = Category.ZONE, EventName = "Zone Open"},
            new Event(){Byte1 = "08", EventCategory = Category.STATUS, EventName = "Status"},
            new Event(){Byte1 = "34", EventCategory = Category.ACCESS_CODES, EventName = "Arming"},
            new Event(){Byte1 = "3C", EventCategory = Category.ACCESS_CODES, EventName = "Disarming"},
            new Event(){Byte1 = "40", EventCategory = Category.ACCESS_CODES, EventName = "Disarming after Alarm"},
            new Event(){Byte1 = "44", EventCategory = Category.SPECIAL_ALARM, EventName = "Unknown_44"},
            new Event(){Byte1 = "50", EventCategory = Category.ZONE, EventName = "Zone in Alarm"},
            new Event(){Byte1 = "54", EventCategory = Category.ZONE, EventName = "24h Zone in Alarm"},
            new Event(){Byte1 = "58", EventCategory = Category.ZONE, EventName = "Zone Alarm restore"},
            new Event(){Byte1 = "5C", EventCategory = Category.ZONE, EventName = "24h Zone Alarm rstore"},
            new Event(){Byte1 = "70", EventCategory = Category.TROUBLE, EventName = "Trouble fail"},
            new Event(){Byte1 = "74", EventCategory = Category.TROUBLE, EventName = "Trouble back to normal"},
            new Event(){Byte1 = "78", EventCategory = Category.INSTALLER, EventName = "Installer mode"}
        };
        public static List<Byte2Data> statuses = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = "01", Name = "Zones open"},
            new Byte2Data(){Byte2 = "11", Name = "Zones closed"},
            new Byte2Data(){Byte2 = "21", Name = "Alarm21/Bell"},
            new Byte2Data(){Byte2 = "31", Name = "Silent alarm"},
            new Byte2Data(){Byte2 = "41", Name = "Alarm41/Bell"},
            new Byte2Data(){Byte2 = "51", Name = "Alarm occurred during arm"},
            new Byte2Data(){Byte2 = "61", Name = "ArmCode61"},
            new Byte2Data(){Byte2 = "71", Name = "ArmCode71"},
            new Byte2Data(){Byte2 = "91", Name = "Disarmed"},
            new Byte2Data(){Byte2 = "A1", Name = "Armed"},
            new Byte2Data(){Byte2 = "B1", Name = "Entry delay started"},
        };
        public static List<Byte2Data> troubles = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = "21", Name = "Battery"},
            new Byte2Data(){Byte2 = "51", Name = "Bell"}
        };
        public static List<Byte2Data> installers = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = "41", Name = "Enter installer mode"},
            new Byte2Data(){Byte2 = "51", Name = "Exit installer mode"}
        };
        public static List<Zone> zones = new List<Zone> {
            new Zone(){Byte2 = "11", ZoneName = "DOOR", IsZoneOpen=false},
            new Zone(){Byte2 = "21", ZoneName = "ENTRY", IsZoneOpen=false},
            new Zone(){Byte2 = "31", ZoneName = "LIVING ROOM", IsZoneOpen=false},
            new Zone(){Byte2 = "41", ZoneName = "OFFICE", IsZoneOpen=false},
            new Zone(){Byte2 = "51", ZoneName = "HALL", IsZoneOpen=false},
            new Zone(){Byte2 = "61", ZoneName = "BEDROOM", IsZoneOpen=false},
            new Zone(){Byte2 = "71", ZoneName = "FIRE", IsZoneOpen=false},
            new Zone(){Byte2 = "81", ZoneName = "TECHNO", IsZoneOpen=false},
            new Zone(){Byte2 = "91", ZoneName = "PIANO", IsZoneOpen=false}
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
                    string Event = events.Where(x => x.Byte1 == EventID).Select(x => x.EventName).DefaultIfEmpty($"NoName {EventID}").First();
                    int EventCategory = events.Where(x => x.Byte1 == EventID).Select(x => x.EventCategory).DefaultIfEmpty(DataStream[0]).First();

                    string MessageID = DataStream[1].ToString("X2");
                    string Message = MessageID;

                    bool isZoneAction = EventCategory == Category.ZONE;
                    bool isUserAction = EventCategory == Category.ACCESS_CODES;
                    bool isTrouble = EventCategory == Category.TROUBLE;
                    bool isStatus = EventCategory == Category.STATUS;
                    bool isInstaller = EventCategory == Category.INSTALLER;

                    if (!isStatus)
                        Console.Write($" {Event}");

                    if (isZoneAction)
                    {
                        //save the IRState into zone's list
                        bool IsZoneOpen = false;
                        if (EventID == "04") IsZoneOpen = true;
                        zones.Where(x => x.Byte2 == MessageID).Select(x => { x.IsZoneOpen = IsZoneOpen; return x; }).ToList();
                        Message = zones.Where(x => x.Byte2 == MessageID).Select(x => $"{x.ZoneName} {(x.IsZoneOpen ? "Open" : "Closed")}").DefaultIfEmpty($"NoName {MessageID}").First();
                    }
                    if (isStatus)
                        Message = statuses.Where(x => x.Byte2 == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();

                    if (isTrouble)
                        Message = troubles.Where(x => x.Byte2 == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();

                    if (isInstaller)
                        Message = installers.Where(x => x.Byte2 == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();

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
        public string Byte1 { get; set; }
        public string EventName { get; set; }
        public int EventCategory { get; set; }
    }
    class Category
    {
        public const int ZONE = 1;
        public const int STATUS = 2;
        public const int TROUBLE = 3;
        public const int ACCESS_CODES = 4;
        public const int INSTALLER = 5;
        public const int SPECIAL_ALARM = 6;
        public const int SPECIAL_ARM = 7;
        public const int SPECIAL_DISARM = 8;
        public const int NON_REPORT_EVENTS = 9;
        public const int SPECIAL_REPORT = 10;
    }
    class Zone
    {
        public string Byte2 { get; set; }
        public string ZoneName { get; set; }
        public bool IsZoneOpen { get; set; }
    }
    class Byte2Data
    {
        public string Byte2 { get; set; }
        public string Name { get; set; }
    }
}
