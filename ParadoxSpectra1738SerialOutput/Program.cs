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

                int Byte1id = DataStream[0];
                string Event = events.Where(x => x.Byte1 == Byte1id).Select(x => x.EventName).DefaultIfEmpty($"Event_{Byte1id:X2}").First();
                int EventCategory = events.Where(x => x.Byte1 == Byte1id).Select(x => x.EventCategory).DefaultIfEmpty(DataStream[0]).First();

                int Byte2id = DataStream[1];
                string Message = Byte2id.ToString("X2");

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
                    if (Byte1id == 0x04) IsZoneOpen = true;
                    //update existing list with the IR statuses and activating/closing time
                    Zones.Where(x => x.Byte2 == Byte2id).Select(x => { x.IsZoneOpen = IsZoneOpen; x.ZoneEventTime = DateTimeOffset.Now; return x; }).ToList();
                    Message = Zones.Where(x => x.Byte2 == Byte2id).Select(x => $"{x.ZoneName} {(x.IsZoneOpen ? "Open" : "Closed")}").DefaultIfEmpty($"Zone_{Byte2id}").First();
                }
                if (isStatus) Message = PartitionStatuses.Where(x => x.Byte2 == Byte2id).Select(x => x.Name).DefaultIfEmpty($"Status_{Byte2id:X2}").First();
                if (isTrouble) Message = SystemTroubles.Where(x => x.Byte2 == Byte2id).Select(x => x.Name).DefaultIfEmpty($"Trouble_{Byte2id:X2}").First();
                if (isSpecialAlarm) Message = SpecialAlarms.Where(x => x.Byte2 == Byte2id).Select(x => x.Name).DefaultIfEmpty($"SpecialAlarm_{Byte2id:X2}").First();
                if (isSpecialArm) Message = SpecialArms.Where(x => x.Byte2 == Byte2id).Select(x => x.Name).DefaultIfEmpty($"SpecialArm_{Byte2id:X2}").First();
                if (isSpecialDisarm) Message = SpecialDisarms.Where(x => x.Byte2 == Byte2id).Select(x => x.Name).DefaultIfEmpty($"SpecialDisarm_{Byte2id:X2}").First();
                if (isNonReportEvents) Message = NonReportableEvents.Where(x => x.Byte2 == Byte2id).Select(x => x.Name).DefaultIfEmpty($"NonReportEvent_{Byte2id:X2}").First();
                if (isSpecialReport) Message = SpecialReportings.Where(x => x.Byte2 == Byte2id).Select(x => x.Name).DefaultIfEmpty($"SpecialReporting_{Byte2id:X2}").First();
                if (isRemoteControl) Message = $"Remote_{Byte2id:X2}";
                if (isAccessCode) Message = GetAccessCode(Byte1id, Byte2id);

                Console.Write($"{Event}, {Message}");
                Console.WriteLine();
            }
        }
        public static string GetAccessCode(int Byte1, int Byte2)
        {
            int twoBytes = (Byte1 << 8) + Byte2;
            int code = (twoBytes & 0x3F0) >> 4;

            string AccessCode = code < 10 ? $"User Code 00{code}" : $"User Code 0{code}";
            if (code == 1) AccessCode = "Master code";
            if (code == 2) AccessCode = "Master Code 1";
            if (code == 3) AccessCode = "Master Code 2";
            if (code == 48) AccessCode = "Duress Code";
            return AccessCode;
        }
        public static List<Event> events = new List<Event>
        {
            new Event(){Byte1 = 0x00, EventCategory = Category.ZONE, EventName = "Zone OK"},
            new Event(){Byte1 = 0x04, EventCategory = Category.ZONE, EventName = "Zone Open"},
            new Event(){Byte1 = 0x08, EventCategory = Category.STATUS, EventName = "Partition Status"},
            new Event(){Byte1 = 0x14, EventCategory = Category.NON_REPORT_EVENTS, EventName = "Non-Reportable Events"},
            new Event(){Byte1 = 0x18, EventCategory = Category.REMOTE_CONTROL, EventName = "Arm/Disarm with Remote Control"},
            new Event(){Byte1 = 0x1C, EventCategory = Category.REMOTE_CONTROL, EventName = "Button Pressed on Remote (B)"},
            new Event(){Byte1 = 0x20, EventCategory = Category.REMOTE_CONTROL, EventName = "Button Pressed on Remote (C)"},
            new Event(){Byte1 = 0x24, EventCategory = Category.REMOTE_CONTROL, EventName = "Button Pressed on Remote (D)"},
            new Event(){Byte1 = 0x28, EventCategory = Category.ACCESS_CODE, EventName = "Bypass programming"},
            new Event(){Byte1 = 0x29, EventCategory = Category.ACCESS_CODE, EventName = "Bypass programming"},
            new Event(){Byte1 = 0x2A, EventCategory = Category.ACCESS_CODE, EventName = "Bypass programming"},
            new Event(){Byte1 = 0x2B, EventCategory = Category.ACCESS_CODE, EventName = "Bypass programming"},
            new Event(){Byte1 = 0x2C, EventCategory = Category.ACCESS_CODE, EventName = "User Activated PGM"},
            new Event(){Byte1 = 0x2D, EventCategory = Category.ACCESS_CODE, EventName = "User Activated PGM"},
            new Event(){Byte1 = 0x2E, EventCategory = Category.ACCESS_CODE, EventName = "User Activated PGM"},
            new Event(){Byte1 = 0x2F, EventCategory = Category.ACCESS_CODE, EventName = "User Activated PGM"},
            new Event(){Byte1 = 0x30, EventCategory = Category.ZONE, EventName = "Zone with delay is breached"},
            new Event(){Byte1 = 0x34, EventCategory = Category.ACCESS_CODE, EventName = "Arm"},
            new Event(){Byte1 = 0x35, EventCategory = Category.ACCESS_CODE, EventName = "Arm"},
            new Event(){Byte1 = 0x36, EventCategory = Category.ACCESS_CODE, EventName = "Arm"},
            new Event(){Byte1 = 0x37, EventCategory = Category.ACCESS_CODE, EventName = "Arm"},
            new Event(){Byte1 = 0x38, EventCategory = Category.SPECIAL_ARM, EventName = "Special Arm"},
            new Event(){Byte1 = 0x3C, EventCategory = Category.ACCESS_CODE, EventName = "Disarm"},
            new Event(){Byte1 = 0x3D, EventCategory = Category.ACCESS_CODE, EventName = "Disarm"},
            new Event(){Byte1 = 0x3E, EventCategory = Category.ACCESS_CODE, EventName = "Disarm"},
            new Event(){Byte1 = 0x3F, EventCategory = Category.ACCESS_CODE, EventName = "Disarm"},
            new Event(){Byte1 = 0x40, EventCategory = Category.ACCESS_CODE, EventName = "Disarm after Alarm"},
            new Event(){Byte1 = 0x41, EventCategory = Category.ACCESS_CODE, EventName = "Disarm after Alarm"},
            new Event(){Byte1 = 0x42, EventCategory = Category.ACCESS_CODE, EventName = "Disarm after Alarm"},
            new Event(){Byte1 = 0x43, EventCategory = Category.ACCESS_CODE, EventName = "Disarm after Alarm"},
            new Event(){Byte1 = 0x44, EventCategory = Category.ACCESS_CODE, EventName = "Cancel Alarm"},
            new Event(){Byte1 = 0x45, EventCategory = Category.ACCESS_CODE, EventName = "Cancel Alarm"},
            new Event(){Byte1 = 0x46, EventCategory = Category.ACCESS_CODE, EventName = "Cancel Alarm"},
            new Event(){Byte1 = 0x47, EventCategory = Category.ACCESS_CODE, EventName = "Cancel Alarm"},
            new Event(){Byte1 = 0x48, EventCategory = Category.SPECIAL_DISARM, EventName = "Special Disarm"},
            new Event(){Byte1 = 0x4C, EventCategory = Category.ZONE, EventName = "Zone Bypassed on arming"},
            new Event(){Byte1 = 0x50, EventCategory = Category.ZONE, EventName = "Zone in Alarm"},
            new Event(){Byte1 = 0x54, EventCategory = Category.ZONE, EventName = "Fire Alarm"},
            new Event(){Byte1 = 0x58, EventCategory = Category.ZONE, EventName = "Zone Alarm restore"},
            new Event(){Byte1 = 0x5C, EventCategory = Category.ZONE, EventName = "Fire Alarm restore"},
            new Event(){Byte1 = 0x60, EventCategory = Category.SPECIAL_ALARM, EventName = "Special alarm"},
            new Event(){Byte1 = 0x64, EventCategory = Category.ZONE, EventName = "Auto zone shutdown"},
            new Event(){Byte1 = 0x68, EventCategory = Category.ZONE, EventName = "Zone tamper"},
            new Event(){Byte1 = 0x6C, EventCategory = Category.ZONE, EventName = "Zone tamper restore"},
            new Event(){Byte1 = 0x70, EventCategory = Category.TROUBLE, EventName = "System Trouble"},
            new Event(){Byte1 = 0x74, EventCategory = Category.TROUBLE, EventName = "System Trouble restore"},
            new Event(){Byte1 = 0x78, EventCategory = Category.SPECIAL_REPORT, EventName = "Special Reporting"},
            new Event(){Byte1 = 0x7C, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Supervision Loss"},
            new Event(){Byte1 = 0x80, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Supervision Loss Restore"},
            new Event(){Byte1 = 0x84, EventCategory = Category.ZONE, EventName = "Arming with a Keyswitch"},
            new Event(){Byte1 = 0x88, EventCategory = Category.ZONE, EventName = "Disarming with a Keyswitch"},
            new Event(){Byte1 = 0x8C, EventCategory = Category.ZONE, EventName = "Disarm after Alarm with a Keyswitch"},
            new Event(){Byte1 = 0x90, EventCategory = Category.ZONE, EventName = "Cancel Alarm with a Keyswitch"},
            new Event(){Byte1 = 0x94, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Low Battery"},
            new Event(){Byte1 = 0x98, EventCategory = Category.ZONE, EventName = "Wireless Transmitter Low Battery Restore"}
        };
        public static List<Byte2Data> PartitionStatuses = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = 0x01, Name = "System not ready"},
            new Byte2Data(){Byte2 = 0x11, Name = "System ready"},
            new Byte2Data(){Byte2 = 0x21, Name = "Steady alarm"},
            new Byte2Data(){Byte2 = 0x31, Name = "Pulsed alarm"},
            new Byte2Data(){Byte2 = 0x41, Name = "Pulsed or Steady Alarm"},
            new Byte2Data(){Byte2 = 0x51, Name = "Alarm in partition restored"},
            new Byte2Data(){Byte2 = 0x61, Name = "Bell Squawk Activated"},
            new Byte2Data(){Byte2 = 0x71, Name = "Bell Squawk Deactivated"},
            new Byte2Data(){Byte2 = 0x81, Name = "Ground start"},
            new Byte2Data(){Byte2 = 0x91, Name = "Disarm partition"},
            new Byte2Data(){Byte2 = 0xA1, Name = "Arm partition"},
            new Byte2Data(){Byte2 = 0xB1, Name = "Entry delay started"}
        };
        public static List<Byte2Data> SystemTroubles = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = 0x11, Name = "AC Loss"},
            new Byte2Data(){Byte2 = 0x21, Name = "Battery Failure"},
            new Byte2Data(){Byte2 = 0x31, Name = "Auxiliary current overload"},
            new Byte2Data(){Byte2 = 0x41, Name = "Bell current overload"},
            new Byte2Data(){Byte2 = 0x51, Name = "Bell disconnected"},
            new Byte2Data(){Byte2 = 0x61, Name = "Timer Loss"},
            new Byte2Data(){Byte2 = 0x71, Name = "Fire Loop Trouble"},
            new Byte2Data(){Byte2 = 0x81, Name = "Future use"},
            new Byte2Data(){Byte2 = 0x91, Name = "Module Fault"},
            new Byte2Data(){Byte2 = 0xA1, Name = "Printer Fault"},
            new Byte2Data(){Byte2 = 0xB1, Name = "Fail to Communicate"}
        };
        public static List<Byte2Data> NonReportableEvents = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = 0x01, Name = "Telephone Line Trouble"},
            new Byte2Data(){Byte2 = 0x11, Name = "Reset smoke detectors"},
            new Byte2Data(){Byte2 = 0x21, Name = "Instant arming"},
            new Byte2Data(){Byte2 = 0x31, Name = "Stay arming"},
            new Byte2Data(){Byte2 = 0x41, Name = "Force arming"},
            new Byte2Data(){Byte2 = 0x51, Name = "Fast Exit (Force & Regular Only)"},
            new Byte2Data(){Byte2 = 0x61, Name = "PC Fail to Communicate"},
            new Byte2Data(){Byte2 = 0x71, Name = "Midnight"}
        };
        public static List<Byte2Data> SpecialAlarms = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = 0x01, Name = "Emergency, keys [1] [3]"},
            new Byte2Data(){Byte2 = 0x11, Name = "Auxiliary, keys [4] [6]"},
            new Byte2Data(){Byte2 = 0x21, Name = "Fire, keys [7] [9]"},
            new Byte2Data(){Byte2 = 0x31, Name = "Recent closing"},
            new Byte2Data(){Byte2 = 0x41, Name = "Auto Zone Shutdown"},
            new Byte2Data(){Byte2 = 0x51, Name = "Duress alarm"},
            new Byte2Data(){Byte2 = 0x61, Name = "Keypad lockout"}
        };
        public static List<Byte2Data> SpecialReportings = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = 0x01, Name = "System power up"},
            new Byte2Data(){Byte2 = 0x11, Name = "Test report"},
            new Byte2Data(){Byte2 = 0x21, Name = "WinLoad Software Access"},
            new Byte2Data(){Byte2 = 0x31, Name = "WinLoad Software Access finished"},
            new Byte2Data(){Byte2 = 0x41, Name = "Installer enters programming mode"},
            new Byte2Data(){Byte2 = 0x51, Name = "Installer exits programming mode"}
        };
        public static List<Byte2Data> SpecialDisarms = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = 0x01, Name = "Cancel Auto Arm (timed/no movement)"},
            new Byte2Data(){Byte2 = 0x11, Name = "Disarm with WinLoad Software"},
            new Byte2Data(){Byte2 = 0x21, Name = "Disarm after alarm with WinLoad Software"},
            new Byte2Data(){Byte2 = 0x31, Name = "Cancel Alarm with WinLoad Software"}
        };
        public static List<Byte2Data> SpecialArms = new List<Byte2Data>
        {
            new Byte2Data(){Byte2 = 0x01, Name = "Auto arming (timed/no movement)"},
            new Byte2Data(){Byte2 = 0x11, Name = "Late to Close (Auto-Arming failed)"},
            new Byte2Data(){Byte2 = 0x21, Name = "No Movement Auto-Arming"},
            new Byte2Data(){Byte2 = 0x31, Name = "Partial Arming (Stay, Force, Instant, Bypass)"},
            new Byte2Data(){Byte2 = 0x41, Name = "One-Touch Arming"},
            new Byte2Data(){Byte2 = 0x51, Name = "Arm with WinLoad Software"},
            new Byte2Data(){Byte2 = 0x71, Name = "Closing Delinquency"}
        };
        public static List<Zone> Zones = new List<Zone>
        {
            new Zone(){Byte2 = 0x11, IsZoneOpen=false, ZoneName = "DOOR"},
            new Zone(){Byte2 = 0x21, IsZoneOpen=false, ZoneName = "ENTRY",},
            new Zone(){Byte2 = 0x31, IsZoneOpen=false, ZoneName = "LIVING ROOM"},
            new Zone(){Byte2 = 0x41, IsZoneOpen=false, ZoneName = "OFFICE"},
            new Zone(){Byte2 = 0x51, IsZoneOpen=false, ZoneName = "HALL"},
            new Zone(){Byte2 = 0x61, IsZoneOpen=false, ZoneName = "BEDROOM"},
            new Zone(){Byte2 = 0x71, IsZoneOpen=false, ZoneName = "FIRE"},
            new Zone(){Byte2 = 0x81, IsZoneOpen=false, ZoneName = "TECHNO"},
            new Zone(){Byte2 = 0x91, IsZoneOpen=false, ZoneName = "PIANO"}
         };
    }
    class Event
    {
        public int Byte1 { get; set; }
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
        public int Byte2 { get; set; }
        public string ZoneName { get; set; }
        public bool IsZoneOpen { get; set; }
        public DateTimeOffset ZoneEventTime { get; set; }
    }
    class Byte2Data
    {
        public int Byte2 { get; set; }
        public string Name { get; set; }
    }
}
