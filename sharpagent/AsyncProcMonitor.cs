using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace sharpagent
{
    public delegate void AsyncProcMonitorCallback(string data);
    public class AsyncProcMonitor
    {
        private ManualResetEvent threadshutdown;
        private Thread thread;
        bool terminated;
        JObject report;
        AsyncProcMonitorCallback callback;

        Dictionary<String, TickObject> caches;

        public AsyncProcMonitor(AsyncProcMonitorCallback callback)
        {
            if (callback == null)
            {
                this.callback = new AsyncProcMonitorCallback(DefaultCallback);
            }
            else {
                this.callback = callback; 
            }
            terminated = false;
            caches = new Dictionary<String, TickObject>();

            report = new JObject();
            InitReport();

            threadshutdown = new ManualResetEvent(false);
            thread = new Thread(WorkerThreadFunc);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Terminate(int millisecondsTimeout)
        {
            if (terminated) return;
            terminated = true;
            threadshutdown.Set();
            if (!thread.Join(millisecondsTimeout))
            {
                // thread failed to terminated itself
                thread.Abort();
            }
        }

        private void WorkerThreadFunc()
        {
            InitReport();
            callback(FetchReport());
            while (!threadshutdown.WaitOne(0)) // 
            {
                callback(FetchReport());
                Thread.Sleep(1000 * 3);
            }
        }

        class ReportValue : JObject
        {
            public ReportValue(Double volume, String unit)
            {
                this["volume"] = volume;
                this["unit"] = unit;
            }
            public ReportValue(Int32 volume, String unit)
            {
                this["volume"] = volume;
                this["unit"] = unit;
            }
        }

        private string InitReport()
        {
            report = new JObject();
            report["status"] = "ERROR";
            report["hostname"] = "";
            report["ip_address"] = "127.0.0.1";
            report["timestamp"] = DateTime.Now;
            report["data"] = new JObject();

            {
                JObject item = new JObject();
                item["timestamp"] = DateTime.Now;
                item["data"] = new JObject();
                item["data"]["cpu_num"] = new ReportValue(0, "");
                item["data"]["idletime"] = new ReportValue(0, "s");
                item["data"]["uptime"] = new ReportValue(0, "s");
                item["data"]["idle_rate"] = new ReportValue(0, "%");
                report["data"]["UptimePollster"] = item;
            }

            {
                JObject item = new JObject();
                item["timestamp"] = DateTime.Now;
                item["data"] = new JObject();
                item["data"]["net_pkts_in"] = new ReportValue(0, "p/s");
                item["data"]["net_pkts_out"] = new ReportValue(0, "p/s");
                item["data"]["net_bytes_out_sum"] = new ReportValue(0, "B");
                item["data"]["net_bytes_out"] = new ReportValue(0, "B/s");
                item["data"]["net_bytes_in_sum"] = new ReportValue(0, "B");
                item["data"]["net_bytes_in"] = new ReportValue(0, "B/s");
                report["data"]["NetStatPollster"] = item;
            }

            {
                JObject item = new JObject();
                item["timestamp"] = DateTime.Now;
                item["data"] = new JObject();
                item["data"]["cpu"] = new ReportValue(0, "%");
                report["data"]["CPUUsagePollster"] = item;
            }

            {
                JObject item = new JObject();
                item["timestamp"] = DateTime.Now;
                item["data"] = new JObject();
                item["data"]["total_available"] = 0;
                item["data"]["total_free"] = 0;
                item["data"]["total_capacity"] = 0;
                report["data"]["DiskUsagePollster"] = item;
            }

            {
                JObject item = new JObject();
                item["timestamp"] = DateTime.Now;
                item["data"] = new JObject();
                item["data"]["load_1_min"] = new ReportValue(0.0, "");
                item["data"]["load_5_min"] = new ReportValue(0.0, "");
                item["data"]["load_15_min"] = new ReportValue(0.0, "");
                item["data"]["nr_thread"] = "";
                item["data"]["last_pid"] = "";
                report["data"]["LoadStatPollster"] = item;
            }

            {
                JObject item = new JObject();
                item["timestamp"] = DateTime.Now;
                item["data"] = new JObject();
                item["data"]["Hugepagesize"] = new ReportValue(0.0, "MB");
                item["data"]["WritebackTmp"] = new ReportValue(0.0, "MB");
                item["data"]["Cached"] = new ReportValue(0.0, "MB");
                item["data"]["SwapCached"] = new ReportValue(0.0, "MB");
                item["data"]["SwapFree"] = new ReportValue(0.0, "MB");
                item["data"]["VmallocTotal"] = new ReportValue(0.0, "MB");
                item["data"]["KernelStack"] = new ReportValue(0.0, "MB");
                item["data"]["HugePages_Rsvd"] = new ReportValue(0, "");
                item["data"]["MemFree"] = new ReportValue(0.0, "MB");
                item["data"]["Unevictable"] = new ReportValue(0.0, "MB");
                item["data"]["Committed_AS"] = new ReportValue(0.0, "MB");
                item["data"]["Active(file)"] = new ReportValue(0.0, "MB");
                item["data"]["NFS_Unstable"] = new ReportValue(0.0, "MB");
                item["data"]["Mapped"] = new ReportValue(0.0, "MB");
                item["data"]["VmallocChunk"] = new ReportValue(0.0, "MB");
                item["data"]["AnonHugePages"] = new ReportValue(0.0, "MB");
                item["data"]["SUnreclaim"] = new ReportValue(0.0, "MB");
                item["data"]["Writeback"] = new ReportValue(0.0, "MB");
                item["data"]["Inactive(file)"] = new ReportValue(0.0, "MB");
                item["data"]["MemTotal"] = new ReportValue(0.0, "MB");
                item["data"]["SReclaimable"] = new ReportValue(0.0, "MB");
                item["data"]["VmallocUsed"] = new ReportValue(0.0, "MB");
                item["data"]["HardwareCorrupted"] = new ReportValue(0.0, "MB");
                item["data"]["HugePages_Total"] = new ReportValue(0, "");
                item["data"]["AnonPages"] = new ReportValue(0.0, "MB");
                item["data"]["HugePages_Free"] = new ReportValue(0, "");
                item["data"]["DirectMap2M"] = new ReportValue(0.0, "MB");
                item["data"]["HugePages_Surp"] = new ReportValue(0, "");
                item["data"]["Bounce"] = new ReportValue(0.0, "MB");
                item["data"]["SwapTotal"] = new ReportValue(0.0, "MB");
                item["data"]["Shmem"] = new ReportValue(0.0, "MB");
                item["data"]["Inactive"] = new ReportValue(0.0, "MB");
                item["data"]["PageTables"] = new ReportValue(0.0, "MB");
                item["data"]["Inactive(anon)"] = new ReportValue(0.0, "MB");
                item["data"]["Active(anon)"] = new ReportValue(0.0, "MB");
                item["data"]["Active"] = new ReportValue(0.0, "MB");
                item["data"]["DirectMap4k"] = new ReportValue(0.0, "MB");
                item["data"]["CommitLimit"] = new ReportValue(0.0, "MB");
                item["data"]["Mlocked"] = new ReportValue(0.0, "MB");
                item["data"]["Slab"] = new ReportValue(0.0, "MB");
                item["data"]["Buffers"] = new ReportValue(0.0, "MB");
                item["data"]["Dirty"] = new ReportValue(0.0, "MB");
                report["data"]["MemInfoPollster"] = item;
            }

            string json = report.ToString();
            return json;
        }

        class ReportDiskValue : JObject
        {
            public ReportDiskValue(string name)
            {
                this["available"] = new ReportValue(0.0, "GB");
                this["used"] = 0.0;
                this["capacity"] = new ReportValue(0.0, "GB");
                this["free"] = new ReportValue(0.0, "GB");
                this["mnt"] = "";
                this["dev"] = name;
                this["fstype"] = "";
                this["io_stat"] = new JObject();
                this["io_stat"]["r/s"] = new ReportValue(0.0, "");
                this["io_stat"]["w/s"] = new ReportValue(0.0, "");
                this["io_stat"]["wkB/s"] = new ReportValue(0.0, "KB/s");
                this["io_stat"]["rkB/s"] = new ReportValue(0.0, "KB/s");
            }
        }

        private string FetchReport()
        {

            // hostname
            try
            {
                report["hostname"] = Dns.GetHostName();
            }
            catch { }

            // ip_address, find first ip address, maybe ipv4 or ipv6, but must enternet with gateway
            try
            {
                // NetworkInterface can also get statics
                // This place only find first ethernet ip(v4) with gateway
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                
                foreach (NetworkInterface adapter in adapters)
                {
                    if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;
                    IPInterfaceProperties ipip = adapter.GetIPProperties();
                    if (ipip.GatewayAddresses.Count == 0) continue;
                    foreach (UnicastIPAddressInformation uipi in ipip.UnicastAddresses)
                    {
                        // May have a auto ipv6 address, LinkLayerAddress is used to id it
                        if (uipi.SuffixOrigin != SuffixOrigin.LinkLayerAddress)
                        {
                            report["ip_address"] = uipi.Address.ToString();
                        }
                    }
                }
            }
            catch { }

            try
            {
                UpdateReportProcessor();
            }
            catch { }
            try
            {
                UpdateReportMemory();
            }
            catch { }
            try
            {
                UpdateReportDisk();
            }
            catch { }
            try
            {
                UpdateReportNetwork();
            }
            catch { }

            report["status"] = "NORMAL";
            report["timestamp"] = DateTime.Now;

            string json = report.ToString();
            return json;
        }


        class TickObject
        {
            DateTime datetime;
            public UInt64 ValueIn;
            public UInt64 ValueOut;
            public UInt64 UnitIn;
            public UInt64 UnitOut;

            private Double _x_ValueIn;
            private Double _x_ValueOut;
            private Double _x_UnitIn;
            private Double _x_UnitOut;
            public TickObject()
            {
                datetime = DateTime.MinValue;
                ValueIn = 0;
                ValueOut = 0;
                UnitIn = 0;
                UnitOut = 0;
                _x_ValueIn = 0;
                _x_ValueOut = 0;
                _x_UnitIn = 0;
                _x_UnitOut = 0;
            }

            public void Tick(UInt64 _ValueIn, UInt64 _ValueOut, UInt64 _UnitIn, UInt64 _UnitOut,
                ref Double x_ValueIn, ref Double x_ValueOut, ref Double x_UnitIn, ref Double x_UnitOut)
            {
                DateTime datetimetmp = DateTime.Now;

                Double span = (DateTime.Now - datetime).TotalSeconds;
                x_ValueIn = (_ValueIn - ValueIn) / span;
                x_ValueOut = (_ValueOut - ValueOut) / span;
                x_UnitIn = (_UnitIn - UnitIn) / span;
                x_UnitOut = (_UnitOut - UnitOut) / span;

                x_ValueIn  = _x_ValueIn * 0.5 + x_ValueIn * 0.5;
                x_ValueOut = _x_ValueOut * 0.5 + x_ValueOut * 0.5;
                x_UnitIn   = _x_UnitIn * 0.5 + x_UnitIn * 0.5;
                x_UnitOut  = _x_UnitOut * 0.5 + x_UnitOut * 0.5;

                _x_ValueIn  = x_ValueIn;
                _x_ValueOut = x_ValueOut;
                _x_UnitIn   = x_UnitIn;
                _x_UnitOut  = x_UnitOut;

                datetime = datetimetmp;
                ValueIn = _ValueIn;
                ValueOut = _ValueOut;
                UnitIn = _UnitIn;
                UnitOut = _UnitOut;
            }
        }

        private void UpdateReportNetwork()
        {
            ManagementClass mc;
            ManagementObjectCollection moc;
            JObject item = (JObject)report["data"]["NetStatPollster"];
            item["timestamp"] = DateTime.Now;
            // Win32_NetworkAdapterSetting (2003 or later, not xp)
            // -- Win32_NetworkAdapterConfiguration
            // -- Win32_NetworkAdapter
            // Win32_PerfRawData_Tcpip_NetworkInterface Win32_PerfFormattedData_Tcpip_NetworkInterface

            /*
            // Win32_PerfFormattedData_Tcpip_NetworkInterface not working in our virtual machine
            mc = new ManagementClass("Win32_PerfFormattedData_Tcpip_NetworkInterface");
            moc = mc.GetInstances();
            UInt64 BytesReceivedPerSec = 0;
            UInt64 BytesSentPerSec = 0;
            UInt64 PacketsReceivedPerSec = 0;
            UInt64 PacketsSentPerSec = 0;
            foreach (ManagementObject mo in moc)
            {
                // BUGFIX for some os
                if (mo.Properties["BytesReceivedPerSec"].Type == CimType.UInt64)
                {
                    BytesReceivedPerSec += (UInt64)mo.Properties["BytesReceivedPerSec"].Value;
                    BytesSentPerSec += (UInt64)mo.Properties["BytesSentPerSec"].Value;
                    PacketsReceivedPerSec += (UInt64)mo.Properties["PacketsReceivedPerSec"].Value;
                    PacketsSentPerSec += (UInt64)mo.Properties["PacketsSentPerSec"].Value;
                }
                else
                {
                    BytesReceivedPerSec += (UInt32)mo.Properties["BytesReceivedPerSec"].Value;
                    BytesSentPerSec += (UInt32)mo.Properties["BytesSentPerSec"].Value;
                    PacketsReceivedPerSec += (UInt32)mo.Properties["PacketsReceivedPerSec"].Value;
                    PacketsSentPerSec += (UInt32)mo.Properties["PacketsSentPerSec"].Value;
                }
            }
            */

            mc = new ManagementClass("Win32_PerfRawData_Tcpip_NetworkInterface");
            moc = mc.GetInstances();
            UInt64 TotalBytesReceivedPerSec = 0;
            UInt64 TotalBytesSentPerSec = 0;
            UInt64 TotalPacketsReceivedPerSec = 0;
            UInt64 TotalPacketsSentPerSec = 0;

            foreach (ManagementObject mo in moc)
            {
                // BUGFIX for some os
                if (mo.Properties["BytesReceivedPerSec"].Type == CimType.UInt64)
                {
                    TotalBytesReceivedPerSec += (UInt64)mo.Properties["BytesReceivedPerSec"].Value;
                    TotalBytesSentPerSec += (UInt64)mo.Properties["BytesSentPerSec"].Value;
                    TotalPacketsReceivedPerSec += (UInt64)mo.Properties["PacketsReceivedPerSec"].Value;
                    TotalPacketsSentPerSec += (UInt64)mo.Properties["PacketsSentPerSec"].Value;
                }
                else
                {
                    TotalBytesReceivedPerSec += (UInt32)mo.Properties["BytesReceivedPerSec"].Value;
                    TotalBytesSentPerSec += (UInt32)mo.Properties["BytesSentPerSec"].Value;
                    TotalPacketsReceivedPerSec += (UInt32)mo.Properties["PacketsReceivedPerSec"].Value;
                    TotalPacketsSentPerSec += (UInt32)mo.Properties["PacketsSentPerSec"].Value;
                }
            }


            Double BytesReceivedPerSec = 0;
            Double BytesSentPerSec = 0;
            Double PacketsReceivedPerSec = 0;
            Double PacketsSentPerSec = 0;

            {
                if (!caches.ContainsKey("network"))
                {
                    caches.Add("network", new TickObject());
                }

                caches["network"].Tick(
                    TotalBytesReceivedPerSec, TotalBytesSentPerSec,
                    TotalPacketsReceivedPerSec, TotalPacketsSentPerSec,
                    ref BytesReceivedPerSec, ref BytesSentPerSec,
                    ref PacketsReceivedPerSec, ref PacketsSentPerSec
                    );
            }

            item["data"]["net_pkts_in"] = new ReportValue(PacketsReceivedPerSec, "p/s");
            item["data"]["net_pkts_out"] = new ReportValue(PacketsSentPerSec, "p/s");
            item["data"]["net_bytes_in"] = new ReportValue(BytesReceivedPerSec, "B/s");
            item["data"]["net_bytes_out"] = new ReportValue(BytesSentPerSec, "B/s");
            item["data"]["net_bytes_in_sum"] = new ReportValue(TotalBytesReceivedPerSec, "B");
            item["data"]["net_bytes_out_sum"] = new ReportValue(TotalBytesSentPerSec, "B");
        }


        private void UpdateReportDisk()
        {
            ManagementClass mc;
            ManagementObjectCollection moc;
            JObject item = (JObject)report["data"]["DiskUsagePollster"];
            item["timestamp"] = DateTime.Now;

            mc = new ManagementClass("Win32_LogicalDisk");
            moc = mc.GetInstances();
            UInt64 TotalFreeSpace = 0;
            UInt64 TotalSize = 0;
            foreach (ManagementObject mo in moc)
            {
                UInt32 DriveType = (UInt32) mo.Properties["DriveType"].Value;
                String Name = (String) mo.Properties["Name"].Value;
                if (DriveType != 3) continue; // not local disk
                String FileSystem = (String)mo.Properties["FileSystem"].Value;
                UInt64 Size = (UInt64) mo.Properties["Size"].Value;
                UInt64 FreeSpace = (UInt64) mo.Properties["FreeSpace"].Value;

                TotalFreeSpace += FreeSpace;
                TotalSize += Size;

                item["data"][Name] = new ReportDiskValue(Name);
                item["data"][Name]["available"] = new ReportValue(FreeSpace / 1024 / 1024 / 1024, "GB");
                item["data"][Name]["used"] = (Size - FreeSpace) / 1024 / 1024 / 1024; // BUGFIX
                item["data"][Name]["free"] = new ReportValue(FreeSpace / 1024 / 1024 / 1024, "GB");
                item["data"][Name]["capacity"] = new ReportValue(Size / 1024 / 1024 / 1024, "GB");
                item["data"][Name]["fstype"] = FileSystem;
            }

            // // Win32_PerfRawData_PerfDisk_LogicalDisk Win32_PerfFormattedData_PerfDisk_LogicalDisk
            /*
            // Win32_PerfFormattedData_Tcpip_NetworkInterface not working in our virtual machine
            mc = new ManagementClass("Win32_PerfFormattedData_PerfDisk_LogicalDisk");
            moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                String Name = (String) mo.Properties["Name"].Value;
                if (Name == "_Total") // if it has a _Total value
                {
                    // nothing
                }
                else if (item["data"][Name] != null)
                {
                    JObject subitem = (JObject)item["data"][Name]["io_stat"];
                    UInt64 DiskReadBytesPerSec = (UInt64)mo.Properties["DiskReadBytesPerSec"].Value;
                    UInt64 DiskWriteBytesPerSec = (UInt64)mo.Properties["DiskWriteBytesPerSec"].Value;
                    UInt32 DiskReadsPerSec = (UInt32)mo.Properties["DiskReadsPerSec"].Value;
                    UInt32 DiskWritesPerSec = (UInt32)mo.Properties["DiskWritesPerSec"].Value;


                    subitem["r/s"] = new ReportValue(DiskReadsPerSec, "");
                    subitem["w/s"] = new ReportValue(DiskWritesPerSec, "");
                    subitem["rkB/s"] = new ReportValue(DiskReadBytesPerSec / 1024, "KB/s");
                    subitem["wkB/s"] = new ReportValue(DiskWriteBytesPerSec / 1024, "KB/s");
                }

            }
             * */


            mc = new ManagementClass("Win32_PerfRawData_PerfDisk_LogicalDisk");
            moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                String Name = (String)mo.Properties["Name"].Value;
                if (Name == "_Total") // if it has a _Total value
                {
                    // nothing
                }
                else if (item["data"][Name] != null)
                {
                    JObject subitem = (JObject)item["data"][Name]["io_stat"];

                    if (!caches.ContainsKey("disk_" + Name))
                    {
                        caches.Add("disk_" + Name, new TickObject());
                    }

                    Double DiskReadBytesPerSec = 0;
                    Double DiskWriteBytesPerSec = 0;
                    Double DiskReadsPerSec = 0;
                    Double DiskWritesPerSec = 0;

                    UInt64 TotalDiskReadBytesPerSec = (UInt64)mo.Properties["DiskReadBytesPerSec"].Value;
                    UInt64 TotalDiskWriteBytesPerSec = (UInt64)mo.Properties["DiskWriteBytesPerSec"].Value;
                    UInt32 TotalDiskReadsPerSec = (UInt32)mo.Properties["DiskReadsPerSec"].Value;
                    UInt32 TotalDiskWritesPerSec = (UInt32)mo.Properties["DiskWritesPerSec"].Value;

                    caches["disk_" + Name].Tick(
                        TotalDiskReadBytesPerSec, TotalDiskWriteBytesPerSec,
                        TotalDiskReadsPerSec, TotalDiskWritesPerSec,
                        ref DiskReadBytesPerSec, ref DiskWriteBytesPerSec,
                        ref DiskReadsPerSec, ref DiskWritesPerSec
                        );

                    subitem["r/s"] = new ReportValue(DiskReadsPerSec, "");
                    subitem["w/s"] = new ReportValue(DiskWritesPerSec, "");
                    subitem["rkB/s"] = new ReportValue(DiskReadBytesPerSec / 1024, "KB/s");
                    subitem["wkB/s"] = new ReportValue(DiskWriteBytesPerSec / 1024, "KB/s");
                }

            }

            // BUGFIX
            item["data"]["total_available"] = TotalFreeSpace / 1024 / 1024 / 1024;
            item["data"]["total_free"] = TotalFreeSpace / 1024 / 1024 / 1024;
            item["data"]["total_capacity"] = TotalSize / 1024 / 1024 / 1024;
        }

        private void UpdateReportProcessor()
        {
            ManagementClass mc;
            ManagementObjectCollection moc;
            JObject item = (JObject)report["data"]["CPUUsagePollster"];
            item["timestamp"] = DateTime.Now;

            // Win32_Processor

            mc = new ManagementClass("Win32_Processor");
            moc = mc.GetInstances();

            uint TotalLoadPercentage = 0;
            uint count = 0;
            foreach (ManagementObject mo in moc)
            {
                UInt16 LoadPercentage = (UInt16) mo.Properties["LoadPercentage"].Value;
                TotalLoadPercentage += LoadPercentage;
                item["data"]["cpu" + count] = new ReportValue(LoadPercentage, "%");
                count++;
            }

            item["data"]["cpu"] = new ReportValue((TotalLoadPercentage/count), "%");
        }

        private void UpdateReportMemory() {
            ManagementClass mc;
            ManagementObjectCollection moc;
            JObject item = (JObject)report["data"]["MemInfoPollster"];
            item["timestamp"] = DateTime.Now;

            // Win32_ComputerSystem
            // Win32_OperatingSystem
            // Win32_PageFileUsage

            mc = new ManagementClass("Win32_OperatingSystem");
            moc = mc.GetInstances();


            foreach (ManagementObject mo in moc)
            {
                // http://msdn.microsoft.com/en-us/library/aa394239.aspx
                // TotalVirtualMemorySize, FreeVirtualMemory

                UInt64 TotalVisibleMemorySize = (UInt64)mo.Properties["TotalVisibleMemorySize"].Value; // Not TotalPhysicalMemory, BIOS and some device will reserve some for hardware
                UInt64 FreePhysicalMemory = (UInt64) mo.Properties["FreePhysicalMemory"].Value;
                UInt64 PagingFilesFree = (UInt64) mo.Properties["FreeSpaceInPagingFiles"].Value;
                UInt64 PagingFilesCached = (UInt64) mo.Properties["SizeStoredInPagingFiles"].Value;
                UInt64 PagingFilesTotal = PagingFilesFree + PagingFilesCached;

                item["data"]["MemTotal"] = new ReportValue((double) TotalVisibleMemorySize / 1024, "MB");
                item["data"]["MemFree"] = new ReportValue((double) FreePhysicalMemory / 1024, "MB");
                item["data"]["Buffers"] = new ReportValue(0.0, "MB");
                item["data"]["Cached"] = new ReportValue(0.0, "MB");
                item["data"]["SwapCached"] = new ReportValue((double) PagingFilesCached / 1024, "MB");
                item["data"]["SwapTotal"] = new ReportValue((double) PagingFilesTotal / 1024, "MB");
                item["data"]["SwapFree"] = new ReportValue((double) PagingFilesFree / 1024, "MB");
            }
        }


        private static void DefaultCallback(string data)
        {
            Console.WriteLine(data);
        }
    }
}
