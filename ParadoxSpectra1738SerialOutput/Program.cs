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
                }
                catch (Exception e) { Console.WriteLine($"Timeout {e}"); }

                int msb = DataStream[2];
                int lsb = DataStream[3];

                //getting minute and hour with shift operations
                int hour = msb >> 3;
                int minute = ((msb & 3) << 4) + (lsb >> 4);
                string paradoxTime = $"{(hour < 10 ? $"0{hour}" : $"{hour}")}:{(minute < 10 ? $"0{minute}" : $"{minute}")}";

                Console.Write($"{paradoxTime} ");

                for (int i = 0; i < DataStream.Length; i++)
                {
                    Console.Write($"{DataStream[i]:X2} ");
                }

                int EventId = DataStream[0] >> 2;
                int CategoryId = ((DataStream[0] & 3) << 4) + (DataStream[1] >> 4);

                string Event = events.Where(x => x.EventId == EventId).Select(x => x.EventName).DefaultIfEmpty($"Event_{EventId}").First();
                int EventCategory = events.Where(x => x.EventId == EventId).Select(x => x.EventCategory).DefaultIfEmpty(EventId).First();

                string Message = CategoryId.ToString();

                bool isZoneEvent = EventCategory == Category.ZONE;
                bool isStatus = EventCategory == Category.STATUS;
                bool isTrouble = EventCategory == Category.TROUBLE;
                bool isAccessCode = EventCategory == Category.ACCESS_CODE;
                bool isSpecialAlarm = EventCategory == Category.SPECIAL_ALARM;
                bool isSpecialArm = EventCategory == Category.SPECIAL_ARM;
                bool isSpecialDisarm = EventCategory == Category.SPECIAL_DISARM;
                bool isNonReportEvents = EventCategory == Category.NON_REPORT_EVENTS;
                bool isSpecialReport = EventCategory == Category.SPECIAL_REPORT;
                bool isRemoteControl = EventCategory == Category.REMOTE_CONTROL;

                if (isZoneEvent)
                {
                    bool IsZoneOpen = false;
                    if (EventId == 1) IsZoneOpen = true;
                    //update existing list with the IR statuses and activating/closing time
                    Zones.Where(x => x.CategoryId == CategoryId).Select(x => { x.IsZoneOpen = IsZoneOpen; x.ZoneEventTime = DateTimeOffset.Now; return x; }).ToList();
                    Message = Zones.Where(x => x.CategoryId == CategoryId).Select(x => $"{x.ZoneName}").DefaultIfEmpty($"Zone_{CategoryId}").First();
                }
                if (isStatus) Message = PartitionStatuses.Where(x => x.CategoryId == CategoryId).Select(x => x.Name).DefaultIfEmpty($"Status_{CategoryId}").First();
                if (isTrouble) Message = SystemTroubles.Where(x => x.CategoryId == CategoryId).Select(x => x.Name).DefaultIfEmpty($"Trouble_{CategoryId}").First();
                if (isSpecialAlarm) Message = SpecialAlarms.Where(x => x.CategoryId == CategoryId).Select(x => x.Name).DefaultIfEmpty($"SpecialAlarm_{CategoryId}").First();
                if (isSpecialArm) Message = SpecialArms.Where(x => x.CategoryId == CategoryId).Select(x => x.Name).DefaultIfEmpty($"SpecialArm_{CategoryId}").First();
                if (isSpecialDisarm) Message = SpecialDisarms.Where(x => x.CategoryId == CategoryId).Select(x => x.Name).DefaultIfEmpty($"SpecialDisarm_{CategoryId}").First();
                if (isNonReportEvents) Message = NonReportableEvents.Where(x => x.CategoryId == CategoryId).Select(x => x.Name).DefaultIfEmpty($"NonReportEvent_{CategoryId}").First();
                if (isSpecialReport) Message = SpecialReportings.Where(x => x.CategoryId == CategoryId).Select(x => x.Name).DefaultIfEmpty($"SpecialReporting_{CategoryId}").First();
                if (isRemoteControl) Message = $"Remote_{CategoryId}";
                if (isAccessCode) Message = GetAccessCode(CategoryId);

                Console.Write($"{Event}, {Message}");
                Console.WriteLine();
            }
        }
        public static string GetAccessCode(int code)
        {
            string AccessCode = code < 10 ? $"User Code 00{code}" : $"User Code 0{code}";
            if (code == 1) AccessCode = "Master code";
            if (code == 2) AccessCode = "Master Code 1";
            if (code == 3) AccessCode = "Master Code 2";
            if (code == 48) AccessCode = "Duress Code";
            return AccessCode;
        }
        public static List<Event> events = new List<Event>
        {
            new Event(){EventId = 0, EventCategory = Category.ZONE, EventName = "Zone OK"},
            new Event(){EventId = 1, EventCategory = Category.ZONE, EventName = "Zone Open"},
            new Event(){EventId = 2, EventCategory = Category.STATUS, EventName = "Partition Status"},
            new Event(){EventId = 5, EventCategory = Category.NON_REPORT_EVENTS, EventName = "Non-Reportable Events"},
            new Event(){EventId = 6, EventCategory = Category.REMOTE_CONTROL, EventName = "Arm/Disarm with Remote Control"},
            new Event(){EventId = 7, EventCategory = Category.REMOTE_CONTROL, EventName = "Button Pressed on Remote (B)"},
            new Event(){EventId = 8, EventCategory = Category.REMOTE_CONTROL, EventName = "Button Pressed on Remote (C)"},
            new Event(){EventId = 9, EventCategory = Category.REMOTE_CONTROL, EventName = "Button Pressed on Remote (D)"},
            new Event(){EventId = 10, EventCategory = Category.ACCESS_CODE, EventName = "Bypass programming"},
            new Event(){EventId = 11, EventCategory = Category.ACCESS_CODE, EventName = "User Activated PGM"},
            new Event(){EventId = 12, EventCategory = Category.ZONE, EventName = "Zone with delay is breached"},
            new Event(){EventId = 13, EventCategory = Category.ACCESS_CODE, EventName = "Arm"},
            new Event(){EventId = 14, EventCategory = Category.SPECIAL_ARM, EventName = "Special Arm"},
            new Event(){EventId = 15, EventCategory = Category.ACCESS_CODE, EventName = "Disarm"},
            new Event(){EventId = 16, EventCategory = Category.ACCESS_CODE, EventName = "Disarm after Alarm"},
            new Event(){EventId = 17, EventCategory = Category.ACCESS_CODE, EventName = "Cancel Alarm"},
            new Event(){EventId = 18, EventCategory = Category.SPECIAL_DISARM, EventName = "Special Disarm"},
            new Event(){EventId = 19, EventCategory = Category.ZONE, EventName = "Zone Bypassed on arming"},
            new Event(){EventId = 20, EventCategory = Category.ZONE, EventName = "Zone in Alarm"},
            new Event(){EventId = 21, EventCategory = Category.ZONE, EventName = "Fire Alarm"},
            new Event(){EventId = 22, EventCategory = Category.ZONE, EventName = "Zone Alarm restore"},
            new Event(){EventId = 23, EventCategory = Category.ZONE, EventName = "Fire Alarm restore"},
            new Event(){EventId = 24, EventCategory = Category.SPECIAL_ALARM, EventName = "Special alarm"},
            new Event(){EventId = 25, EventCategory = Category.ZONE, EventName = "Auto zone shutdown"},
            new Event(){EventId = 26, EventCategory = Category.ZONE, EventName = "Zone tamper"},
            new Event(){EventId = 27, EventCategory = Category.ZONE, EventName = "Zone tamper restore"},
            new Event(){EventId = 28, EventCategory = Category.TROUBLE, EventName = "System Trouble"},
            new Event(){EventId = 29, EventCategory = Category.TROUBLE, EventName = "System Trouble restore"},
            new Event(){EventId = 30, EventCategory = Category.SPECIAL_REPORT, EventName = "Special Reporting"},
            new Event(){EventId = 31, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Supervision Loss"},
            new Event(){EventId = 32, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Supervision Loss Restore"},
            new Event(){EventId = 33, EventCategory = Category.ZONE, EventName = "Arming with a Keyswitch"},
            new Event(){EventId = 34, EventCategory = Category.ZONE, EventName = "Disarming with a Keyswitch"},
            new Event(){EventId = 35, EventCategory = Category.ZONE, EventName = "Disarm after Alarm with a Keyswitch"},
            new Event(){EventId = 36, EventCategory = Category.ZONE, EventName = "Cancel Alarm with a Keyswitch"},
            new Event(){EventId = 37, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Low Battery"},
            new Event(){EventId = 38, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Low Battery Restore"}
        };
        public static List<Byte2Data> PartitionStatuses = new List<Byte2Data>
        {
            new Byte2Data(){CategoryId = 0, Name = "System not ready"},
            new Byte2Data(){CategoryId = 1, Name = "System ready"},
            new Byte2Data(){CategoryId = 2, Name = "Steady alarm"},
            new Byte2Data(){CategoryId = 3, Name = "Pulsed alarm"},
            new Byte2Data(){CategoryId = 4, Name = "Pulsed or Steady Alarm"},
            new Byte2Data(){CategoryId = 5, Name = "Alarm in partition restored"},
            new Byte2Data(){CategoryId = 6, Name = "Bell Squawk Activated"},
            new Byte2Data(){CategoryId = 7, Name = "Bell Squawk Deactivated"},
            new Byte2Data(){CategoryId = 8, Name = "Ground start"},
            new Byte2Data(){CategoryId = 9, Name = "Disarm partition"},
            new Byte2Data(){CategoryId = 10, Name = "Arm partition"},
            new Byte2Data(){CategoryId = 11, Name = "Entry delay started"}
        };
        public static List<Byte2Data> SystemTroubles = new List<Byte2Data>
        {
            new Byte2Data(){CategoryId =  1, Name = "AC Loss"},
            new Byte2Data(){CategoryId =  2, Name = "Battery Failure"},
            new Byte2Data(){CategoryId =  3, Name = "Auxiliary current overload"},
            new Byte2Data(){CategoryId =  4, Name = "Bell current overload"},
            new Byte2Data(){CategoryId =  5, Name = "Bell disconnected"},
            new Byte2Data(){CategoryId =  6, Name = "Timer Loss"},
            new Byte2Data(){CategoryId =  7, Name = "Fire Loop Trouble"},
            new Byte2Data(){CategoryId =  8, Name = "Future use"},
            new Byte2Data(){CategoryId =  9, Name = "Module Fault"},
            new Byte2Data(){CategoryId = 10, Name = "Printer Fault"},
            new Byte2Data(){CategoryId = 11, Name = "Fail to Communicate"}
        };
        public static List<Byte2Data> NonReportableEvents = new List<Byte2Data>
        {
            new Byte2Data(){CategoryId =  0, Name = "Telephone Line Trouble"},
            new Byte2Data(){CategoryId =  1, Name = "Reset smoke detectors"},
            new Byte2Data(){CategoryId =  2, Name = "Instant arming"},
            new Byte2Data(){CategoryId =  3, Name = "Stay arming"},
            new Byte2Data(){CategoryId =  4, Name = "Force arming"},
            new Byte2Data(){CategoryId =  5, Name = "Fast Exit (Force & Regular Only)"},
            new Byte2Data(){CategoryId =  6, Name = "PC Fail to Communicate"},
            new Byte2Data(){CategoryId =  7, Name = "Midnight"}
        };
        public static List<Byte2Data> SpecialAlarms = new List<Byte2Data>
        {
            new Byte2Data(){CategoryId =  0, Name = "Emergency, keys [1] [3]"},
            new Byte2Data(){CategoryId =  1, Name = "Auxiliary, keys [4] [6]"},
            new Byte2Data(){CategoryId =  2, Name = "Fire, keys [7] [9]"},
            new Byte2Data(){CategoryId =  3, Name = "Recent closing"},
            new Byte2Data(){CategoryId =  4, Name = "Auto Zone Shutdown"},
            new Byte2Data(){CategoryId =  5, Name = "Duress alarm"},
            new Byte2Data(){CategoryId =  6, Name = "Keypad lockout"}
        };
        public static List<Byte2Data> SpecialReportings = new List<Byte2Data>
        {
            new Byte2Data(){CategoryId =  0, Name = "System power up"},
            new Byte2Data(){CategoryId =  1, Name = "Test report"},
            new Byte2Data(){CategoryId =  2, Name = "WinLoad Software Access"},
            new Byte2Data(){CategoryId =  3, Name = "WinLoad Software Access finished"},
            new Byte2Data(){CategoryId =  4, Name = "Installer enters programming mode"},
            new Byte2Data(){CategoryId =  5, Name = "Installer exits programming mode"}
        };
        public static List<Byte2Data> SpecialDisarms = new List<Byte2Data>
        {
            new Byte2Data(){CategoryId =  0, Name = "Cancel Auto Arm (timed/no movement)"},
            new Byte2Data(){CategoryId =  1, Name = "Disarm with WinLoad Software"},
            new Byte2Data(){CategoryId =  2, Name = "Disarm after alarm with WinLoad Software"},
            new Byte2Data(){CategoryId =  3, Name = "Cancel Alarm with WinLoad Software"}
        };
        public static List<Byte2Data> SpecialArms = new List<Byte2Data>
        {
            new Byte2Data(){CategoryId =  0, Name = "Auto arming (timed/no movement)"},
            new Byte2Data(){CategoryId =  1, Name = "Late to Close (Auto-Arming failed)"},
            new Byte2Data(){CategoryId =  2, Name = "No Movement Auto-Arming"},
            new Byte2Data(){CategoryId =  3, Name = "Partial Arming (Stay, Force, Instant, Bypass)"},
            new Byte2Data(){CategoryId =  4, Name = "One-Touch Arming"},
            new Byte2Data(){CategoryId =  5, Name = "Arm with WinLoad Software"},
            new Byte2Data(){CategoryId =  7, Name = "Closing Delinquency"}
        };
        public static List<Zone> Zones = new List<Zone>
        {
            new Zone(){CategoryId =  1, IsZoneOpen=false, ZoneName = "DOOR"},
            new Zone(){CategoryId =  2, IsZoneOpen=false, ZoneName = "ENTRY",},
            new Zone(){CategoryId =  3, IsZoneOpen=false, ZoneName = "LIVING ROOM"},
            new Zone(){CategoryId =  4, IsZoneOpen=false, ZoneName = "OFFICE"},
            new Zone(){CategoryId =  5, IsZoneOpen=false, ZoneName = "HALL"},
            new Zone(){CategoryId =  6, IsZoneOpen=false, ZoneName = "BEDROOM"},
            new Zone(){CategoryId =  7, IsZoneOpen=false, ZoneName = "FIRE"},
            new Zone(){CategoryId =  8, IsZoneOpen=false, ZoneName = "TECHNO"},
            new Zone(){CategoryId =  9, IsZoneOpen=false, ZoneName = "PIANO"}
         };
    }
    class Event
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public int EventCategory { get; set; }
    }
    class Category
    {
        public const int ZONE = 1;
        public const int STATUS = 2;
        public const int TROUBLE = 3;
        public const int ACCESS_CODE = 4;
        public const int SPECIAL_ALARM = 5;
        public const int SPECIAL_ARM = 6;
        public const int SPECIAL_DISARM = 7;
        public const int NON_REPORT_EVENTS = 8;
        public const int SPECIAL_REPORT = 9;
        public const int REMOTE_CONTROL = 10;
    }
    class Zone
    {
        public int CategoryId { get; set; }
        public string ZoneName { get; set; }
        public bool IsZoneOpen { get; set; }
        public DateTimeOffset ZoneEventTime { get; set; }
    }
    class Byte2Data
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
    }
}
