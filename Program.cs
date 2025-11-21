using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class SysInfoWin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;           // Размер структуры в байтах
        public uint dwMemoryLoad;       // Процент использования памяти 
        public ulong ullTotalPhys;      // Общий объем физической памяти в байтах
        public ulong ullAvailPhys;      // Доступная физическая память в байтах
        public ulong ullTotalPageFile;  // Общий размер файла подкачки в байтах
        public ulong ullAvailPageFile;  // Доступный размер файла подкачки в байтах
        public ulong ullTotalVirtual;   // Общая виртуальная память в байтах
        public ulong ullAvailVirtual;   // Доступная виртуальная память в байтах
        public ulong ullAvailExtendedVirtual; // Расширенная виртуальная память
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);


    public void PrintSystemInfo()
    {
        string osName = Environment.OSVersion.ToString();
        Console.WriteLine("OS: " + osName);

        string computerName = Environment.MachineName;
        Console.WriteLine("Computer Name: " + computerName);

        Console.WriteLine("User: " + Environment.UserName);

        string arch = Environment.Is64BitOperatingSystem ? "x64 (AMD64)" : "x86";
        Console.WriteLine("Architecture: " + arch);

        PrintFormattedMemoryInfo();

        Console.WriteLine("\nProcessors: " + Environment.ProcessorCount);

        Console.WriteLine("\nDrives:");
        DriveInfo[] drives = DriveInfo.GetDrives();
        foreach (DriveInfo drive in drives)
        {
            if (drive.IsReady)
            {
                long totalGB = drive.TotalSize / (1024 * 1024 * 1024);
                long freeGB = drive.TotalFreeSpace / (1024 * 1024 * 1024);
                string driveType = GetDriveType(drive);
                Console.WriteLine($"  - {drive.Name}  ({driveType}): {freeGB} GB free / {totalGB} GB total");
            }
        }
    }

    private void PrintFormattedMemoryInfo()
    {
        try
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                long totalMB = (long)(memStatus.ullTotalPhys / (1024 * 1024));
                long freeMB = (long)(memStatus.ullAvailPhys / (1024 * 1024));
                long usedMB = totalMB - freeMB;
                int memoryLoad = (int)memStatus.dwMemoryLoad;

                long totalPageMB = (long)(memStatus.ullTotalPageFile / (1024 * 1024));
                long freePageMB = (long)(memStatus.ullAvailPageFile / (1024 * 1024));
                long usedPageMB = totalPageMB - freePageMB;

                Console.WriteLine("RAM: " + usedMB + "MB / " + totalMB + "MB");
                Console.WriteLine("Virtual Memory: " + totalPageMB + "MB");
                Console.WriteLine("Memory Load: " + memoryLoad + "%");
                Console.WriteLine("Pagefile: " + usedPageMB + "MB / " + totalPageMB + "MB");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error retrieving memory info: " + ex.Message);
        }
    }

    private string GetDriveType(DriveInfo drive)
    {
        return drive.DriveFormat;
    }

    public static void Main(string[] args)
    {
        SysInfoWin sysInfo = new SysInfoWin();
        sysInfo.PrintSystemInfo();
    }
}