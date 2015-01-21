using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace sharpagent
{
    class Program : ServiceBase
    {
        public const string Title = "ZeStack Agent";
        public const string Description = "System Resource Monitor Agent for ZeStack";
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                string path = Assembly.GetExecutingAssembly().Location;
                string tmppath = Path.GetTempPath();
                switch (parameter)
                {
                    case "-i":
                    case "/i":
                        ManagedInstallerClass.InstallHelper(new string[] { "/LogFile=", "/LogToConsole=false", "/InstallStateDir=" + tmppath, path });
                        break;
                    case "-u":
                    case "/u":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", "/LogFile=", "/LogToConsole=false", "/InstallStateDir=" + tmppath, path });
                        break;
                    default:
                        Console.WriteLine("USAGE: -i -u /i /u");
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(":)");
                        Console.ResetColor();
                        
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new Program());
            }
        }

        static string report = "{}";
        private static void HttpCallback(string path, ref byte[] body, ref string contenttype)
        {
            contenttype = "text/json";
            body = System.Text.Encoding.UTF8.GetBytes(report);
        }

        private static void ProcCallback(string data)
        {
            report = data;
        }


        private AsyncHttpListener httpserver;
        private AsyncProcMonitor procminitor;
        public Program()
        {
            this.ServiceName = Title;
        }

        private Thread thread;
        protected override void OnStart(string[] args)
        {
            procminitor = null;
            httpserver = null;
            thread = new Thread(StartFunc);
            thread.IsBackground = true;
            thread.Start();
        }

        private void StartFunc()
        {
            procminitor = new AsyncProcMonitor(ProcCallback);
            httpserver = new AsyncHttpListener(8655, HttpCallback);
        }

        protected override void OnStop()
        {
            if (!thread.Join(1000))
            {
                thread.Abort();
            }
            if (httpserver != null) httpserver.Terminate(1000);
            if (procminitor != null) procminitor.Terminate(3000);
        }
    }


    [RunInstaller(true)]
    public class ProgramServiceInstaller : Installer
    {
        public ProgramServiceInstaller()
        {
            ServiceProcessInstaller spi = new ServiceProcessInstaller();
            spi.Account = ServiceAccount.LocalSystem;
            Installers.Add(spi);

            ServiceInstaller si = new ServiceInstaller();
            si.ServiceName = Program.Title;
            si.DisplayName = Program.Title;
            si.Description = Program.Description;
            si.StartType = ServiceStartMode.Automatic;
            Installers.Add(si);
        }
    }
}
