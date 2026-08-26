using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine;

namespace GPSTrackerListeners.AtlantaNew
{
    class Program : ServiceBase
    {

        // ATL864180053248535,$GPRMC,183235,A,2835.1582,N,07703.7021,E,0.0,0,121124,,,*21,#01111011000000,1.06,-70.00,0A6FFA8C,14891.03,33,4.9,23,404,10,ca,6082ATL
        static void Main(string[] args)
        {


            //var server = new AtlantaNewServer();
            //var config = new ServerConfig { Port = 16214, MaxConnectionNumber = 10000, MaxRequestLength = 10485760 };
            //server.Setup(config);
            //server.Start();
            //Console.Read();


            if (args.Length > 0)
            {
                for (int ii = 0; ii < args.Length; ii++)
                {
                    switch (args[ii].ToUpper())
                    {
                        case "/I":
                            InstallService();
                            return;
                        case "/U":
                            UninstallService();
                            return;
                        default:
                            break;
                    }
                }
            }

            else
                System.ServiceProcess.ServiceBase.Run(new Program());

        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string loggerMessage = DateTime.Now + Environment.NewLine +
                                  "An unhandled exception has occured in method " + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetMethod().Name + " at line no." + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetFileLineNumber() + " : " + Environment.NewLine +
                                 "Exception is: " + ((Exception)e.ExceptionObject).Message;
            General.WriteToLogFile(loggerMessage, AppDomain.CurrentDomain.BaseDirectory, "exUn.txt");

           // General.SendSms("Alerts service crashed", "8750992183");
            General.SendEmail("Alerts service crashed", loggerMessage, "tauseef@atlantasys.in");

        }

        protected override void OnStart(string[] args)
        {
            try
            {            
            base.OnStart(args);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            var server = new AtlantaNewServer();
            var config = new ServerConfig { Port = 16214, MaxConnectionNumber = 10000, MaxRequestLength = 4096 };
            server.Setup(config);
            server.Start();
            
            }
            catch(Exception ex)
            {
              
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void Dispose(bool disposing)
        {
            //clean your resources if you have to
            base.Dispose(disposing);
        }

        private static void InstallService()
        {
            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == General.ServiceName);
        }

        private static void UninstallService()
        {

            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }

    }
}
