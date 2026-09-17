using System;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.Data.SqlClient;
using GPSTrackerListeners.Util;
using SuperSocket.SocketBase.Config;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;
using MongoDB.Driver.Builders;
using System.Linq.Expressions;

namespace GPSTrackerListeners.AtlantaNew
{
    public class AtlantaNewServer : AppServer<CustomSession>
    {

        private int PICK_BOARD = 1;
        private int PICK_DEBOARD = 2;
        private int DROP_BOARD = 3;
        private int DROP_DEBOARD = 4;
        private string m_FolderPath;
        private string m_FolderPath2;

        private string m_userName;
        private int i = 0;
        string serviceid = "0";

        public AtlantaNewServer()
            : base(new DefaultReceiveFilterFactory<CustomReceiveFilter, StringRequestInfo>())
        {

            this.NewRequestReceived += new RequestHandler<CustomSession, StringRequestInfo>(server_NewRequestReceived);

        }

        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {

            m_FolderPath = @"E:\ListenerData\AtlantaNew\16214";
            m_FolderPath2 = @"E:\ListenerData\AtlantaNew\1299";
            return true;
        }


        protected override void OnNewSessionConnected(CustomSession session)
        {
            base.OnNewSessionConnected(session);

        }

        protected override void OnSessionClosed(CustomSession session, CloseReason reason)
        {

            base.OnSessionClosed(session, reason);
        }





        private void server_NewRequestReceived(CustomSession session, StringRequestInfo requestInfo)
        {

            General gen = new General();
         
        
            //if (requestInfo.Key == "response")
            //{
            //    insertVersionCommand(requestInfo.Body, session, gen);
            //    General.WriteToLogFile(requestInfo.Body, m_FolderPath, "response.txt");
            //    insertResponse(requestInfo.Body, session,gen);
            //}
            //sendVersionCommand(requestInfo, session, gen);
            General.HandleNewRequest(session, requestInfo, m_FolderPath, m_userName);

            if (requestInfo.Key == "response")
            {
                insertResponse(requestInfo.Body, session, gen);
                General.WriteToLogFile(requestInfo.Body, m_FolderPath, "response.txt");
                General.WriteToLogFile(requestInfo.Parameters[1].Substring(0, 15) + ":" + requestInfo.Body, m_FolderPath, "res.txt");
                CheckCommand(session.ServiceId, session, gen, requestInfo.Key);
                return;
            }

            if (!string.IsNullOrEmpty(session.ServiceId))
            {
                InsertIntoDB(requestInfo, session,gen);

                CheckCommand(session.ServiceId, session,gen,requestInfo.Key);
            }


            

        }

        private async void InsertIntoDB(StringRequestInfo requestInfo, CustomSession session,General gen)
        {

            DataTable dt = new DataTable();


            int totalcount = 0;

            // session.SessionID();

            int counter = 0;
            foreach (string data in requestInfo.Parameters)
            {
                counter += 1;
                if (counter == 1)
                    continue;

                string[] fieldsArray = data.Trim().Split(',');

                string gpsValidity = fieldsArray[2];



                //GPRMC Data
                //string gpsTime = General.GetGPSDateTime(fieldsArray[9], fieldsArray[1]).ToString("yyyy-MM-dd HH:mm:ss");
                DateTime gpsDateTime = General.GetGPSDateTime(fieldsArray[9], fieldsArray[1]);

                string gpsTime = gpsDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                string sysProcTime = gpsDateTime.AddMinutes(330)
                                                .ToString("yyyy-MM-dd HH:mm:ss");

                // session.CurrentGPSTime = Convert.ToDateTime(gpsTime);
                if (Convert.ToDateTime(gpsTime) == new DateTime())
                {
                    General.WriteToLogFile(requestInfo.Body, m_FolderPath, "exDate.txt");
                    //continue;
                }

                double latitude = string.IsNullOrEmpty(fieldsArray[3]) ? 0 : General.GetLatitude(fieldsArray[3]);
                string latitudeDirection = fieldsArray[4];

                if (latitudeDirection == "S")
                    latitude = latitude * -1;

                double longitude = string.IsNullOrEmpty(fieldsArray[5]) ? 0 : General.GetLongitude(fieldsArray[5]);

                if (latitude == 0 || longitude == 0 || requestInfo.Key.Length < 15)
                {
                    General.WriteToLogFile(requestInfo.Body, m_FolderPath, "exInvalidPacket.txt");

                }

                string longitudeDirection = fieldsArray[6];

                if (longitudeDirection == "W")
                    longitude = longitude * -1;

                double gpsSpeed = string.IsNullOrEmpty(fieldsArray[7]) ? 0 : Convert.ToDouble(fieldsArray[7]);
                gpsSpeed = gpsSpeed * 1.852; // In kilometers
                string gpsOrientation_trueCourse = (fieldsArray[8] == "") ? "0.000000000" : fieldsArray[8];
                string variation = fieldsArray[10];
                string checksum = fieldsArray[12];
                string location = "";
                int locationFlag = 0;
                // Locaction enabled or not 
                if (checksum.Contains("$LOC"))
                {
                    checksum = checksum.Substring(0, checksum.IndexOf('$'));
                    location = fieldsArray[13];
                    locationFlag = 1;
                }
                // Alerts of various I/O. skipping '#' in first place
                char[] IOAlertsArray = fieldsArray[13 + locationFlag].ToCharArray();

                int ignition_i2 = Convert.ToInt16(IOAlertsArray[1].ToString());
                int door_i8 = Convert.ToInt16(IOAlertsArray[2].ToString());
                int sos_i3 = Convert.ToInt16(IOAlertsArray[3].ToString());
                int i4 = Convert.ToInt16(IOAlertsArray[4].ToString());
                int i5 = Convert.ToInt16(IOAlertsArray[5].ToString());
                int ac_i6 = Convert.ToInt16(IOAlertsArray[6].ToString());
                int i7 = Convert.ToInt16(IOAlertsArray[7].ToString());
                int mainPower_i1 = Convert.ToInt16(IOAlertsArray[8].ToString());
                int harshSpeeding_i9 = Convert.ToInt16(IOAlertsArray[9].ToString());
                int harshBreaking_i10 = Convert.ToInt16(IOAlertsArray[10].ToString());
                int armDisarm_i11 = Convert.ToInt16(IOAlertsArray[11].ToString());
                int sleepStatus_i12 = Convert.ToInt16(IOAlertsArray[12].ToString());
                int generalRelayStatus_i13 = Convert.ToInt16(IOAlertsArray[13].ToString());
                int accelerometerStatus_i14 = Convert.ToInt16(IOAlertsArray[14].ToString());

                try
                {
                    //   
                    // Panic Alert
                    if (sos_i3 == 1)
                    {


                        if (session.IsPanicAlert)
                        {



                            if (latitude == 0.0 || longitude == 0.0)
                            {
                                continue;
                            }
                            string address = General.GetLocationFromLatLong(latitude, longitude);




                            string insertQuery = @"insert into panicalert_log  (sys_service_id,lastAlertTime,alert_msg,readFlag,gps_latitude,gps_longitude)  values (" + session.ServiceId + ",'" + Convert.ToDateTime(gpsTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "','Warning: Panic button has been pressed on vehicle " + session.VehicleName + " near " + address + " on  " + Convert.ToDateTime(gpsTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "' ,0,'" + latitude + "','" + longitude + "')";
                            gen.DML(insertQuery);

                            if (General.IsAlertTime(Convert.ToDateTime(session.PanicAlert.FromAlertTime.ToString()), Convert.ToDateTime(session.PanicAlert.ToAlertTime.ToString())))
                            {
                                if (session.LastPanicTime == new DateTime())
                                    session.LastPanicTime = DateTime.Now;

                                if (!session.IsPanicSent || (DateTime.Now - session.LastPanicTime).TotalMinutes >= 15)
                                {
                                    session.IsPanicSent = true;
                                    session.LastPanicTime = DateTime.Now;

                                    //General.SendNotification("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " near " + address + " on " + session.LastPanicTime + "", session.UserId);

                                    //if (!string.IsNullOrEmpty(session.PanicAlert.Mobile))
                                    //    General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " near " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Mobile);

                                    //if (!string.IsNullOrEmpty(session.PanicAlert.Mobile1))
                                    //    General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " near " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Mobile1);

                                    //if (!string.IsNullOrEmpty(session.PanicAlert.Mobile2))
                                    //    General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " near " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Mobile2);

                                    List<string> cc = new List<string>();

                                    if (!string.IsNullOrEmpty(session.PanicAlert.Email1))
                                    {
                                        cc.Add(session.PanicAlert.Email1);
                                    }
                                    if (!string.IsNullOrEmpty(session.PanicAlert.Email2))
                                        cc.Add(session.PanicAlert.Email2);

                                    if (!string.IsNullOrEmpty(session.PanicAlert.Email))
                                        General.SendEmail("Panic Alert", "Warning: Panic button has been pressed on vehicle " + session.VehicleName + " near " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Email, cc);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, m_FolderPath, "exPanic.txt");
                }


                //int a = Convert.ToInt32(fieldsArray[14 + locationFlag]);

                // voltageForFuel
                // Other ALERTS
                string totalfuel = fieldsArray[14 + locationFlag];

                string gps_restartCount = "";
                string snrCount = "";
                string invalid_data = " ";


                if (fieldsArray[2].ToString() == "A")
                {
                    invalid_data = "0";
                }
                else
                {
                    invalid_data = "1";
                }
                fieldsArray[2].ToString();
                double voltageForFuel = Convert.ToDouble(totalfuel) * 12;

                string fuel = fieldsArray[15 + locationFlag];
                string tankCapacity = fieldsArray[16 + locationFlag];
                double odometer = (fieldsArray[17 + locationFlag]) == "" ? 0 : Convert.ToDouble(fieldsArray[17 + locationFlag]);


                string firmwareVersion = fieldsArray[18 + locationFlag];
                string deviceInternalVoltageBattery = fieldsArray[19 + locationFlag];    
                string gsmSignalStrength = fieldsArray[20 + locationFlag];
                string mobileCountryCode = fieldsArray[21 + locationFlag];
                string mobileNetworkCode = fieldsArray[22 + locationFlag];
                string locationAreaCode = fieldsArray[23 + locationFlag];
                //string cellIdWithSignature = fieldsArray[(i * validDataCount) + (validDataCount - 1)];
                string cellId = fieldsArray[24 + locationFlag];

                // Database extra mandatory fields
                string sysDateTime = DateTime.Now.ToString("yyy-MM-dd HH:mm:ss");
                string machineName = Environment.MachineName;
                //session.UTCOffset = 330;
                //string rfId = fieldsArray[16];
                //rfId = rfId.ToLower();
                string rfId = fieldsArray[16];

                // remove hidden control chars like STX/ETX and extra spaces
                rfId = new string(rfId.Where(c => !char.IsControl(c)).ToArray()).Trim().ToLower();

                if (!string.IsNullOrEmpty(rfId) && rfId != "0")
                {
                    double diff = (DateTime.Now - Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset)).TotalMinutes;

                    General.WriteToLogFile("diff = " + diff + " here", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                    General.WriteToLogFile(requestInfo.Body, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                    General.WriteToLogFile("inside the time checking section, route Id = " + session.RouteId, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                    if (Convert.ToDateTime(gpsTime).Year == 1)
                        gpsTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                    General.WriteToLogFile("after Convert.ToDateTime(gpsTime).Year, route Id = " + session.RouteId, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                    string userTable = session.IsAtlTrackingDevice
                           ? "atltracking.dbo.tbl_users"
                           : "newtrack.dbo.users";
                    string routemaster;
                    string student_master_backup;
                    string bs_route_students;
                    string bs_attendance;
                    string rf_punch_history;
                    string msg_supscription;
                    if (userTable == "atltracking.dbo.tbl_users")
                    {
                        routemaster = "atltracking.dbo.bs_route_master";
                        student_master_backup = "atltracking.dbo.bs_student_master_backup";
                        bs_route_students = "atltracking.dbo.bs_route_students";
                        rf_punch_history = "atltracking.dbo.rf_punch_history ";
                        bs_attendance = "atltracking.dbo.bs_attendance";
                        msg_supscription = "atltracking.dbo.bs_msg_subscription";

                    }
                    else
                    {
                        routemaster = "newtrack.dbo.bs_route_master";
                        student_master_backup = "newtrack.dbo.bs_student_master_backup";
                        bs_route_students = "newtrack.dbo.bs_route_students";
                        bs_attendance = "newtrack.dbo.bs_attendance";
                        rf_punch_history = "newtrack.dbo.rf_punch_history ";
                        msg_supscription = "newtrack.dbo.bs_msg_subscription";


                    }
                    if (session.RouteId == 0)
                    {
                        General.WriteToLogFile("inside the session.RouteId == 0", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                        TimeSpan currentRouteTime = Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).TimeOfDay;

                       

                        string query2 = @"
select top 1
    r.Id,
    r.start_time_up,
    r.end_time_up,
    u.school_lat,
    u.school_lng,
    r.route_name
from "+routemaster + @" r
inner join " + userTable + @" u
    on u.id = r.sys_user_id
where r.sys_service_id = " + session.ServiceId + @"
AND CAST('" + currentRouteTime.ToString(@"hh\:mm\:ss") + @"' AS TIME) BETWEEN
        CAST(DATEADD(MINUTE, -15, CAST(r.start_time_up AS DATETIME)) AS TIME)
        AND
        CAST(DATEADD(MINUTE, 30, CAST(r.end_time_up AS DATETIME)) AS TIME)
order by r.id desc";
//                        string query2 = @"
//select top 1
//    r.Id,
//    r.start_time_up,
//    r.end_time_up,
//    u.school_lat,
//    u.school_lng,
//    r.route_name
//from newtrack.dbo.bs_route_master r
//inner join " + userTable + @" u
//    on u.id = r.sys_user_id
//where r.sys_service_id = " + session.ServiceId + @"
//and '" + currentRouteTime.ToString(@"hh\:mm\:ss") + @"' between r.start_time_up and r.end_time_up
//order by r.id desc";

                        General.WriteToLogFile("Route Query = " + query2 + " service_id = " + session.ServiceId, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                        DataTable dtRoute = General.SelectQuery(query2);

                        General.WriteToLogFile(
                            "Route Query Result Rows = " + (dtRoute == null ? "NULL" : dtRoute.Rows.Count.ToString()),
                            AppDomain.CurrentDomain.BaseDirectory,
                            session.CurrentCommand + ".txt"
                        );

                        if (dtRoute != null && dtRoute.Rows.Count > 0)
                        {
                            session.RouteId = Convert.ToInt32(dtRoute.Rows[0]["Id"]);
                            session.RouteName = Convert.ToString(dtRoute.Rows[0]["route_name"]);
                            session.RouteStartTime = TimeSpan.Parse(Convert.ToString(dtRoute.Rows[0]["start_time_up"]));
                            session.RouteEndTime = TimeSpan.Parse(Convert.ToString(dtRoute.Rows[0]["end_time_up"]));
                            session.SchoolLat = Convert.ToDouble(dtRoute.Rows[0]["school_lat"]);
                            session.SchoolLng = Convert.ToDouble(dtRoute.Rows[0]["school_lng"]);

                            General.WriteToLogFile("RouteId = " + session.RouteId + ", RouteName = " + session.RouteName, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                        }

                        if (session.RouteId != 0)
                        {
                            string query1 = @"
select 
    s.id,
    s.student_name,
    s.rf_id,
    s.mobile_no1,
    ms.pick_B,
    ms.pick_D,
    ms.drop_B,
    ms.drop_D,
    rs.route_id,
    ms.id as msg_id
from "+ student_master_backup + @" s
inner join "+ bs_route_students + @" rs
    on rs.student_id = s.Id
inner join "+msg_supscription+@" ms
    on s.id = ms.student_id
where rs.route_id = " + session.RouteId;

                            General.WriteToLogFile("Student Query = " + query1, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                            DataTable dtRF = General.SelectQuery(query1);

                            General.WriteToLogFile(
                                "Student Query Result Rows = " + (dtRF == null ? "NULL" : dtRF.Rows.Count.ToString()),
                                AppDomain.CurrentDomain.BaseDirectory,
                                session.CurrentCommand + ".txt"
                            );

                            if (dtRF != null && dtRF.Rows.Count > 0)
                            {
                                if (session.StudentList == null)
                                    session.StudentList = new List<Student>();

                                session.StudentList.Clear();

                                foreach (DataRow dr in dtRF.Rows)
                                {
                                    Student student = new Student();

                                    student.id = Convert.ToInt32(dr["id"]);
                                    student.RouteId = Convert.ToInt32(dr["route_id"]);
                                    student.RFID = Convert.ToString(dr["rf_id"]).Trim().ToLower();
                                    student.MobileNo = Convert.ToString(dr["mobile_no1"]).Trim();
                                    student.pick_B = Convert.ToInt32(dr["pick_B"]);
                                    student.pick_D = Convert.ToInt32(dr["pick_D"]);
                                    student.drop_B = Convert.ToInt32(dr["drop_B"]);
                                    student.drop_D = Convert.ToInt32(dr["drop_D"]);
                                    student.msg_id = Convert.ToInt32(dr["msg_id"]);
                                    student.student_name = Convert.ToString(dr["student_name"]);

                                    session.StudentList.Add(student);

                                    General.WriteToLogFile(
                                        "Student Loaded => RFID = " + student.RFID + ", Mobile = " + student.MobileNo + ", Name = " + student.student_name,
                                        AppDomain.CurrentDomain.BaseDirectory,
                                        session.CurrentCommand + ".txt"
                                    );
                                }
                            }
                        }
                    }

                    string message = @"NA";
                    string queryAttn = "";
                    bool is_msg_send = false;
                    string msg_query = "";

                    try
                    {
                        General.WriteToLogFile("RFID Processing RouteId = " + session.RouteId, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                        if (session.StudentList == null)
                        {
                            session.StudentList = new List<Student>();
                            General.WriteToLogFile("StudentList was null. Initialized empty list.", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                        }

                        Student matchedStudent = session.StudentList.FirstOrDefault(m1 => m1.RFID == rfId);

                        General.WriteToLogFile(
                            "RFID Match Debug => RFID = " + rfId +
                            ", Matched = " + (matchedStudent == null ? "NO" : "YES") +
                            ", Student = " + (matchedStudent == null ? "NULL" : matchedStudent.student_name) +
                            ", RouteName = " + session.RouteName +
                            ", IsAtlTrackingDevice = " + session.IsAtlTrackingDevice,
                            AppDomain.CurrentDomain.BaseDirectory,
                            session.CurrentCommand + ".txt"
                        );

                        if (latitude != 0 && longitude != 0)
                        {
                            try
                            {
                                General.WriteToLogFile("Route Name = " + session.RouteName, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                TimeSpan currentRouteTime = Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).TimeOfDay;
                                if (!string.IsNullOrEmpty(session.RouteName) &&
    matchedStudent != null &&
    session.RouteStartTime <= currentRouteTime &&
    currentRouteTime <= session.RouteEndTime)
                                {
                                    double distanceFromSchoolMeter = General.HaversineInKM(session.SchoolLat, session.SchoolLng, latitude, longitude) * 1000;

                                    General.WriteToLogFile(
                                        "Distance From School Meter = " + distanceFromSchoolMeter,
                                        AppDomain.CurrentDomain.BaseDirectory,
                                        session.CurrentCommand + ".txt"
                                    );

                                    if (session.RouteName.ToLower().Contains("pick"))
                                    {
                                        is_msg_send = false;

                                        if (distanceFromSchoolMeter <= 200)
                                        {
                                            if (session.AttendanceSubscription == null || session.AttendanceSubscription.Contains(PICK_DEBOARD))
                                            {
                                                if (matchedStudent.pick_D == 1)
                                                {
                                                    is_msg_send = true;
                                                    General.WriteToLogFile("Pick Deboard already sent.", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                                }

                                                message = @"Dear parent, your ward " + matchedStudent.student_name + " deboarded from the Bus of Route " + session.RouteName + " at " + Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("hh:mm tt");

                                                queryAttn = @"insert into " +bs_attendance+@" (rf_id,date_,gps_time,attendance_type_id,route_id) 
values ('" + rfId + "','" + DateTime.Today.ToString("yyyy-MM-dd") + "','" + gpsTime + "'," + PICK_DEBOARD + "," + session.RouteId + ")";

                                                msg_query = "update "+ msg_supscription+" set pick_D = 1 where id = " + matchedStudent.msg_id;

                                                matchedStudent.pick_D = 1;
                                            }
                                        }
                                        else
                                        {
                                            if (session.AttendanceSubscription == null || session.AttendanceSubscription.Contains(PICK_BOARD))
                                            {
                                                if (matchedStudent.pick_B == 1)
                                                {
                                                    is_msg_send = true;
                                                    General.WriteToLogFile("Pick Board already sent.", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                                }

                                                message = @"Dear parent, your ward " + matchedStudent.student_name + " boarded from the Bus of Route " + session.RouteName + " at " + Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("hh:mm tt");

                                                queryAttn = @"insert into " +bs_attendance+@" (rf_id,date_,gps_time,attendance_type_id,route_id) 
values ('" + rfId + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + gpsTime + "'," + PICK_BOARD + "," + session.RouteId + ")";

                                                msg_query = "update " + msg_supscription+" set pick_B = 1 where id = " + matchedStudent.msg_id;

                                                matchedStudent.pick_B = 1;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        is_msg_send = false;

                                        if (distanceFromSchoolMeter <= 200)
                                        {
                                            if (session.AttendanceSubscription == null || session.AttendanceSubscription.Contains(DROP_BOARD))
                                            {
                                                if (matchedStudent.drop_B == 1)
                                                {
                                                    is_msg_send = true;
                                                    General.WriteToLogFile("Drop Board already sent.", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                                }

                                                message = @"Dear parent, your ward " + matchedStudent.student_name + " boarded from the Bus of Route " + session.RouteName + " at " + Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("hh:mm tt");

                                                queryAttn = @"insert into " +bs_attendance+@" (rf_id,date_,gps_time,attendance_type_id,route_id) 
values ('" + rfId + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + gpsTime + "'," + DROP_BOARD + "," + session.RouteId + ")";

                                                msg_query = "update " + msg_supscription +" set drop_B = 1 where id = " + matchedStudent.msg_id;

                                                matchedStudent.drop_B = 1;
                                            }
                                        }
                                        else
                                        {
                                            if (session.AttendanceSubscription == null || session.AttendanceSubscription.Contains(DROP_DEBOARD))
                                            {
                                                if (matchedStudent.drop_D == 1)
                                                {
                                                    is_msg_send = true;
                                                    General.WriteToLogFile("Drop Deboard already sent.", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                                }

                                                message = @"Dear parent, your ward " + matchedStudent.student_name + " deboarded from the Bus of Route " + session.RouteName + " at " + Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("hh:mm tt");

                                                queryAttn = @"insert into " +bs_attendance+@"(rf_id,date_,gps_time,attendance_type_id,route_id) 
values ('" + rfId + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + gpsTime + "'," + DROP_DEBOARD + "," + session.RouteId + ")";

                                                msg_query = "update "+ msg_supscription+ " set drop_D = 1 where id = " + matchedStudent.msg_id;

                                                matchedStudent.drop_D = 1;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                General.WriteToLogFile(ex.ToString() + " In RFID route decision exception", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                            }
                        }

                        General.WriteToLogFile("Message before fallback = " + message, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                        if (message == "NA")
                        {
                            General.WriteToLogFile("Message still NA, fallback notification will be used.", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                            if (matchedStudent != null)
                            {
                                message = @"Dear parent, your ward " + matchedStudent.student_name + " just punched his/her RF card. " + Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("hh:mm tt");
                            }
                            else
                            {
                                message = @"Dear parent, your ward just punched his/her RF card. " + Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("hh:mm tt");
                            }
                        }

                        if (matchedStudent != null)
                        {
                            General.WriteToLogFile("is_msg_send = " + is_msg_send, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                            if (!is_msg_send)
                            {
                                if (!string.IsNullOrEmpty(msg_query))
                                {
                                    General.WriteToLogFile("Message Subscription Update Query = " + msg_query, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                    gen.DML(msg_query);
                                }

                                if (!string.IsNullOrEmpty(queryAttn))
                                {
                                    General.WriteToLogFile("Attendance Insert Query = " + queryAttn, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                    //gen.DML(queryAttn);
                                }

                                try
                                {
                                    IEnumerable<DataRow> n = Enumerable.Empty<DataRow>();

                                    if (session.DtMobileIds != null)
                                    {
                                        n = session.DtMobileIds.AsEnumerable()
                                            .Where(x => x.Field<string>("bs_user_name") == matchedStudent.MobileNo);
                                    }

                                    General.WriteToLogFile(
                                        "Mobile Token Match Count = " + n.Count() + ", Mobile = " + matchedStudent.MobileNo,
                                        AppDomain.CurrentDomain.BaseDirectory,
                                        session.CurrentCommand + ".txt"
                                    );

                                    if (n.Count() > 0)
                                    {
                                        string auid = n.First().Field<string>("auid");
                                        string iuid = n.First().Field<string>("iuid");

                                        General.WriteToLogFile(
                                            "Before Notification => Mobile = " + matchedStudent.MobileNo + ", Message = " + message + ", AUID = " + auid + ", IUID = " + iuid,
                                            AppDomain.CurrentDomain.BaseDirectory,
                                            session.CurrentCommand + ".txt"
                                        );

                                        await General.SendNotification(matchedStudent.MobileNo, message, gen,matchedStudent.RouteId,matchedStudent.id,session);
                                    }
                                    else
                                    {
                                        General.WriteToLogFile(
                                            "No FCM token mapping found in session.DtMobileIds for mobile = " + matchedStudent.MobileNo,
                                            AppDomain.CurrentDomain.BaseDirectory,
                                            session.CurrentCommand + ".txt"
                                        );

                                        General.SendNotification_dd(matchedStudent.MobileNo, message, gen,matchedStudent.RouteId,matchedStudent.id,session);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    General.WriteToLogFile(ex.Message + " Notification exception", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                }
                            }
                        }
                        else
                        {
                            DataTable dtRF = General.SelectQuery(@"select mobile_no1,id from " +student_master_backup+@" where rf_id='" + rfId + "'");

                            if (dtRF != null && dtRF.Rows.Count > 0)
                            {
                                if (!is_msg_send)
                                {
                                    string mn = Convert.ToString(dtRF.Rows[0][0]);
                                    int id = Convert.ToInt32(dtRF.Rows[0][1]);

                                    gen.SendSms(message + " Download the app and track bus: https://goo.gl/akxDX3", mn, session.SMSAPI, session.sender_id, session.service_from, id, session, gen);

                                    try
                                    {
                                        IEnumerable<DataRow> n = Enumerable.Empty<DataRow>();

                                        if (session.DtMobileIds != null)
                                        {
                                            n = session.DtMobileIds.AsEnumerable()
                                                .Where(x => x.Field<string>("bs_user_name") == mn);
                                        }

                                        if (n.Count() > 0)
                                        {
                                            string auid = n.First().Field<string>("auid");
                                            string iuid = n.First().Field<string>("iuid");
                                            int route_id = session.RouteId;
                                            if (!string.IsNullOrEmpty(auid))
                                            {
                                                await General.SendNotification(mn, message, gen,route_id, id,session);
                                            }

                                            if (!string.IsNullOrEmpty(iuid) && iuid != "(null)")
                                            {
                                                await General.SendNotification(mn, message, gen, route_id, id,session);
                                            }

                                            if (string.IsNullOrEmpty(auid) && string.IsNullOrEmpty(iuid))
                                            {
                                                General.SendNotification_dd(mn, message, gen, route_id, id,session);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        General.WriteToLogFile(ex.Message, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                                    }
                                }
                            }
                        }

                        if (!is_msg_send)
                        {
                            int student_id = 0;

                            try
                            {
                                if (matchedStudent != null)
                                    student_id = matchedStudent.id;
                            }
                            catch (Exception ex)
                            {
                                General.WriteToLogFile(ex.Message + " student id exception", AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");
                            }

                            if (student_id != 0)
                            {
                                gen.DML(@"insert into " + rf_punch_history + @" 
(rfid, sys_service_id, punch_gps_time, gps_latitude, gps_longitude, student_id, sys_proc_time) 
values 
('" + rfId + "', " + session.ServiceId + ", '" + gpsTime + "', '" + latitude + "', '" + longitude + "', " + student_id + ", '" + sysProcTime + "')");
//                                gen.DML(@"insert into " +rf_punch_history+ @" 
//(rfid,sys_service_id,punch_gps_time,gps_latitude,gps_longitude,student_id) 
//values 
//('" + rfId + "'," + session.ServiceId + ",'" + gpsTime + "','" + latitude + "','" + longitude + "'," + student_id + ")");

                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(session.ServiceId))
                                {
                                    //                                    gen.DML(@"insert into " +rf_punch_history+ @" 
                                    //(rfid,sys_service_id,punch_gps_time,gps_latitude,gps_longitude,student_id, sys_proc_time) 
                                    //values 
                                    //('" + rfId + "'," + session.ServiceId + ",'" + gpsTime + "','" + latitude + "','" + longitude + "',(select top 1 id from " +student_master_backup +" where rf_id ='" + rfId + "'))");
                                   
                                    gen.DML(@"insert into " + rf_punch_history + @" 
(rfid, sys_service_id, punch_gps_time, gps_latitude, gps_longitude, student_id, sys_proc_time) 
values 
('" + rfId + "', " + session.ServiceId + ", '" + gpsTime + "', '" + latitude + "', '" + longitude + "', (select top 1 id from " + student_master_backup + @" where rf_id = '" + rfId + "'),'" + sysProcTime + "')");
                                }
                                else
                                {
                                    //                                    gen.DML(@"insert into " +rf_punch_history+@"  
                                    //(rfid,sys_service_id,punch_gps_time,gps_latitude,gps_longitude,student_id) 
                                    //values 
                                    //('" + rfId + "',(select top 1 rm.sys_service_id from " +routemaster+@"  rm,  " +student_master_backup +@"  s,  " +bs_route_students+@"  rs where rs.route_id=rm.id and s.id = rs.student_id and s.rf_id ='" + rfId + "'),'" + gpsTime + "','" + latitude + "','" + longitude + "',(select top 1 id from " +student_master_backup +" where rf_id ='" + rfId + "'))");
                                    gen.DML(@"insert into " + rf_punch_history + @"  
(rfid, sys_service_id, punch_gps_time, gps_latitude, gps_longitude, student_id, sys_proc_time) 
values 
('" + rfId + @"',
(select top 1 rm.sys_service_id 
 from " + routemaster + @" rm, " + student_master_backup + @" s, " + bs_route_students + @" rs 
 where rs.route_id = rm.id 
 and s.id = rs.student_id 
 and s.rf_id = '" + rfId + @"'),
'" + gpsTime + @"',
'" + latitude + @"',
'" + longitude + @"',
(select top 1 id 
 from " + student_master_backup + @" 
 where rf_id = '" + rfId + @"'),
'" + sysProcTime + @"')");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        General.WriteToLogFile(ex.Message, AppDomain.CurrentDomain.BaseDirectory, session.CurrentCommand + ".txt");

                        //                        gen.DML(@"insert into  " +rf_punch_history+@"  
                        //(rfid,sys_service_id,punch_gps_time,gps_latitude,gps_longitude) 
                        //values 
                        //('" + rfId + "'," + session.ServiceId + ",'" + gpsTime + "','" + latitude + "','" + longitude + "')");
                        gen.DML(@"insert into " + rf_punch_history + @"  
(rfid, sys_service_id, punch_gps_time, gps_latitude, gps_longitude, sys_proc_time) 
values 
('" + rfId + "', " + session.ServiceId + ", '" + gpsTime + "', '" + latitude + "', '" + longitude + "', '" + sysProcTime + "')");
                    }
                }


                //string tableMonth = Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("MMMyy").ToLower();

                //    //session.LastGPSTime = Convert.ToDateTime(gpsTime);

                //    string query = "insert into telemetry_" + tableMonth + "(signature,sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude," +
                //             "longitude_direction,gps_speed,gps_orientation,variation,checksum,I1," +
                //             "I2,I3,I4,I5,I6,I7,I8,I9," +
                //             "I10,I11,I12,I13,I14,I15," +
                //             "serial1_voltage,tel_fuel,tank_capacity,tel_odometer,tel_temperature,battery_voltage," +
                //             "signal_strength,mobile_country_code,mobile_network_code,location_area_code,cell_id," +
                //             "sys_msg_type,sys_proc_host,data_status,vehicle_status,firmware_version,visible_satellite,altitude,address_from_device)" +
                //             "values(" +
                //             "'ATL_NEW','" + session.ServiceId + "','" + sysDateTime + "','" + gpsTime + "','" + gpsValidity + "','" + latitude + "','" + latitudeDirection + "','" + longitude + "','" +
                //             longitudeDirection + "','" + gpsSpeed + "','" + gpsOrientation_trueCourse + "','" + variation + "','" + checksum + "','" + mainPower_i1 + "','" +
                //             ignition_i2 + "','" + sos_i3 + "','" + i4 + "','" + i5 + "','0','" + i7 + "','" + door_i8 + "','" + harshSpeeding_i9 + "','" +
                //             harshBreaking_i10 + "','" + armDisarm_i11 + "','" + sleepStatus_i12 + "','" + generalRelayStatus_i13 + "','" + accelerometerStatus_i14 + "','" + ac_i6 + "','" +
                //             voltageForFuel + "','" + fuel + "','" + tankCapacity + "','" + odometer + "','" + fuel + "','" + deviceInternalVoltageBattery + "','" +
                //             gsmSignalStrength + "','" + mobileCountryCode + "','" + mobileNetworkCode + "','" + locationAreaCode + "','" + cellId + "','" +
                //             "1','" + Environment.MachineName + "','','','" + firmwareVersion + "','0','','" + location + "')";


                string tableMonth = Convert.ToDateTime(gpsTime)
    .AddMinutes(session.UTCOffset)
    .ToString("MMMyy")
    .ToLower();

                string telemetryTable = session.IsAtlTrackingDevice
                    ? "atltracking.dbo.tbl_telemetry_" + tableMonth
                    : "newtrack.dbo.telemetry_" + tableMonth;

                string query = "";

                if (session.IsAtlTrackingDevice)
                {
                    query = "insert into " + telemetryTable + "(" +
                            "sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude," +
                            "longitude_direction,gps_speed,gps_orientation,variation,checksum,I1," +
                            "I2,I3,I4,I5,I6,I7,I8,I9," +
                            "I10,I11,I12,I13,I14,I15," +
                            "serial1_voltage,tel_fuel,tank_capacity,tel_odometer,tel_temperature,battery_voltage," +
                            "signal_strength,mobile_country_code,mobile_network_code,location_area_code,cell_id," +
                            "sys_msg_type,firmware_version,visible_satellite,altitude,address_from_device)" +
                            "values(" +
                            "'" + session.ServiceId + "','" + sysDateTime + "','" + gpsTime + "','" + gpsValidity + "','" + latitude + "','" + latitudeDirection + "','" + longitude + "','" +
                            longitudeDirection + "','" + gpsSpeed + "','" + gpsOrientation_trueCourse + "','" + variation + "','" + checksum + "','" + mainPower_i1 + "','" +
                            ignition_i2 + "','" + sos_i3 + "','" + i4 + "','" + i5 + "','0','" + i7 + "','" + door_i8 + "','" + harshSpeeding_i9 + "','" +
                            harshBreaking_i10 + "','" + armDisarm_i11 + "','" + sleepStatus_i12 + "','" + generalRelayStatus_i13 + "','" + accelerometerStatus_i14 + "','" + ac_i6 + "','" +
                            voltageForFuel + "','" + fuel + "','" + tankCapacity + "','" + odometer + "','" + fuel + "','" + deviceInternalVoltageBattery + "','" +
                            gsmSignalStrength + "','" + mobileCountryCode + "','" + mobileNetworkCode + "','" + locationAreaCode + "','" + cellId + "','" +
                            "1','" + firmwareVersion + "','0','','" + location + "')";
                }
                else
                {
                    query = "insert into " + telemetryTable + "(signature,sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude," +
                            "longitude_direction,gps_speed,gps_orientation,variation,checksum,I1," +
                            "I2,I3,I4,I5,I6,I7,I8,I9," +
                            "I10,I11,I12,I13,I14,I15," +
                            "serial1_voltage,tel_fuel,tank_capacity,tel_odometer,tel_temperature,battery_voltage," +
                            "signal_strength,mobile_country_code,mobile_network_code,location_area_code,cell_id," +
                            "sys_msg_type,sys_proc_host,data_status,vehicle_status,firmware_version,visible_satellite,altitude,address_from_device)" +
                            "values(" +
                            "'ATL_NEW','" + session.ServiceId + "','" + sysDateTime + "','" + gpsTime + "','" + gpsValidity + "','" + latitude + "','" + latitudeDirection + "','" + longitude + "','" +
                            longitudeDirection + "','" + gpsSpeed + "','" + gpsOrientation_trueCourse + "','" + variation + "','" + checksum + "','" + mainPower_i1 + "','" +
                            ignition_i2 + "','" + sos_i3 + "','" + i4 + "','" + i5 + "','0','" + i7 + "','" + door_i8 + "','" + harshSpeeding_i9 + "','" +
                            harshBreaking_i10 + "','" + armDisarm_i11 + "','" + sleepStatus_i12 + "','" + generalRelayStatus_i13 + "','" + accelerometerStatus_i14 + "','" + ac_i6 + "','" +
                            voltageForFuel + "','" + fuel + "','" + tankCapacity + "','" + odometer + "','" + fuel + "','" + deviceInternalVoltageBattery + "','" +
                            gsmSignalStrength + "','" + mobileCountryCode + "','" + mobileNetworkCode + "','" + locationAreaCode + "','" + cellId + "','" +
                            "1','" + Environment.MachineName + "','','','" + firmwareVersion + "','0','','" + location + "')";
                }




                string querypg = "insert into dbo.tbl_device_off_detail(signature,sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude," +
                             "longitude_direction,gps_speed,gps_orientation,variation,checksum,I1," +
                             "I2,I3,I4,I5,I6,I7,I8,I9," +
                             "I10,I11,I12,I13,I14," +
                             "serial1_voltage,tel_fuel,tank_capacity,tel_odometer,tel_temperature,battery_voltage," +
                             "signal_strength,mobile_country_code,mobile_network_code,location_area_code,cell_id," +
                             "sys_msg_type,sys_proc_host,data_status,vehicle_status,firmware_version,visible_satellite,altitude,gps_restartCount,snr_count,session_id,invalid_protocol)" +
                             "values(" +
                             "'ATL_NEW','" + requestInfo.Key + "','" + sysDateTime + "','" + gpsTime + "','" + gpsValidity + "','" + latitude + "','" + latitudeDirection + "','" + longitude + "','" +
                             longitudeDirection + "','" + gpsSpeed + "','" + gpsOrientation_trueCourse + "','" + variation + "','" + checksum + "','" + mainPower_i1 + "','" +
                             ignition_i2 + "','" + sos_i3 + "','" + i4 + "','" + i5 + "','" + accelerometerStatus_i14 + "','" + i7 + "','" + door_i8 + "','" + harshSpeeding_i9 + "','" +
                             harshBreaking_i10 + "','" + armDisarm_i11 + "','" + sleepStatus_i12 + "','" + generalRelayStatus_i13 + "','" + ac_i6 + "','" +
                             voltageForFuel + "','" + fuel + "','" + tankCapacity + "','" + odometer + "','0','" + deviceInternalVoltageBattery + "','" +
                             gsmSignalStrength + "','" + mobileCountryCode + "','" + mobileNetworkCode + "','" + locationAreaCode + "','" + cellId + "','" +
                             "1','" + Environment.MachineName + "','','','" + firmwareVersion + "','0','','" + gps_restartCount + "','" + snrCount + "','" + session.SessionID + "','" + invalid_data + "')";

                try
                {
                    gen.DML(query);

                    General.WriteToLogFile(
                        "Telemetry Insert Success => IMEI=" + session.IMEI +
                        ", ServiceId=" + session.ServiceId +
                        ", DB=" + (session.IsAtlTrackingDevice ? "atltracking" : "newtrack") +
                        ", Table=" + telemetryTable,
                        m_FolderPath,
                        "telemetry_db_mapping_log.txt"
                    );
                }
                catch (Exception ex)
                {
                    General.WriteToLogFile(
                        "Telemetry Insert Failed => IMEI=" + session.IMEI +
                        ", ServiceId=" + session.ServiceId +
                        ", DB=" + (session.IsAtlTrackingDevice ? "atltracking" : "newtrack") +
                        ", Table=" + telemetryTable +
                        Environment.NewLine + ex.Message +
                        Environment.NewLine + requestInfo.Body,
                        m_FolderPath,
                        "sqlservererrorwithpacket"
                    );

                    General.WriteToLogFile(ex.Message + Environment.NewLine, m_FolderPath, "sqlservererror");
                }


                try
                    {


                        if (locationFlag == 1)
                        {
                            if (session.PrevLatitude != latitude.ToString() && session.PrevLongitude != longitude.ToString())
                            {
                                if (!string.IsNullOrEmpty(location.Trim()) && !location.Trim().ToLower().Contains("not found"))
                                {

                                    //MongoServer server = MongoServer.Create(new MongoConnectionStringBuilder("Server=localhost:27017"));
                                    //server.Connect();
                                    //var database = server.GetDatabase("newtrack");
                                    //var collection = database.GetCollection<BsonDocument>("tbl_geodata");

                                    //var document = new BsonDocument 

                                    //{

                                    //     { "geo_street",location},
                                    //    {"location", new BsonArray(new[] {longitude,latitude})}

                                    //};
                                    //collection.Insert(document);

                                    //Mysql //General.DML("INSERT INTO tbl_geodata (geo_street,gps_latitude,gps_longitude) VALUES ('" + location + "','" + latitude + "','" + longitude + "')", true);

                                    //  if (!string.IsNullOrEmpty(m_userName))
                                    // General.DML("INSERT INTO tbl_geodata (geo_street,gps_latitude,gps_longitude) VALUES ('" + location + "'," + latitude + "," + longitude + ")", m_userName);
                                    //  else
                                    // General.DML("INSERT INTO tbl_geodata (geo_street,gps_latitude,gps_longitude) VALUES ('" + location + "'," + latitude + "," + longitude + ")");
                                }

                                session.PrevLatitude = latitude.ToString();
                                session.PrevLongitude = longitude.ToString();
                            }


                        }
                    }

                    catch (Exception ex)
                    {



                        General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, m_FolderPath, "exDb.txt");
                        General.WriteToLogFile(ex.Message + Environment.NewLine, m_FolderPath, "exDbpost.txt");
                    }



                }



            try
            {
                string subject = "Imei=" + session.IMEI + "";
                string ddate = DateTime.Now.ToString("ddMMyyyy");
                string check = "select * from deviceoff where imei =" + session.IMEI;

                dt = General.SelectQuery(check);

                if (dt == null || dt.Rows.Count == 0)
                {
                    General.WriteToLogFile("No deviceoff row found for IMEI = " + session.IMEI, m_FolderPath, "deviceoffexception.txt");
                    return;
                }

                int counti = Convert.ToInt32(dt.Rows[0][4].ToString());

                string sds = "AtlantaNew_1299_" + ddate + "_ReceivedData_" + session.IMEI + counti;
                string msg23 = "Device(" + session.IMEI + ") is now online.\n Link : http://45.113.189.20:8080/rawdebuger/rawdata/details/" + sds;

                if (dt.Rows[0][3].ToString().Trim() == "0")
                {
                    General.WriteToLogFile("imei =" + session.IMEI + Environment.NewLine, m_FolderPath, "notificationcode.txt");

                    string qryww = "update deviceoff set val = 1 where imei =" + session.IMEI;
                    gen.DML(qryww);

                    General.SendEmail1(subject, "metacoder1222@gmail.com", "pswd", msg23, "vipin@atlantasys.com");
                }

                string check2 = dt.Rows[0][3].ToString().Trim();

                if (check2 == "1" || check2 == "0")
                {
                    General.WriteToLogFile("imei =" + session.IMEI + "check2 = " + check2 + Environment.NewLine, m_FolderPath, "notificationcode.txt");

                    if (requestInfo.Parameters.Length <= 2)
                    {
                        General.WriteToLogFile(requestInfo.Body, m_FolderPath2, "ReceivedData\\" + requestInfo.Key + counti + ".txt");
                        counti++;

                        string qry = "update deviceoff set counti =" + counti + " where imei =" + session.IMEI;
                        string qry_f = "update deviceoff set val = 2 where imei =" + session.IMEI;

                        gen.DML(qry);
                        gen.DML(qry_f);
                    }

                    if (requestInfo.Parameters.Length > 2)
                    {
                        General.WriteToLogFile(requestInfo.Body, m_FolderPath2, "ReceivedData\\" + requestInfo.Key + counti + ".txt");
                    }
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message + Environment.NewLine, m_FolderPath, "deviceoffexception.txt");
            }


        }

        private void insertResponse(string response, CustomSession session,General gen)
        {
            General.WriteToLogFile("RESPONSE FROM DEVICE: '" + response + "'", m_FolderPath, "ReceivedData\\" + session.IMEI + ".txt");
            try
            {
                if (session.command_id != 0)
                {

                    string query = "update send_command_log set response = '" + response + "' , response_time =getdate(),is_pending =0 where id =" + session.command_id;
                    gen.DML(query);
                    session.command_id = 0;

                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, m_FolderPath, "cmd_err.txt");

            }
        }
       


       
        private void CheckCommand(string service_id, CustomSession session,General gen,string imei)
        {
            try
            {
                DataTable dt = General.SelectQuery("select top 1 command ,id from send_command_log where sys_service_id = " + service_id + " and is_pending =1");

                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {

                        string command = Convert.ToString(dt.Rows[0]["command"]).Trim();
                        int id = Convert.ToInt32(dt.Rows[0]["id"]);
                        session.command_id = id;
                        session.Send(command);
                        //Thread.Sleep(1000);
                        General.WriteToLogFile(command, m_FolderPath, "ReceivedData\\" + imei + ".txt");
                        gen.DML("update send_command_log set send_time = getdate(),is_pending=0 where id=" + id);
                        //General.DML("update send_command_log set is_pending =0 where id=" + id);
                    }
                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, m_FolderPath, "cmd_err.txt");

            }
        }

        private void sendVersionCommand(StringRequestInfo requestInfo, CustomSession session, General gen)
        {
            try
            {
                if ((requestInfo.Key.StartsWith("8")) && (!requestInfo.Key.Contains(',')) && (!requestInfo.Key.Contains('.')))
                {
                    string command = $"$MSG,VERSION<6906>&";
                    string commandForCheckingDualIpEnable = $"$MSG,SYSSTATUS<6906>&";

                    string selectquery = $"select top 1 id,imei,is_pending,dualipenableresponse from find_version_ip where imei = '{requestInfo.Key}' order by id desc";
                    DataTable dt = General.SelectQuery(selectquery);
                    if (dt != null)
                    {
                        if (dt.Rows.Count == 0)
                        {
                            session.Send(command);
                            General.WriteToLogFile($"Send Command : {command}", m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");
                            StringBuilder insertquery = new StringBuilder();
                            insertquery.Append($"insert into find_version_ip ");
                            insertquery.Append($"(imei,command,is_pending,send_command_date,port) ");
                            insertquery.Append($"values ");
                            insertquery.Append($"('{requestInfo.Key}','{command}',0,getdate(),16214)");

                            gen.DML(insertquery.ToString());

                        }

                        else if (dt.Rows.Count == 1)
                        {
                            int id = Convert.ToInt32(dt.Rows[0][0].ToString());
                            if (dt.Rows[0][2].ToString() == "0")
                            {
                                session.Send(command);
                                General.WriteToLogFile($"Send Command : {command}", m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");
                                StringBuilder updatequery = new StringBuilder();
                                updatequery.Append($"update find_version_ip ");
                                updatequery.Append($"set ");
                                updatequery.Append($"send_command_date = getdate() ");
                                updatequery.Append($"where id = {id}");

                                gen.DML(updatequery.ToString());

                            }
                            else if (dt.Rows[0][2].ToString() == "1")
                            {
                                session.Send(commandForCheckingDualIpEnable);
                                General.WriteToLogFile($"Send Command : {commandForCheckingDualIpEnable}", m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");
                                StringBuilder updatequery = new StringBuilder();
                                updatequery.Append($"update find_version_ip ");
                                updatequery.Append($"set ");
                                updatequery.Append($"send_command_date = getdate(),dualipenablecommand='{commandForCheckingDualIpEnable}'");
                                updatequery.Append($"where id = {id}");

                                gen.DML(updatequery.ToString());

                            }

                            else if (dt.Rows[0][2].ToString() == "2")
                            {
                                if (dt.Rows[0][3].ToString().Contains("DIPOFF"))
                                {
                                    string commandShiftIPtoDomain = "$MSG,#SERVERCHANGE::abctraq.com::16214;<6906>&";
                                    session.Send(commandShiftIPtoDomain);
                                    General.WriteToLogFile($"Send Command : {commandShiftIPtoDomain}", m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");

                                    StringBuilder updatequery = new StringBuilder();
                                    updatequery.Append($"update find_version_ip ");
                                    updatequery.Append($"set ");
                                    updatequery.Append($"send_command_date = getdate(),sendiptodomaincommand='{commandShiftIPtoDomain}'");
                                    updatequery.Append($"where id = {id}");

                                    gen.DML(updatequery.ToString());

                                }
                                else if ((dt.Rows[0][3].ToString().Contains("DIPON")) && ((!(dt.Rows[0][3].ToString().Contains("abctraq.com"))) || (!(dt.Rows[0][3].ToString().Contains("ABCTRAQ.COM"))) || (!(dt.Rows[0][3].ToString().Contains("45.113.189.20")))))
                                {
                                    string commandShiftIPtoDomain = "$MSG,#SERVERCHANGE::abctraq.com::16214;<6906>&";
                                    session.Send(commandShiftIPtoDomain);
                                    General.WriteToLogFile($"Send Command : {commandShiftIPtoDomain}", m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");

                                    StringBuilder updatequery = new StringBuilder();
                                    updatequery.Append($"update find_version_ip ");
                                    updatequery.Append($"set ");
                                    updatequery.Append($"send_command_date = getdate(),sendiptodomaincommand='{commandShiftIPtoDomain}'");
                                    updatequery.Append($"where id = {id}");

                                    gen.DML(updatequery.ToString());
                                }
                                else if ((dt.Rows[0][3].ToString().Contains("DIPON")) && (dt.Rows[0][3].ToString().Contains("45.113.189.20")))
                                {
                                    string commandShiftIPtoDomain = "$MSG,#SERVERCHANGE1::abctraq.com::16214;<6906>&";
                                    session.Send(commandShiftIPtoDomain);
                                    General.WriteToLogFile($"Send Command : {commandShiftIPtoDomain}", m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");

                                    StringBuilder updatequery = new StringBuilder();
                                    updatequery.Append($"update find_version_ip ");
                                    updatequery.Append($"set ");
                                    updatequery.Append($"send_command_date = getdate(),sendiptodomaincommand='{commandShiftIPtoDomain}'");
                                    updatequery.Append($"where id = {id}");

                                    gen.DML(updatequery.ToString());
                                }
                            }

                            else if (dt.Rows[0][2].ToString() == "3")
                            {
                                string commandcheckipordomain = "$MSG,GPRSSTATUS<6906>&";
                                session.Send(commandcheckipordomain);
                                General.WriteToLogFile($"Send Command : {commandcheckipordomain}", m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");

                                StringBuilder updatequery = new StringBuilder();
                                updatequery.Append($"update find_version_ip ");
                                updatequery.Append($"set ");
                                updatequery.Append($"send_command_date = getdate(),send_command_to_check_ip_domain ='{commandcheckipordomain}'");
                                updatequery.Append($"where id = {id}");

                                gen.DML(updatequery.ToString());
                            }

                        }



                    }
                }


            }
            catch (Exception ex)
            {

            }




        }

        private void insertVersionCommand(string response, CustomSession session, General gen)
        {
            try
            {
                string Response = response;
                string[] responseSplitbycomma = Response.Split(',');
                string imei = responseSplitbycomma[0].Replace("$", "");
                string version = responseSplitbycomma[1];
                General.WriteToLogFile($"Response:{Response}", m_FolderPath, "ReceivedData\\" + imei + ".txt");

                if (!version.StartsWith("ARM"))
                {
                    string selectquery = $"select top 1 imei,id,is_pending from find_version_ip ";
                    selectquery += $"where imei = '{imei}' order by id desc";
                    DataTable dt = General.SelectQuery(selectquery);
                    if (dt != null)
                    {
                        if (dt.Rows[0][2].ToString() == "0")
                        {
                            if (!(response.Contains("INVALID")) && !(response.Contains("invalid")) && !(response.Contains("CSConnect=0")))
                            {
                                int id = Convert.ToInt32(dt.Rows[0][1].ToString());
                                StringBuilder updatequery = new StringBuilder();
                                updatequery.Append($"update find_version_ip ");
                                updatequery.Append($"set is_pending = 1,response = '{Response}',");
                                updatequery.Append($"version = '{version}',response_date=getdate() ");
                                updatequery.Append($"where imei = '{imei}' and id = {id}");

                                gen.DML(updatequery.ToString());
                            }


                        }
                        else if (dt.Rows[0][2].ToString() == "1")
                        {
                            int id = Convert.ToInt32(dt.Rows[0][1].ToString());
                            if (response.Contains("abctraq.com") || response.Contains("ABCTRAQ.COM"))
                            {
                                StringBuilder updatequery = new StringBuilder();
                                updatequery.Append($"update find_version_ip ");
                                updatequery.Append($"set is_pending = 4,dualipenableresponse = '{Response}',");
                                updatequery.Append($"response_date=getdate(),response_command_to_check_ip_domain='IP:abctraq.com' ");
                                updatequery.Append($"where imei = '{imei}' and id = {id}");

                                gen.DML(updatequery.ToString());
                            }
                            else
                            {
                                StringBuilder updatequery = new StringBuilder();
                                updatequery.Append($"update find_version_ip ");
                                updatequery.Append($"set is_pending = 2,dualipenableresponse = '{Response}',");
                                updatequery.Append($"response_date=getdate() ");
                                updatequery.Append($"where imei = '{imei}' and id = {id}");

                                gen.DML(updatequery.ToString());
                            }


                        }
                        else if (dt.Rows[0][2].ToString() == "2")
                        {
                            int id = Convert.ToInt32(dt.Rows[0][1].ToString());
                            StringBuilder updatequery = new StringBuilder();
                            updatequery.Append($"update find_version_ip ");
                            updatequery.Append($"set is_pending = 3,responseiptodomaincommand = '{Response}',");
                            updatequery.Append($"response_date=getdate() ");
                            updatequery.Append($"where imei = '{imei}' and id = {id}");

                            gen.DML(updatequery.ToString());
                        }
                        else if (dt.Rows[0][2].ToString() == "3")
                        {
                            int id = Convert.ToInt32(dt.Rows[0][1].ToString());
                            StringBuilder updatequery = new StringBuilder();
                            updatequery.Append($"UPDATE find_version_ip ");
                            updatequery.Append($"SET is_pending = 4,response_command_to_check_ip_domain = '{Response}',");
                            updatequery.Append($"response_date = GETDATE()");
                            updatequery.Append($"where imei = '{imei}' and id = {id}");



                            gen.DML(updatequery.ToString());
                        }


                    }

                }

            }
            catch (Exception ex)
            {

            }


        }
    }
}
