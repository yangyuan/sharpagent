using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using sharpagent;

namespace sharpagentcli
{
    class Program
    {
        static string report = "{}";
        private static void HttpCallback(string path, ref byte[] body, ref string contenttype)
        {
            contenttype = "text/json";
            body = System.Text.Encoding.UTF8.GetBytes(report);
            Console.WriteLine("fetched");
        }

        private static void ProcCallback(string data)
        {
            report = data;
            Console.WriteLine("reported");
        }

        static void Main(string[] args)
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HttpListener Not Available.");
                return;
            }
            AsyncProcMonitor apm = new AsyncProcMonitor(ProcCallback);
            AsyncHttpListener ahl = new AsyncHttpListener(8655, HttpCallback);
            Console.Read();
            ahl.Terminate(2000);
            apm.Terminate(2000);
        }

    }
}
