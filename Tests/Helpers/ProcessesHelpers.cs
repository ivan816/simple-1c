using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Tests.Helpers
{
    public static class ProcessesHelpers
    {
        public static void KillOwnProcessesByName(string name)
        {
            var processes = GetOwnPidsByName(name)
                .Select(GetProcessById)
                .Where(x => x != null);
            foreach (var process in processes)
                KillProcess(process);
        }

        private static IEnumerable<int> GetOwnPidsByName(string name)
        {
            var currentUserName = WindowsIdentity.GetCurrent().Name;
            var query = "select Handle, ProcessId from Win32_Process where Name = '" + name + "'";
            using (var searcher = new ManagementObjectSearcher(new SelectQuery(query)))
            using (var processes = searcher.Get())
            {
                return processes.Cast<ManagementObject>()
                    .Select(process => new
                    {
                        ProcessId = Convert.ToInt32((uint) process["ProcessId"]),
                        User = GetUser(process)
                    })
                    .Where(x => x.User != null)
                    .Where(x => x.User.Is(currentUserName))
                    .Select(x => x.ProcessId)
                    .ToList();
            }
        }

        private static User GetUser(ManagementObject process)
        {
            var outParameters = new object[2];
            var getOwnerResult = (uint) process.InvokeMethod("GetOwner", outParameters);
            return getOwnerResult == 0 ? new User((string) outParameters[1], (string) outParameters[0]) : null;
        }

        private static void KillProcess(Process process)
        {
            try
            {
                process.Kill();
            }
            catch
            {
                Console.WriteLine("Cannot kill process {0}/{1}", process.ProcessName, process.Id);
            }
        }

        private static Process GetProcessById(int x)
        {
            try
            {
                return Process.GetProcessById(x);
            }
            catch
            {
                return null;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public readonly IntPtr ExitStatus;
            public readonly IntPtr PebBaseAddress;
            public readonly IntPtr AffinityMask;
            public readonly IntPtr BasePriority;
            public readonly UIntPtr UniqueProcessId;
            public readonly IntPtr InheritedFromUniqueProcessId;

            public int Size
            {
                get { return Marshal.SizeOf(typeof (PROCESS_BASIC_INFORMATION)); }
            }
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength, out int returnLength);

        //тут баг, не помню какой, фиксил в диадоке, содрать
        public static int GetParentProcessId(IntPtr handle)
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            int sizeInfoReturned;
            var status = NtQueryInformationProcess(handle, 0, ref pbi, pbi.Size, out sizeInfoReturned);
            if (status != 0)
                throw new Win32Exception(status);
            return (int) pbi.InheritedFromUniqueProcessId;
        }


        private class User
        {
            private readonly string domain;
            private readonly string name;

            public User(string domain, string name)
            {
                this.domain = domain;
                this.name = name;
            }

            public bool Is(string userFullName)
            {
                return string.Equals(userFullName, string.Join("\\", domain, name),
                    StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}