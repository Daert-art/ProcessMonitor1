#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace ProcessMonitor.Utils
{
    public static class ProcessExtensions
    {
        private static readonly string TemporaryUserName = "<UPDATING>";
        private static readonly string TemporaryCpuUsage = "<CALCULATING>";
        private static readonly string TemporaryMemoryUsage = "<CALCULATING>";
        private static readonly string TemporaryDescription = "UPDATING";
        private static readonly Dictionary<int, string> userNamesCache = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> cpuUsageCache = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> memoryUsageCache = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> descriptionCache = new Dictionary<int, string>();

        public static string GetAssociatedUserName(this Process process)
        {
            if (!userNamesCache.TryGetValue(process.Id, out var userName))
            {
                userNamesCache[process.Id] = TemporaryUserName;
                Task.Run(() => InternalGetUserName(Convert.ToUInt32(process.Id)))
                    .ContinueWith(task =>
                    {
                        lock (userNamesCache)
                        {
                            userNamesCache[process.Id] = task.Result;
                        }
                    });
            }
            return userNamesCache[process.Id];
        }

        private static string InternalGetUserName(uint processId)
        {
            IntPtr hProcess = IntPtr.Zero, hToken = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(AccessRights.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                if (hProcess == IntPtr.Zero)
                    return "System";

                OpenProcessToken(hProcess, AccessRights.TOKEN_QUERY, out hToken);
                if (hToken == IntPtr.Zero)
                    return "System";

                var identity = new WindowsIdentity(hToken);
                return identity.Name;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error. GetUserNameForProcess.UpdateList: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
                if (hToken != IntPtr.Zero)
                    CloseHandle(hToken);
            }
        }

        public static string GetProcessStatus(this Process process)
        {
            try
            {
                if (process.Threads.Count > 0)
                {
                    ProcessThread mainThread = process.Threads[0];
                    if (mainThread.ThreadState == System.Diagnostics.ThreadState.Wait)
                    {
                        if (mainThread.WaitReason == ThreadWaitReason.Suspended)
                        {
                            return "Suspended";
                        }
                        return "Running";
                    }
                    return "Running";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting process status: {ex.Message}");
            }
            return "Unknown";
        }

        public static string GetProcessUsingCpu(this Process process)
        {
            if (!cpuUsageCache.TryGetValue(process.Id, out var cpuUsage))
            {
                cpuUsageCache[process.Id] = TemporaryCpuUsage;
                Task.Run(() => InternalGetProcessUsingCpu(process))
                    .ContinueWith(task =>
                    {
                         cpuUsageCache[process.Id] = task.Result;
                    });
            }
            return cpuUsageCache[process.Id];
           
        }

        private static async Task<string> InternalGetProcessUsingCpu(Process process)
        {
            try
            {
                using (var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true))
                {
                    Debug.WriteLine($"Getting CPU usage for process: {process.ProcessName}, ID: {process.Id}");
                    _ = cpuCounter.NextValue(); 
                    await Task.Delay(1000); 
                    float cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;
                    Debug.WriteLine($"CPU usage for process: {process.ProcessName}, ID: {process.Id} is {cpuUsage:F2}%");
                    return cpuUsage.ToString("F2") + " %";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to create performance counter for process {process.ProcessName}, id: {process.Id}. Reason: {ex.Message}");
                return "Unknown";
            }
        }
        public static string GetProcessMemoryUsage(this Process process)
        {
            if (!memoryUsageCache.TryGetValue(process.Id, out var memoryUsage))
            {
                memoryUsageCache[process.Id] = TemporaryMemoryUsage;
                Task.Run(() => InternalGetProcessMemoryUsage(process))
                    .ContinueWith(task =>
                    {
                        memoryUsageCache[process.Id] = task.Result;
                    });
            }
            return memoryUsageCache[process.Id];
        }

        private static async Task<string> InternalGetProcessMemoryUsage(Process process)
        {
            try
            {
                await Task.Delay(100);
                long memoryUsage = process.WorkingSet64;
                return $"{memoryUsage / 1024 / 1024} MB";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get memory usage for process {process.ProcessName}, id: {process.Id}. Reason: {ex.Message}");
                return "Unknown";
            }
        }
        public static string GetProcessDescription(this Process process)
        {
            if (!descriptionCache.TryGetValue(process.Id, out var description))
            {
                descriptionCache[process.Id] = TemporaryDescription;
                Task.Run(() => InternalGetProcessDescription(process))
                    .ContinueWith(task =>
                    {
                        descriptionCache[process.Id] = task.Result;
                    });                
            }
            return descriptionCache[process.Id];
        }
        private static string InternalGetProcessDescription(Process process)
        {
            string result = process.ProcessName;
            try
            {
                result = FileVersionInfo.GetVersionInfo(process.MainModule!.FileName).FileDescription ?? result;
            }
            catch
            {
            }
            return result;

        }

        [DllImport("Advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
    }

    public static class AccessRights
    {
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const uint TOKEN_QUERY = 0x0008;
    }
}
