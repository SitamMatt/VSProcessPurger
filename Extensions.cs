using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessPurger.Extensions
{
    public static class Extensions
    {
        public static Process GetParent(this Process process)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                int parentPid;
                using (ManagementObject mo = new ManagementObject($"win32_process.handle='{process.Id}'"))
                {
                    mo.Get();
                    parentPid = Convert.ToInt32(mo["ParentProcessId"]);
                }
                return Process.GetProcessById(parentPid);
            }
            return null;
        }
    }
}