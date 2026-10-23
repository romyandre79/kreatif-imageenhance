using System;
using System.IO;

namespace Fotocopy
{
    public static class DeviceServiceFactory
    {
        public static bool IsLinuxPlatform()
        {
            // Deteksi OS Linux baik di .NET Framework (Environment.OSVersion.Platform == PlatformID.Unix) 
            // maupun .NET Core / .NET 8 (RuntimeInformation.IsOSPlatform)
            int p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128) || Directory.Exists("/proc");
        }

        public static IScannerService CreateScannerService(IntPtr windowHandle)
        {
            if (IsLinuxPlatform())
            {
                return new LinuxSaneScannerService();
            }
            return new ScannerService(windowHandle);
        }

        public static IPrinterService CreatePrinterService()
        {
            if (IsLinuxPlatform())
            {
                return new LinuxCupsPrinterService();
            }
            return new PrinterService();
        }
    }
}
