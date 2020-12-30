using System;
using System.IO.Ports;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ParadoxSpectra1738SerialOutput
{
    //Raspberry PI Com Port: /dev/ttyAMA0
    //Paradox Spectra 1738 Serial Port
    class Program
    {
        static SerialPort _serialPort;
        static byte[] inData = new byte[4];
        static byte index = 0;
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
            _serialPort = new SerialPort(ComPort, baudrate)
            {
                // Set the read/write timeouts
                //ReadTimeout = 1500,
                WriteTimeout = 1500
            };
            try
            {
                _serialPort.Open();
                while (true)
                {
                    loop();
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{ex}");
            }
        }
        public static List<Event> events = new List<Event>
        {
            new Event(){Data = "00", Category = Category.SENSOR, Name = "Zone Closed"},
            new Event(){Data = "04", Category = Category.SENSOR, Name = "Zone Open"},
            new Event(){Data = "08", Category = Category.STATUS, Name = "Status"},
            new Event(){Data = "34", Category = Category.USER, Name = "Arming"},
            new Event(){Data = "3C", Category = Category.USER, Name = "Disarming"},
            new Event(){Data = "40", Category = Category.USER, Name = "Disarming after Alarm"},
            new Event(){Data = "50", Category = Category.SENSOR, Name = "Zone in Alarm"},
            new Event(){Data = "58", Category = Category.SENSOR, Name = "Zone Alarm restore"},
            new Event(){Data = "70", Category = Category.TROUBLE, Name = "Trouble fail"},
            new Event(){Data = "74", Category = Category.TROUBLE, Name = "Trouble back to normal"}
        };
        public static List<Status> statuses = new List<Status>
        {
            new Status(){Data = "01", Name = "Zones open"},
            new Status(){Data = "11", Name = "Zones closed"},
            new Status(){Data = "21", Name = "Alarm21/Bell"},
            new Status(){Data = "41", Name = "Alarm41/Bell"},
            new Status(){Data = "51", Name = "Alarm occurred during arm"},
            new Status(){Data = "61", Name = "ArmCode61"},
            new Status(){Data = "71", Name = "ArmCode71"},
            new Status(){Data = "91", Name = "Disarmed"},
            new Status(){Data = "A1", Name = "Armed"},
            new Status(){Data = "B1", Name = "Entry delay started"},
        };
        public static List<Trouble> troubles = new List<Trouble>
        {
            new Trouble(){Data = "21", Name = "Battery"},
            new Trouble(){Data = "51", Name = "Bell"}
        };
        public static List<Message> sensors = new List<Message> {
            new Message(){Data = "11", Name = "DOOR"},
            new Message(){Data = "21", Name = "ENTRY/PIANO"},
            new Message(){Data = "31", Name = "LIVING ROOM"},
            new Message(){Data = "41", Name = "OFFICE"},
            new Message(){Data = "51", Name = "HALL"},
            new Message(){Data = "61", Name = "BEDROOM"},
            new Message(){Data = "71", Name = "FIRE"},
            new Message(){Data = "81", Name = "TECHNO"}
            };
        public static void loop()
        {

            try
            {
                if (_serialPort.BytesToRead < 4)
                {
                    index = 0;
                    while (index < 4)
                    {
                        inData[index++] = (byte)_serialPort.ReadByte();
                    }
                }
                int msb = inData[2];
                int lsb = inData[3];

                //thats a clock, nice reverse engineering from octal logic
                int hour = msb / 8;
                int minute = msb % 8 * 16 + lsb / 16;

                TimeSpan time = new TimeSpan(hour, minute, 0);
                DateTime dateTime = DateTime.Now.Date.Add(time);
                Console.Write($"{dateTime:t} ");

                for (int i = 0; i < inData.Length; i++)
                {
                    Console.Write($"{inData[i]:X2} ");
                }

                string EventID = inData[0].ToString("X2");
                string Event = events.Where(x => x.Data == EventID).Select(x => x.Name).DefaultIfEmpty($"NoName {EventID}").First();
                int EventCategory = events.Where(x => x.Data == EventID).Select(x => x.Category).DefaultIfEmpty(inData[0]).First();

                string MessageID = inData[1].ToString("X2");
                string Message = MessageID;

                bool isZoneAction = EventCategory == Category.SENSOR;
                bool isUserAction = EventCategory == Category.USER;
                bool isTrouble = EventCategory == Category.TROUBLE;
                bool isStatus = EventCategory == Category.STATUS;

                if (!isStatus)
                    Console.Write($" {Event}");
                if (isZoneAction)
                {
                    Message = sensors.Where(x => x.Data == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();
                }
                if (isStatus)
                {
                    Message = statuses.Where(x => x.Data == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();
                }
                if (isTrouble)
                {
                    Message = troubles.Where(x => x.Data == MessageID).Select(x => x.Name).DefaultIfEmpty($"NoName {MessageID}").First();
                }
                if (isUserAction)
                {
                    Message = $"User:{MessageID}";
                }
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
    class Event
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public int Category { get; set; }
    }
    class Category
    {
        public const int SENSOR = 1;
        public const int STATUS = 2;
        public const int TROUBLE = 3;
        public const int USER = 4;
    }
    class Message
    {
        public string Data { get; set; }
        public string Name { get; set; }
    }
    class Trouble
    {
        public string Data { get; set; }
        public string Name { get; set; }
    }
    class Status
    {
        public string Data { get; set; }
        public string Name { get; set; }
    }
}
