using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using GPSTrackerListeners.Util.Alert;
using MySql.Data.MySqlClient;
using Npgsql;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace GPSTrackerListeners.AtlantaNew
{
    public class General
    {
        public static readonly string connectionString1 = "Data Source={ip};Initial Catalog={cat};User ID=(user);Password={pass}; Max Pool Size=32767;";
        public static readonly string connectionStringAtlTracking = connectionString1.Replace("Initial Catalog={catt}", "Initial Catalog={cat}");

        public static string ServiceName = "ListenerATL_STD_RFID_16214";

        public class ServiceInfo
        {
            public string ServiceId { get; set; }
            public int UserId { get; set; }
            public int UTCOffset { get; set; }
            public string VehicleName { get; set; }
            public bool IsAtlTrackingDevice { get; set; }
        }






        public static string GetLocationFromLatLong(double latitude, double longitude)
        {
            string address = "NA";
            if (latitude != null && longitude != null)
            {
                using (WebClient wc = new WebClient())
                {
                    try
                    {
                        address = wc.DownloadString("https://fasttracksoft.us/api/get_address_mongo.php?latitude=" + latitude + "&longitude=" + longitude);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            return address;
        }

        public  void DML(string query)
        {

            SqlConnection connection = null;
            try
            {
                using ( connection = new SqlConnection(connectionString1))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {

                        connection.Open();
                        //cmd.CommandTimeout = 100000;
                        cmd.ExecuteNonQuery();

                    }
                }

            }
            catch (Exception ex)
            {
                connection.Close();
            }
            finally
            {
                connection.Close();
            }



        }

        public void DMLByDeviceDatabase(string query, bool isAtlTrackingDevice)
        {
            string connString = isAtlTrackingDevice ? connectionStringAtlTracking : connectionString1;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    connection.Open();
                    cmd.CommandTimeout = 100000;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static string GetTelemetryTableName(bool isAtlTrackingDevice, string tableMonth)
        {
            if (isAtlTrackingDevice)
                return "atltracking.dbo.tbl_telemetry_" + tableMonth;

            return "newtrack.dbo.telemetry_" + tableMonth;
        }

        public static void DMLpostgres(string query)
        {


             using (NpgsqlConnection connection = new NpgsqlConnection(connectionString1))
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                    {

                        connection.Open();
                        cmd.CommandTimeout = 100000;
                        cmd.ExecuteNonQuery();

                    }
                }
          
           

        }

        public static void SendEmail1(string subject, string From_Email, string Passwd, string MessageBody, string sEmailAddress, List<string> cc = null)
        {

            //   Thread.Sleep(10000);




            From_Email = "metacoder1222@gmail.com";
            Passwd = "wzwebpvkopqzcsrw";



            try
            {
                //           System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                int tls12 = 3072; // Tls12 is not defined in the SecurityProtocolType enum in CLI/C++ / ToolsVersion="4.0"  
                                  //  System::Net::ServicePointManager::SecurityProtocol = (SecurityProtocolType)tls12;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                //shubham .....
                string toEmailId = sEmailAddress;

                var fromAddress = new MailAddress(From_Email);
                var toAddress = new MailAddress(toEmailId);


                //if (count > 9)
                //{
                //    toEmailId = "metacoder1222@gmail.com";
                //  
                //}


                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,

                    //587
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, Passwd)
                };
                string messge = MessageBody.ToString();
                var message = new MailMessage(fromAddress, toAddress)
                {

                    Subject = subject,
                    Body = messge,
                    IsBodyHtml = true
                };
                if (cc != null)
                {
                    foreach (string s in cc)
                    { }


                    //       message.Bcc.Add();
                }
              //  message.CC.Add("shivangi@atlantasys.com");
                message.Bcc.Add("chirag@atlantasys.in");

                {



                    ServicePointManager.ServerCertificateValidationCallback =
                          delegate (object s, X509Certificate certificate,
                            System.Security.Cryptography.X509Certificates.X509Chain chain, SslPolicyErrors sslPolicyErrors)
                          { return true; };

                    smtp.Send(message);
                    General.WriteToLogFile(sEmailAddress.ToString(), AppDomain.CurrentDomain.BaseDirectory, "Mailsend.txt");




                }
            }

            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, "E:\\shubham\\Services\\bin\\Error", "db.txt");
                //General.WriteToLogFile(sEmailAddress.ToString(), mFolderPath, "Exception.txt");
                //General.WriteToLogFile(ex.Message.ToString(), mFolderPath, "Exception.txt");
                //General.WriteToLogFile(ex.InnerException.ToString(), mFolderPath, "InnerException.txt");

            }

        }


        //public static void DML(string query, string userName)
        //{
        //    string connString = "Data Source=49.50.68.155;Initial Catalog=newtrack_" + userName + "; User ID = newtrack; Password=qwert@123; Max Pool Size=32767";
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection(connString))
        //        {

        //            using (SqlCommand cmd = new SqlCommand(query, connection))
        //            {

        //                connection.Open();
        //                cmd.CommandTimeout = 100000;
        //                cmd.ExecuteNonQuery();

        //            }
        //        }
        //    }
        //    catch (SqlException se)
        //    {
        //        if (se.ErrorCode != -2146232060)
        //        {
        //            throw se;
        //        }
        //    }
        //}

        //public static DataTable SelectQuery(string query, string userName = "")
        //{
        //    string connString = "";

        //    if (string.IsNullOrEmpty(userName))
        //        connString = "Data Source=49.50.68.155;Initial Catalog=newtrack;User ID=newtrack;Password=qwert@123; Max Pool Size=32767";
        //    else
        //        connString = "Data Source=49.50.68.155;Initial Catalog=newtrack_" + userName + "; User ID = newtrack; Password=qwert@123; Max Pool Size=32767";

        //    using (SqlConnection conn = new SqlConnection(connString))
        //    {
        //        conn.Open();
        //        SqlCommand command = new SqlCommand(query, conn);
        //        command.CommandTimeout = 5000000;
        //        SqlDataAdapter adapter = new SqlDataAdapter(command);
        //        DataTable dt = new DataTable();
        //        adapter.Fill(dt);
        //        return dt;
        //    }
        //}

        private static DataTable SelectQuery(string query, string connectionString)
        {//checking
            SqlConnection conn = null;
            try
            {

               
                using ( conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand(query, conn);
                    command.CommandTimeout = 5000000;
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
            catch (Exception ex)
            {
                conn.Close();
                return null;
            }
            finally
            {
                conn.Close();
            }
        }

        public static DataTable SelectQuery(string query)
        {
            return SelectQuery(query, connectionString1);
        }

        public static DataTable SelectQueryAtlTracking(string query)
        {
            return SelectQuery(query, connectionStringAtlTracking);
        }


        //public static DataTable SelectQuerypostgres(string query)
        //{//checking
        //    try
        //    {


        //        using (NpgsqlConnection conn = new NpgsqlConnection(connectionString2))
        //        {
        //            conn.Open();
        //            NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //            command.CommandTimeout = 5000000;
        //            NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command);
        //            DataTable dt = new DataTable();
        //            adapter.Fill(dt);
        //            return dt;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        public static void DML(string query, SqlParameter[] param)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString1))
                {

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {

                        connection.Open();
                        cmd.CommandTimeout = 100000;
                        cmd.Parameters.AddRange(param);
                        cmd.ExecuteNonQuery();

                    }
                }
            }
            catch (SqlException se)
            {
                if (se.ErrorCode != -2147467259)
                {
                    throw se;
                }

            }
        }


        public static bool IsLatest(string serviceId, DateTime gpsTime)
        {
            DataTable dt = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString1))
            {

                SqlCommand command = new SqlCommand("select max(gps_time) from  latest_telemetry with(nolock) where sys_service_id = '" + serviceId + "'", connection);
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dt);
                if (dt != null)
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToDateTime(dt.Rows[0][0]) < gpsTime && gpsTime < DateTime.UtcNow)
                            return true;
                    }
            }
            return false;
        }

        public static string GetServiceID(string IMEINumber, string receivedPacket)
        {

            return GetServiceID(IMEINumber);
        }

        public static string GetServiceID(string IMEINumber)
        {
            ServiceInfo serviceInfo = GetServiceInfo(IMEINumber);
            return serviceInfo == null ? "0" : serviceInfo.ServiceId;
        }

        private static string SqlSafe(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Replace("'", "''");
        }

        public static ServiceInfo GetServiceInfo(string IMEINumber)
        {
            string imei = SqlSafe(IMEINumber);
            try
            {
                string atlQuery = @"select top 1 s.id,s.sys_user_id,isnull(t.utc_offset,330) as utc_offset,s.veh_reg
                                    from tbl_devices d
                                    inner join tbl_services s on d.id=s.sys_device_id
                                    inner join tbl_users u on u.id=s.sys_user_id
                                    left join tbl_timezone t on t.id=u.timezone_id
                                    where d.imei = '" + imei + "'";
                DataTable dtAtl = SelectQueryAtlTracking(atlQuery);
                if (dtAtl != null && dtAtl.Rows.Count > 0)
                {
                    DataRow row = dtAtl.Rows[0];
                    return new ServiceInfo
                    {
                        ServiceId = Convert.ToString(row["id"]),
                        UserId = Convert.ToInt32(row["sys_user_id"]),
                        UTCOffset = Convert.ToInt32(row["utc_offset"]),
                        VehicleName = Convert.ToString(row["veh_reg"]),
                        IsAtlTrackingDevice = true
                    };
                }

                string newtrackQuery = @"select top 1 s.id,
                                                isnull(us.sys_user_id,isnull(s.sys_user_id,d.sys_user_id)) as sys_user_id,
                                                isnull(t.utc_offset,330) as utc_offset,
                                                isnull(us.veh_reg,s.veh_reg) as veh_reg
                                         from devices d
                                         inner join services s on d.id=s.sys_device_id
                                         left join user_services us on us.sys_service_id=s.id
                                         left join users u on u.id=isnull(us.sys_user_id,isnull(s.sys_user_id,d.sys_user_id))
                                         left join time_zone_php t on t.tz_name=u.sys_timezone
                                         where d.imei = '" + imei + "'";
                DataTable dtNewtrack = SelectQuery(newtrackQuery);
                if (dtNewtrack != null && dtNewtrack.Rows.Count > 0)
                {
                    DataRow row = dtNewtrack.Rows[0];
                    return new ServiceInfo
                    {
                        ServiceId = Convert.ToString(row["id"]),
                        UserId = Convert.ToInt32(row["sys_user_id"]),
                        UTCOffset = Convert.ToInt32(row["utc_offset"]),
                        VehicleName = Convert.ToString(row["veh_reg"]),
                        IsAtlTrackingDevice = false
                    };
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, "E://ListenerData/AtlantaNew/16214", "serviceiderror.txt");
            }

            return null;
        }



        public static double GetLatitude(string latitude)
        {
            if (latitude.Length <= 4)
                return 0.0;
            string[] strArray = latitude.Split('.');
            if (strArray[0].Length < 4)
            {
                int diff = 4 - strArray[0].Length;
                for (int i = 0; i < diff; i++)
                {
                    strArray[0] = "0" + strArray[0];

                }

            }
            double num = Convert.ToDouble(strArray[0].Substring(strArray[0].Length - 2) + "." + strArray[1]) / 60.0;
            return Convert.ToDouble(strArray[0].Substring(0, strArray[0].Length - 2)) + num;
        }

        public static double GetLongitude(string longitude)
        {
            if (longitude.Length <= 4)
                return 0.0;
            string[] strArray = longitude.Split('.');
            double num = Convert.ToDouble(strArray[0].Substring(strArray[0].Length - 2) + "." + strArray[1]) / 60.0;
            return Convert.ToDouble(strArray[0].Substring(0, strArray[0].Length - 2)) + num;
        }

        public static DateTime GetGPSDateTime(string dateStamp, string timeStamp)
        {
            try
            {
                return new DateTime(Convert.ToInt32("20" + dateStamp.Substring(4, 2)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                                    Convert.ToInt32(dateStamp.Substring(0, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                                    Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));
            }
            catch (Exception ex)
            {
                return new DateTime();
            }
        }

        public static DateTime GetGPSDateTimeWiFi(string dateStamp, string timeStamp)
        {
            try
            {
                return new DateTime(Convert.ToInt32(dateStamp.Substring(4, 4)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                                    Convert.ToInt32(dateStamp.Substring(0, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                                    Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));
            }
            catch (Exception ex)
            {
                return new DateTime();
            }
        }

        public static DateTime GetGPSDateTimeForTaxiMeter(string dateStamp, string timeStamp)
        {
            try
            {
                return new DateTime(Convert.ToInt32("20" + dateStamp.Substring(6, 2)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                                    Convert.ToInt32(dateStamp.Substring(0, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                                    Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));
            }
            catch (Exception ex)
            {
                return new DateTime();
            }
        }

        public static string GetProtocol(string content)
        {
            content = content.Trim();
            if (content.Contains("DIMTS"))
                return "DIMTS";
            if (content.Contains("ATL") && content.Contains("$GPRMC"))
                return "AtlantaNew";
            if ((content.IndexOf('$') == 16 && content.IndexOf('#') == -1) || content.Contains("$GPGGA"))
                return "AtlantaOldRoboG";
            if (content.IndexOf('$') == 16)
                return "AtlantaOld";
            if (content.IndexOf('$') == 0)
                return "TaxiMeter";
            if (content.IndexOf('$') == 23)
                return "AKS";
            if (content.LastIndexOf('@') >= 0)
                return "Wtrack";
            if (content.IndexOf("ATLIMG") >= 0)
                return "Camera";

            return null;

        }
        public static void SendNotification_dd(string mobiles, string msg, General gen, int route_id, int student_id,CustomSession session)
        {
            try
            {
                string userTable = session.IsAtlTrackingDevice
                          ? "atltracking.dbo.tbl_users"
                          : "newtrack.dbo.users";
                string routemaster;
                string student_master_backup;
                string bs_route_students;
                string bs_attendance;
                string rf_punch_history;
                string bs_user_master;
                string bs_parent_login_log;
                string bs_notification_parent;
                if (userTable == "atltracking.dbo.tbl_users")
                {
                    
                    bs_user_master = "atltracking.dbo.bs_user_master";
                    bs_notification_parent = "atltracking.dbo.bs_notification_parent";

                }
                else
                {
                 
                    bs_user_master = "newtrack.dbo.bs_user_master";
                    bs_notification_parent = "newtrack.dbo.bs_notification_parent";


                }
                //gen.DML("insert into bs_notification_parent(parent_id,message,date_time) values ((select id from bs_user_master where bs_user_name='" + mobiles + "') ,'" + msg + "',GETDATE())");
                gen.DML(
    "INSERT INTO "+ bs_notification_parent+"(parent_id, message, date_time, Route_id,student_id) " +
    "VALUES (" +
    "(SELECT Top 1 id FROM "+bs_user_master+" WHERE bs_user_name='" + mobiles + "'), " +
    "'" + msg + "', " +
    "GETDATE(), " +
    route_id +
    "," + student_id + ")"
);

            }
            catch (Exception ex)
            {
                General.WriteToLogFile("mob =" + mobiles + " msg " + msg, AppDomain.CurrentDomain.BaseDirectory, mobiles);
            }
        }

        public static int SendEmail(string subject, string messageBody, string toEmailId, List<string> cc = null)
        {
            try
            {
                var fromAddress = new MailAddress("notification@abctraq.com");
                var toAddress = new MailAddress(toEmailId);
                const string fromPassword = "abctraq123";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = messageBody
                })
                {

                    if (cc != null)
                    {
                        foreach (string s in cc)
                        {
                            message.CC.Add(s);
                        }
                    }
                    ServicePointManager.ServerCertificateValidationCallback =
   delegate (object s, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
   { return true; };

                    smtp.Send(message);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

        }
        static double _eQuatorialEarthRadius = 6378.1370D;
        static double _d2r = (Math.PI / 180D);
        public static double HaversineInKM(double lat1, double long1, double lat2, double long2)
        {
            double dlong = (long2 - long1) * _d2r;
            double dlat = (lat2 - lat1) * _d2r;
            double a = Math.Pow(Math.Sin(dlat / 2D), 2D) + Math.Cos(lat1 * _d2r) * Math.Cos(lat2 * _d2r) * Math.Pow(Math.Sin(dlong / 2D), 2D);
            double c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
            double d = _eQuatorialEarthRadius * c;

            return d;
        }



        public delegate T RetryOpenDelegate<T>();

        public static T RetryOpen<T>(RetryOpenDelegate<T> action)
        {

            while (true)
            {

                try
                {

                    return action();

                }

                catch (IOException)
                {

                    System.Threading.Thread.Sleep(50);

                }

            }

        }

        public static void WriteToLogFile(string msg, string folderPath, string logFile, bool isAppend = true, bool isDateTime = true)
        {

            // create our daily directory

            // string dir = AppDomain.CurrentDomain.BaseDirectory + "\\logs\\" + DateTime.Now.ToString("ddMMyyyy");

            try
            {

                string dir = folderPath + "\\" + DateTime.Now.ToString("ddMMyyyy");

                if (!Directory.Exists(dir))

                    Directory.CreateDirectory(dir);

                if (!Directory.Exists(dir + "\\ReceivedData"))
                    Directory.CreateDirectory(dir + "\\ReceivedData");

                if (!Directory.Exists(dir + "\\WrongProtocol"))
                    Directory.CreateDirectory(dir + "\\WrongProtocol");


                string LogFile = dir + "\\" + logFile;


                TextWriter tw = null;

                try
                {

                    // create a writer and open the file

                    tw = RetryOpen<StreamWriter>(delegate ()
                    {

                        return new StreamWriter(LogFile, isAppend);



                    });


                    // write a line of text to the file
                    if (isDateTime)
                        tw.WriteLine(DateTime.Now + Environment.NewLine + msg);
                    else
                        tw.WriteLine(msg);

                }

                catch { }

                finally
                {

                    // close the stream

                    if (tw != null)
                    {

                        tw.Close();

                        tw.Dispose();

                    }

                }
            }
            catch(Exception ex)
            {


            }

        }

        //public static void SendSms(string message, string mobileNo)
        //{
        //    Thread t = new Thread(new ThreadStart(() => SendSMSInNewThread(message, mobileNo)));
        //    t.Start();

        //}


        public void SendSms(string message, string mobileNo,General gen)
        {
            string receivedData = string.Empty;

            try
            {
                //string msgSenderUrl = "http://bulksms.a-tracker.com/sendsms.php?UserId=abctraq&Pwd=abctraq&Mobileno=" + mobileNo + "&Msg=" + message + "&SenderId=CellApps";
                message = message.Replace("(", "");
                message = message.Replace(")", "");
                message = message.Replace(@"\", "");
                message = message.Replace(@"-", " ");


                StringBuilder sb = new StringBuilder();
                //  int mob_len = 0;
                // StringBuilder student_id = new StringBuilder();

                sb.Clear();
                sb.Append("\"91" + mobileNo + "\"");
                string mobileNo_new = sb.ToString();
                // string msgSenderUrl = "http://182.18.176.147/pushsms.php?username=abctrq&password=90340&sender=abctrq&message=" + message + "&numbers=" + mobileNo;
                WebClient wc = new WebClient();
                wc.UseDefaultCredentials = false;
                wc.Credentials = new NetworkCredential("Atlanta-Systems", "Sandeep@123");
                wc.Headers.Add("Content-Type", "application/json");
                wc.Headers.Add("Authorization", "Basic QXRsYW50YS1TeXN0ZW1zOlNhbmRlZXBAMTIz");

                string data = wc.UploadString("https://api.infobip.com/sms/1/text/single", "{\"from\":\"ATLNTA\",\"to\":[" + mobileNo_new + "],\"text\":\"" + message + "\"}");

            }
            catch (Exception ex)
            {

            }
        }


        public void SendSms(string message, string mobileNo, string msgSenderUrl, string sender_id, int service_from, int student_id, CustomSession session,General gen)
        {

            string receivedData = string.Empty;

            try
            {
                //string msgSenderUrl = "http://bulksms.a-tracker.com/sendsms.php?UserId=abctraq&Pwd=abctraq&Mobileno=" + mobileNo + "&Msg=" + message + "&SenderId=CellApps";
                message = message.Replace("(", "");
                message = message.Replace(")", "");
                message = message.Replace(@"\", "");
                message = message.Replace(@"-", " ");

                // string msgSenderUrl = "http://182.18.176.147/pushsms.php?username=abctrq&password=90340&sender=abctrq&message=" + message + "&numbers=" + mobileNo;
                // string msgSenderUrl = "http://sms.jayinegroup.com/api/pushsms.php?username=abctrq&password=90340&sender=abctrq&message=" + message + "&numbers=" + mobileNo + "&unicode=false";
                if (service_from == 0)
                {
                    if (string.IsNullOrEmpty(msgSenderUrl))
                        msgSenderUrl = "http://smsby2.in/sendsms.php?username=atlanta&password=atlanta@123&sender=ATLNTA&mobile=" + mobileNo + "&message=" + message + "&route=T";
                    else
                        msgSenderUrl = string.Format(msgSenderUrl, mobileNo, message);


                    WebRequest webRequest = HttpWebRequest.Create(msgSenderUrl);
                    HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                    Stream stream = (Stream)response.GetResponseStream();

                    using (StreamReader sr = new StreamReader(stream))
                    {
                        receivedData = sr.ReadToEnd();
                    }

                    response.Close();
                    stream.Close();
                    DML("insert into bs_alert_log (sys_user_id,route_id,sys_service_id,bus_stop_id,mobile_no,student_id,message,message_id,alert_sent_on) values(" + session.UserId + "," + session.RouteId + "," + session.ServiceId + ",0,'" + mobileNo + "'," + student_id + ",'" + message + "','" + receivedData.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')");
                }
                else
                {
                    mobileNo = "91" + mobileNo;
                    WebClient wc = new WebClient();
                    wc.UseDefaultCredentials = false;
                    wc.Credentials = new NetworkCredential("support_team", "support@123");
                    wc.Headers.Add("Content-Type", "application/json");
                    wc.Headers.Add("Authorization", "Basic QXRsYW50YS1TeXN0ZW1zOlNhbmRlZXBAMTIz");
                    string data = wc.UploadString("https://api.infobip.com/sms/1/text/single", "{\"from\":\"" + sender_id + "\",\"to\":[" + mobileNo + "],\"text\":\"" + message + "\"}");
                    data = data.Substring(13);
                    data = data.Substring(0, data.LastIndexOf(']'));
                    Message result = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(data);
                    DML("insert into bs_alert_log (sys_user_id,route_id,sys_service_id,bus_stop_id,mobile_no,student_id,message_id,message,alert_sent_on) values (" + session.UserId + "," + session.RouteId + "," + session.ServiceId + ",0,'" + mobileNo.Substring(2) + "'," + student_id + ",'1','" + message + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')");

                }

            }
            catch (Exception ex)
            {

            }
        }

        public static void HandleNewRequest(CustomSession session, StringRequestInfo requestInfo, string m_FolderPath, string userName = "")
        {
            if (requestInfo.Key == "WrongProtocol")
            {
                if (requestInfo.Parameters[0] != null)
                    General.WriteToLogFile(requestInfo.Body, m_FolderPath, "WrongProtocol\\" + requestInfo.Parameters[0] + ".txt");
                else
                    General.WriteToLogFile(requestInfo.Body, m_FolderPath, "WrongProtocol\\Others.txt");

                return;
            }

            string qq = $"insert into automate_excelreport (imei,sys_proc_time) values {requestInfo.Key}, {DateTime.Now.ToString("ddMMyyyy")}";

            try
            {
                //General.DML(qq);
            }
            catch (Exception ex)
            {
            }

            General.WriteToLogFile(requestInfo.Body, m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");

            try
            {
                if (string.IsNullOrEmpty(session.ServiceId))
                {
                    if (requestInfo.Key.Length < 15)
                        return;

                    foreach (char c in requestInfo.Key)
                    {
                        if (!char.IsDigit(c))
                            return;
                    }

                    ServiceInfo serviceInfo = General.GetServiceInfo(requestInfo.Key);

                    if (serviceInfo != null && serviceInfo.ServiceId != "0")
                    {
                        session.ServiceId = serviceInfo.ServiceId;
                        session.IsAtlTrackingDevice = serviceInfo.IsAtlTrackingDevice;
                        session.IMEI = requestInfo.Key;

                        if (session.UTCOffset == 0)
                        {
                            session.UserId = serviceInfo.UserId;
                            session.UTCOffset = serviceInfo.UTCOffset;
                            session.VehicleName = serviceInfo.VehicleName;

                            General.WriteToLogFile(
                                "Service Mapping Success => IMEI = " + requestInfo.Key +
                                ", ServiceId = " + session.ServiceId +
                                ", UserId = " + session.UserId +
                                ", VehicleName = " + session.VehicleName +
                                ", UTCOffset = " + session.UTCOffset +
                                ", IsAtlTrackingDevice = " + session.IsAtlTrackingDevice,
                                m_FolderPath,
                                "rfid_mapping_debug.txt"
                            );
                            string userTable = session.IsAtlTrackingDevice
                           ? "atltracking.dbo.tbl_users"
                           : "newtrack.dbo.users";
                            string routemaster;
                            string student_master_backup;
                            string bs_route_students;
                            string bs_attendance;
                            string rf_punch_history;
                            string bs_user_master;
                            string sms_master;
                            string supscription;
                            if (userTable == "atltracking.dbo.tbl_users")
                            {
                                routemaster = "atltracking.dbo.bs_route_master";
                                student_master_backup = "atltracking.dbo.bs_student_master_backup";
                                bs_route_students = "atltracking.dbo.bs_route_students";
                                rf_punch_history = "atltracking.dbo.rf_punch_history ";
                                bs_attendance = "atltracking.dbo.bs_attendance";
                                bs_user_master = "atltracking.dbo.bs_user_master";
                                sms_master = "atltracking.dbo.bs_sms_master";
                                supscription = "atltracking.dbo.bs_attendance_subscription";

                            }
                            else
                            {
                                routemaster = "newtrack.dbo.bs_route_master";
                                student_master_backup = "newtrack.dbo.bs_student_master_backup";
                                bs_route_students = "newtrack.dbo.bs_route_students";
                                bs_attendance = "newtrack.dbo.bs_attendance";
                                rf_punch_history = "newtrack.dbo.rf_punch_history ";
                                bs_user_master = "newtrack.dbo.bs_user_master";
                                sms_master = "newtrack.dbo.bs_sms_master";
                                supscription = "newtrack.dbo.bs_attendance_subscription";



                            }

                            DataTable dtAuid = General.SelectQuery(@"
select 
    auid,
    iuid,
    bs_user_name
from "+bs_user_master +@"
where sys_user_id = " + session.UserId);

                            General.WriteToLogFile(
                                "BS User Master Query => UserId = " + session.UserId +
                                ", Rows = " + (dtAuid == null ? "NULL" : dtAuid.Rows.Count.ToString()),
                                m_FolderPath,
                                "rfid_mapping_debug.txt"
                            );

                            if (dtAuid != null && dtAuid.Rows.Count > 0)
                            {
                                session.DtMobileIds = dtAuid;
                            }

                            DataTable dtSMSAPI = General.SelectQuery(@"
select 
    msg_api_url,
    sender_id,
    msg_service_from
from " +sms_master+@"
where sys_user_id = " + session.UserId);

                            General.WriteToLogFile(
                                "BS SMS Master Query => UserId = " + session.UserId +
                                ", Rows = " + (dtSMSAPI == null ? "NULL" : dtSMSAPI.Rows.Count.ToString()),
                                m_FolderPath,
                                "rfid_mapping_debug.txt"
                            );

                            if (dtSMSAPI != null && dtSMSAPI.Rows.Count > 0)
                            {
                                session.SMSAPI = Convert.ToString(dtSMSAPI.Rows[0][0]);
                                session.sender_id = Convert.ToString(dtSMSAPI.Rows[0][1]).Trim();
                                session.service_from = Convert.ToInt32(dtSMSAPI.Rows[0][2]);
                            }

                            string query4 = @"
select 
    attendance_type_id
from "+supscription+ @"
where sys_user_id = " + session.UserId;

                            DataTable dtAttn = SelectQuery(query4);

                            General.WriteToLogFile(
                                "BS Attendance Subscription Query => UserId = " + session.UserId +
                                ", Rows = " + (dtAttn == null ? "NULL" : dtAttn.Rows.Count.ToString()),
                                m_FolderPath,
                                "rfid_mapping_debug.txt"
                            );

                            if (dtAttn != null && dtAttn.Rows.Count > 0)
                            {
                                session.AttendanceSubscription = new List<int>();

                                foreach (DataRow dr in dtAttn.Rows)
                                {
                                    session.AttendanceSubscription.Add(Convert.ToInt32(dr[0]));
                                }
                            }
                        }
                    }
                    else
                    {
                        General.WriteToLogFile(requestInfo.Key, m_FolderPath, "InvalidServiceId.txt", true, false);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, m_FolderPath, "exHandleRequest.txt");
            }
        }


        //public static void HandleNewRequest(CustomSession session, StringRequestInfo requestInfo, string m_FolderPath, string userName = "")
        //{



        //    // If wrong protocol or unmatched protocol packet received
        //    if (requestInfo.Key == "WrongProtocol")
        //    {
        //        if (requestInfo.Parameters[0] != null)
        //            General.WriteToLogFile(requestInfo.Body, m_FolderPath, "WrongProtocol\\" + requestInfo.Parameters[0] + ".txt");
        //        else
        //            General.WriteToLogFile(requestInfo.Body, m_FolderPath, "WrongProtocol\\Others.txt");
        //        return;
        //    }

        //    string qq = $"insert into automate_excelreport (imei,sys_proc_time) values {requestInfo.Key}, {DateTime.Now.ToString("ddMMyyyy")}";
        //    try
        //    {
        //        //General.DML(qq);
        //    }
        //    catch (Exception ex)
        //    {

        //    }





        //    General.WriteToLogFile(requestInfo.Body, m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");
        //    try
        //    {
        //        if (string.IsNullOrEmpty(session.ServiceId))
        //        {


        //            if (requestInfo.Key.Length < 15)
        //                return;

        //            foreach (char c in requestInfo.Key)
        //            {
        //                if (!char.IsDigit(c))
        //                    return;
        //            }



        //            ServiceInfo serviceInfo = General.GetServiceInfo(requestInfo.Key);

        //            if (serviceInfo != null && serviceInfo.ServiceId != "0")
        //            {
        //                session.ServiceId = serviceInfo.ServiceId;
        //                session.IsAtlTrackingDevice = serviceInfo.IsAtlTrackingDevice;
        //                session.IMEI = requestInfo.Key;

        //                //CheckUpdate(session);

        //                if (session.UTCOffset == 0)
        //                {
        //                    session.UserId = serviceInfo.UserId;
        //                    session.UTCOffset = serviceInfo.UTCOffset;
        //                    session.VehicleName = serviceInfo.VehicleName;

        //                    DataTable dtAuid = General.SelectQuery(@"select auid,iuid,bs_user_name from bs_user_master where sys_user_id=" + session.UserId);
        //                    if (dtAuid != null)
        //                    {
        //                        if (dtAuid.Rows.Count > 0)
        //                        {
        //                            session.DtMobileIds = dtAuid;
        //                        }
        //                    }

        //                    DataTable dtSMSAPI = General.SelectQuery(@"select msg_api_url , sender_id,msg_service_from from bs_sms_master where sys_user_id=" + session.UserId);

        //                    if (dtSMSAPI != null)
        //                    {
        //                        if (dtSMSAPI.Rows.Count > 0)
        //                        {
        //                            session.SMSAPI = Convert.ToString(dtSMSAPI.Rows[0][0]);
        //                            session.sender_id = Convert.ToString(dtSMSAPI.Rows[0][1]).Trim();
        //                            session.service_from = Convert.ToInt32(dtSMSAPI.Rows[0][2]);
        //                        }
        //                    }

        //                    string query4 = @"select attendance_type_id from bs_attendance_subscription where sys_user_id=" + session.UserId;
        //                    DataTable dtAttn = SelectQuery(query4);
        //                    if (dtAttn != null)
        //                        if (dtAttn.Rows.Count > 0)
        //                        {
        //                            session.AttendanceSubscription = new List<int>();
        //                            foreach (DataRow dr in dtAttn.Rows)
        //                            {
        //                                session.AttendanceSubscription.Add(Convert.ToInt32(dr[0]));
        //                            }
        //                        }
        //                }




        //            }
        //            else
        //            {
        //                General.WriteToLogFile(requestInfo.Key, m_FolderPath, "InvalidServiceId.txt", true, false);
        //                return;
        //            }
        //        }


        //        //if (!session.IsPortUpdated)
        //        //{
        //        //    string query = @"UPDATE services SET port_no=" + session.Config.Port + " WHERE id=" + session.ServiceId;
        //        //    //if (!string.IsNullOrEmpty(userName))
        //        //    // General.DML(query, userName);
        //        //    // else
        //        //    gen.DML(query);
        //        //    session.IsPortUpdated = true;
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        General.WriteToLogFile(ex.Message, m_FolderPath, "exHandleRequest.txt");
        //    }


        //}

        public static bool IsAlertTime(DateTime timeFrom, DateTime timeTo)
        {
            try
            {

                if (timeTo < timeFrom)
                    timeTo = timeTo.AddDays(1);

                // Checking if current time is within specified range
                if (DateTime.Now >= timeFrom && DateTime.Now <= timeTo)
                    return true;

                return false;
            }
            catch (Exception ex) { return false; }

        }


        public static void SendNotification_old(string msg, int user_id)
        {
            // EtaPredict p = new EtaPredict();


            string address = "https://fcm.googleapis.com/fcm/send";
            DataTable dt = null;


            try
            {

                dt = General.SelectQuery("select auid,iuid from user_did where sys_user_id=" + user_id);
                if (dt != null)
                {
                    if (dt.Rows.Count != 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            using (WebClient wc = new WebClient())
                            {
                                wc.Headers.Add("Content-Type", "application/json");
                                wc.Headers.Add("Authorization", "key=AAAAT3DETk8:APA91bF6KEzkfSZPWohBFj1eCct1U3JlbtWulHxNFqjmw9n75TFYsI3dpogkCsoUkndJvo9nN8WfGngbKw7Yw4puzdX_KX3tHmi3-ShrWHEsgsRyDo6hnU_zQeLiZS9hLJaTosmwm-MT");
                                if (!string.IsNullOrEmpty(dr["auid"].ToString()))
                                {
                                    try
                                    {
                                        wc.UploadString(address, "{\"to\":\"" + dr["auid"].ToString().Trim() + "\",\"data\": {\"abctraq\": \"" + msg + "\"}}");

                                    }
                                    catch (Exception e)
                                    {
                                        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
                                    }


                                }
                                if (!string.IsNullOrEmpty(dr["iuid"].ToString()))
                                {
                                    try
                                    {

                                        // string s = wc.UploadString(address, "{\"to\":\"" + dr["iuid"].ToString().Trim() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
                                        wc.UploadString(address, "{\"to\":\"" + dr["iuid"].ToString().Trim() + "\",\"notification\": {\"body\": \"" + msg + "\",\"priority\" : \"high\",\"sound\": \"default\"}}");


                                    }
                                    catch (Exception e)
                                    {
                                        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
                                    }


                                }

                            }
                        }
                        // DML("insert into alert_notification_log(alert_setting_id,message,sent_on,gps_latitude,gps_longitude) values (" + alert_setting_id + ",'" + msg + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + lat + "," + lng + ")");

                    }
                }

            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
            }

        }

        public static void SendNotification_old1(string mobiles, string msg,General gen)
        {


            General.WriteToLogFile("mob =" + mobiles + " msg " + msg, AppDomain.CurrentDomain.BaseDirectory, mobiles+".txt");



            // EtaPredict p = new EtaPredict();
            try
            {

                // string[] mobileNOs = mobiles.Split(',');
                string address = "https://fcm.googleapis.com/fcm/send";
                DataTable dt = null;

                //for (int i = 0; i < mobileNOs.Length - 1; i++)
                //{
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("Content-Type", "application/json");
                        wc.Headers.Add("Authorization", "key=AAAAXlE6GGI:APA91bE77NMjJQ-2WhpzV3TVnqReCN1T54hqT0WCgk89_Qb-x4eQjb2qxTkg1c3hrOgWCEGyV-vzmcBKSzhSrfOksan86XaGY3sjqrx4MbnUW6iXjihKoF4TC5PZs5TUn02ya-8YSdRL");
                        dt = General.SelectQuery("select auid,iuid from bs_user_master where bs_user_name='" + mobiles + "'");
                        if (dt.Rows.Count != 0)
                        {
                            // string uri = string.Format(address, msg, Convert.ToString(dt.Rows[0]["auid"]));

                           //  string s = "{\"to\":\"cxqNbSPj4U8:APA91bH9o-5C3jfofETp665qMKiy4JkdbmKpVX_b8tJkJheG9PRNLCy_OfiGrzHXSeisk45Ze91HVtbxQAMNTCaz7-J-qWn4HiyOH3MjcJvNFOuHpMfExO1fSNmOTtlxWXoIOE06XiqK\",\"notification\": {\"body\": \"Hello mobile\"}}";
                            //   request = WebRequest.Create(uri) as HttpWebRequest;
                            if (!string.IsNullOrEmpty(dt.Rows[0]["auid"].ToString()))
                            {
                                 string dd =   wc.UploadString(address, "{\"to\":\"" + dt.Rows[0]["auid"].ToString() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
                                 General.WriteToLogFile("Notification : msg = " + msg + "status = " + dd , AppDomain.CurrentDomain.BaseDirectory, mobiles + ".txt");
                            }
                            if (!string.IsNullOrEmpty(dt.Rows[0]["iuid"].ToString()))
                            {
                                  string ddd = wc.UploadString(address, "{\"to\":\"" + dt.Rows[0]["iuid"].ToString() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
                                 General.WriteToLogFile("Notification : msg = " + msg + "status = iphne " + ddd, AppDomain.CurrentDomain.BaseDirectory, mobiles + ".txt");
                            }
                            gen.DML("insert into bs_notification_parent(parent_id,message,date_time) values ((select id from bs_user_master where bs_user_name='" + mobiles + "') ,'" + msg + "',GETDATE())");

                            General.WriteToLogFile("Notification : msg = " + msg + "status = ", AppDomain.CurrentDomain.BaseDirectory, mobiles+".txt");
                            //t.Start();
                        }
                    }
                }

                catch (Exception ex)
                {

                    General.WriteToLogFile(ex.ToString(), AppDomain.CurrentDomain.BaseDirectory, mobiles);
                }
                // }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, mobiles);
            }
        }


        public static async Task SendNotification(string mobiles, string msg, General gen, int route_id, int student_id,CustomSession session)
        {
            DataTable dt = null;
            DataTable dt1 = null;
            string userTable = session.IsAtlTrackingDevice
                          ? "atltracking.dbo.tbl_users"
                          : "newtrack.dbo.users";
            string routemaster;
            string student_master_backup;
            string bs_route_students;
            string bs_attendance;
            string rf_punch_history;
            string bs_user_master;
            string bs_parent_login_log;
            string bs_notification_parent;
            if (userTable == "atltracking.dbo.tbl_users")
            {
                routemaster = "atltracking.dbo.bs_route_master";
                student_master_backup = "atltracking.dbo.bs_student_master_backup";
                bs_route_students = "atltracking.dbo.bs_route_students";
                rf_punch_history = "atltracking.dbo.rf_punch_history ";
                bs_attendance = "atltracking.dbo.bs_attendance";
                bs_user_master = "atltracking.dbo.bs_user_master";
                bs_parent_login_log = "atltracking.dbo.bs_parent_login_log";
                bs_notification_parent="atltracking.dbo.bs_notification_parent";

            }
            else
            {
                routemaster = "newtrack.dbo.bs_route_master";
                student_master_backup = "newtrack.dbo.bs_student_master_backup";
                bs_route_students = "newtrack.dbo.bs_route_students";
                bs_attendance = "newtrack.dbo.bs_attendance";
                rf_punch_history = "newtrack.dbo.rf_punch_history ";
                bs_user_master = "newtrack.dbo.bs_user_master";
                bs_parent_login_log = "newtrack.dbo.bs_parent_login_log";
                bs_notification_parent = "newtrack.dbo.bs_notification_parent";


            }
            try
            {
                string mobileSafe = string.IsNullOrEmpty(mobiles) ? "" : mobiles.Replace("'", "''");
                string msgSafe = string.IsNullOrEmpty(msg) ? "" : msg.Replace("'", "''");

                dt = General.SelectQuery(@"
select top 1 
    id 
from "+ bs_user_master+@" 
where bs_user_name = '" + mobileSafe + "'");

                if (dt != null && dt.Rows.Count > 0)
                {
                    string bsUserId = dt.Rows[0][0].ToString();

                    dt1 = General.SelectQuery(@"
select 
    fcm 
from "+ bs_parent_login_log+@" 
where bs_user_id = '" + bsUserId + @"' 
and fcm is not null order by id desc");

                    if (dt1 != null && dt1.Rows.Count > 0)
                    {
                        foreach (DataRow dr2 in dt1.Rows)
                        {
                            string fcm = Convert.ToString(dr2["fcm"]).Trim();

                            if (!string.IsNullOrEmpty(fcm))
                            {
                                try
                                {
                                    Firebase firebase = new Firebase();
                                    await firebase.FirebaseNotifications(fcm, msg, mobiles);
                                }
                                catch (Exception e)
                                {
                                    General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
                                }
                            }
                        }
                    }

                    gen.DML(@"
INSERT INTO "+ bs_notification_parent+@" 
(parent_id, message, date_time,Route_id,student_id)
VALUES
(" + bsUserId + ", '" + msgSafe + "', GETDATE(), " + route_id + ", " + student_id + ")");
                }
                else
                {
                    General.WriteToLogFile(
                        "No bs_user_master found for mobile = " + mobiles,
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Notification_Exp.txt"
                    );
                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
            }
        }

        //public static async Task SendNotification(string mobiles, string msg,General gen)
        //{

        //    DataTable dt = null;
        //    DataTable dt1 = null;


        //    try
        //    {

        //        dt = General.SelectQuery("select id from bs_user_master where bs_user_name='" + mobiles + "'");
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count != 0)
        //            {
        //                foreach (DataRow dr in dt.Rows)
        //                {
        //                    dt1 = General.SelectQuery("select fcm from bs_parent_login_log where bs_user_id = '" + dt.Rows[0][0].ToString() +"' order by id desc");
        //                    if (dt1.Rows.Count !=0)
        //                    {
        //                        foreach(DataRow dr2 in dt1.Rows)
        //                        {
        //                            if (!string.IsNullOrEmpty(dr2["fcm"].ToString()))
        //                            {
        //                                try
        //                                {
        //                                    Firebase firebase = new Firebase();
        //                                    await firebase.FirebaseNotifications(dr2["fcm"].ToString().Trim(), msg, mobiles);
        //                                    gen.DML("insert into bs_notification_parent(parent_id,message,date_time) values ((select id from bs_user_master where bs_user_name='" + mobiles + "') ,'" + msg + "',GETDATE())");

        //                                }
        //                                catch (Exception e)
        //                                {
        //                                    General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
        //                                }


        //                            }
        //                            //if (!string.IsNullOrEmpty(dr["iuid"].ToString()))
        //                            //{
        //                            //    try
        //                            //    {

        //                            //        Firebase firebase = new Firebase();
        //                            //        await firebase.FirebaseNotifications(dr["iuid"].ToString().Trim(), msg, mobiles);


        //                            //    }
        //                            //    catch (Exception e)
        //                            //    {
        //                            //        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
        //                            //        break;
        //                            //    }


        //                            //}
        //                        }

        //                    }


        //                    //await General.SendNotification(user_id, msg, "alert");
        //                }
        //                gen.DML("insert into bs_notification_parent(parent_id,message,date_time) values ((select id from bs_user_master where bs_user_name='" + mobiles + "') ,'" + msg + "',GETDATE())");
        //                //gen.DML("insert into alert_notification_log(alert_setting_id,message,sent_on,gps_latitude,gps_longitude) values (" + alert_setting_id + ",'" + main_msg + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + lat + "," + lng + ")");

        //            }
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
        //    }

        //}
        public class Firebase
        {
            string pathToServiceAccountKey = AppDomain.CurrentDomain.BaseDirectory;
            public Firebase()
            {

                var defaultApp = FirebaseApp.DefaultInstance;
                if (defaultApp == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        //Credential = GoogleCredential.FromFile($"{pathToServiceAccountKey}\\schoolbuddy-4fc6d-firebase-adminsdk-xh2kk-f674f1807a.json"),
                        Credential = GoogleCredential.FromFile($"{pathToServiceAccountKey}\\schoolbuddy-4fc6d-firebase-adminsdk-xh2kk-5fac47bd25.json"),
                    });
                }
                // Initialize Firebase App

            }


            public async Task FirebaseNotifications(string auidoriuid, string msg, string mobile)
            {
                try
                {
                    var registrationToken = $"{auidoriuid}";

                    // See documentation on defining a message payload.
                    var message = new Message()
                    {
                        //Data = new Dictionary<string, string>()
                        //{
                        //    //{ "abctraq", $"{msg}" },
                        //    { "abctraq", $"{msg}" },

                        //},
                        //Data = new Dictionary<string, string>()
                        //{
                        //    { "Schoolbuddy", $"{msg}" }, // Custom data you want to send
                        //},
                        Notification = new Notification
                        {
                            Title = "Schoolbuddy",
                            Body = $"{msg}",
                        },
                        Token = registrationToken,
                    };

                    // Send a message to the device corresponding to the provided
                    // registration token.
                    string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                    // Response is a message ID string.
                    General.WriteToLogFile($"{mobile} :{msg} : sent notification", AppDomain.CurrentDomain.BaseDirectory, "notification.txt");
                }
                catch (Exception ex)
                {
                    General.WriteToLogFile($"Notification: {mobile} : {msg}" + ex.Message + "  " + Environment.NewLine + ex.Message, AppDomain.CurrentDomain.BaseDirectory, "notificationerror.txt");

                }
                // This registration token comes from the client FCM SDKs.

            }
        }



    }
}
