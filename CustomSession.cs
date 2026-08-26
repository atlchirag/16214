using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Linq;
using System.Collections.Generic;
using GPSTrackerListeners.Util.Alert;
using System.Threading;
using System.Data;
using System.Timers;
using System.Net.Sockets;

namespace GPSTrackerListeners.AtlantaNew
{
    public class Student
    {
        public int id { get; set; }
        public int RouteId { get; set; }
        public string RFID { get; set; }
        public string MobileNo { get; set; }
        public string student_name { get; set; }

        public double SchoolLat { get; set; }
        public double SchoolLng { get; set; }


        public int pick_B { get; set; }
        public int pick_D { get; set; }
        public int drop_B { get; set; }
        public int drop_D { get; set; }
        public int school_In { get; set; }
        public int school_Out { get; set; }

        public int msg_id { get; set; }
    }

    public class Stop
    {
        public int RouteId { get; set; }
        public string StopName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Via { get; set; }
        public int StopOrder { get; set; }
    }
    public class CustomSession : AppSession<CustomSession>
    {
        protected override void HandleUnknownRequest(StringRequestInfo requestInfo)
        {

        }

        public string ServiceId { get; set; }
        public bool IsAtlTrackingDevice { get; set; }
        public int RouteId { get; set; }
        public string PrevLatitude { get; set; }
        public string RouteName { get; set; }
        public TimeSpan RouteStartTime { get; set; }
        public TimeSpan RouteEndTime { get; set; }
        public double SchoolLat { get; set; }
        public double SchoolLng { get; set; }
        public List<int> AttendanceSubscription { get; set; }


        public string PrevLongitude { get; set; }

        public bool IsPortUpdated { get; set; }

        public DateTime RequestTime { get; set; }

        public DateTime ResponseTime { get; set; }

        public bool IsTimeout { get; set; }

        public const int TimeoutPeriod = 2; // Minutes

        public bool IsResponsePending { get; set; }

        public string CommandRequestId { get; set; }

        public string IMEI { get; set; }

        public PanicAlert PanicAlert { get; set; }

        public bool IsPanicAlert { get; set; }

        public TcpClient TcpClient { get; set; }

        public int UTCOffset { get; set; }

        public int UserId { get; set; }

        public string VehicleName { get; set; }

        public bool IsAlertCheck { get; set; }

        public string Protocol { get; set; }

        public Dictionary<string, string> RFDetail { get; set; }

        public double FuelValue { get; set; }

        public double OdometerOffset { get; set; }

        public double LastOdometer { get; set; }
        public DateTime LastGPSTime { get; set; }
        public double DataConsumed { get; set; }
        public DateTime CurrentGPSTime { get; set; }
        public bool IsPanicSent { get; set; }
        public DateTime LastPanicTime { get; set; }
        public bool is_port_Set { get; set; }
        public int command_id { get; set; }
        public double LastLat { get; set; }

        public double LastLng { get; set; }

        public string MobileNo { get; set; }

        public byte IsODOmeterReset { get; set; }

        public int PanicCount { get; set; }

        public byte IsInside { get; set; }
        public int TripId { get; set; }
        public List<Stop> stops { get; set; }

        public List<Student> StudentList { get; set; }

        public string SMSAPI { get; set; }

        public DataTable DtMobileIds { get; set; }

        public int service_from { get; set; }
        public string sender_id { get; set; }


        protected override void OnSessionStarted()
        {
            base.OnSessionStarted();
            FuelValue = -1;
            OdometerOffset = -1;
            LastOdometer = -1;
            LastPanicTime = new DateTime();
            StudentList = new List<Student>();
        }




    }
    
    
}
