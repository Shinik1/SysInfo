using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class SysInfoLinux
{
    // Структура для хранения информации о системе из sysinfo()
    [StructLayout(LayoutKind.Sequential)]
    struct SysInfo_t
    {
        public long uptime;             // Время работы системы в секундах
        public ulong loads1;            // Загрузка системы за 1 минуту
        public ulong loads5;            // Загрузка системы за 5 минут  
        public ulong loads15;           // Загрузка системы за 15 минут
        public ulong totalram;          // Общая оперативная память
        public ulong freeram;           // Свободная оперативная память
        public ulong sharedram;         // Общая память
        public ulong bufferram;         // Память для буферов
        public ulong totalswap;         // Общий swap
        public ulong freeswap;          // Свободный swap
        public ushort procs;            // Количество процессов
        public ulong totalhigh;         // Общая high-память
        public ulong freehigh;          // Свободная high-память
        public uint mem_unit;           // Единица измерения памяти
    }

    // Структура для информации о системе из uname()
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct Utsname
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
        public string sysname;          // Название ОС
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
        public string nodename;         // Имя хоста
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
        public string release;          // Версия ядра
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
        public string version;          // Версия ОС
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
        public string machine;          // Архитектура
    }

    // Импорт функций из библиотеки libc
    [DllImport("libc")]
    static extern int uname(IntPtr buf);  // Получить информацию о системе

    [DllImport("libc")]
    static extern int sysinfo(ref SysInfo_t info);  // Получить системную статистику

    [DllImport("libc")]
    static extern int gethostname(byte[] name, int len);  // Получить имя хоста

    [DllImport("libc")]
    static extern uint getuid();  // Получить ID пользователя

    [DllImport("libc")]
    static extern IntPtr getpwuid(uint uid);  // Получить информацию о пользователе

    // Главный метод программы
    static void Main()
    {
        // Создаем объект и выводим информацию о системе
        new SysInfoLinux().PrintSystemInfo();
    }

    // Основной метод для вывода всей системной информации
    public void PrintSystemInfo()
    {
        // Информация об операционной системе
        Console.WriteLine("OS: " + GetOSInfo());           // Название дистрибутива
        Console.WriteLine("Kernel: " + GetKernelInfo());   // Информация о ядре
        Console.WriteLine("Architecture: " + GetArchitecture());  // Архитектура процессора
        Console.WriteLine("Hostname: " + GetHostname());   // Имя компьютера
        Console.WriteLine("User: " + GetUserName());       // Текущий пользователь

        // Информация о памяти
        PrintMemoryInfo();                                 // ОЗУ и swap
        Console.WriteLine("Virtual memory: " + GetVirtualMemory() + " MB");  // Виртуальная память

        // Информация о процессоре
        Console.WriteLine("Processors: " + Environment.ProcessorCount);  // Количество ядер
        Console.WriteLine("Load average: " + GetLoadAverage());          // Загрузка системы

        // Информация о дисках
        Console.WriteLine("\nDrives:");
        PrintDrivesInfo();                                 // Список дисков
    }

    // Метод для получения названия дистрибутива Linux
    private string GetOSInfo()
    {
        try
        {
            // Читаем все строки из файла с информацией об ОС
            string[] lines = File.ReadAllLines("/etc/os-release");
            string foundLine = null;

            // Ищем строку с названием дистрибутива
            foreach (string line in lines)
            {
                if (line.StartsWith("PRETTY_NAME="))
                {
                    foundLine = line;
                    break;
                }
            }

            // Если нашли строку, извлекаем значение
            if (foundLine != null)
            {
                string[] parts = foundLine.Split('=');
                if (parts.Length >= 2)
                {
                    string value = parts[1];
                    // Убираем кавычки если они есть
                    if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    return value;
                }
            }
        }
        catch
        {
            // Если произошла ошибка при чтении файла
        }
        return "Unknown";
    }

    // Метод для получения информации о ядре Linux
    private string GetKernelInfo()
    {
        Utsname uts = GetUname();
        return uts.sysname + " " + uts.release;
    }

    // Метод для получения архитектуры процессора
    private string GetArchitecture()
    {
        Utsname uts = GetUname();
        return uts.machine;
    }

    // Метод для получения имени хоста компьютера
    private string GetHostname()
    {
        try
        {
            byte[] buffer = new byte[256];
            // Вызываем системную функцию для получения имени хоста
            if (gethostname(buffer, buffer.Length) == 0)
            {
                string hostname = System.Text.Encoding.ASCII.GetString(buffer);
                // Убираем нулевые символы в конце
                int nullIndex = hostname.IndexOf('\0');
                if (nullIndex >= 0)
                {
                    hostname = hostname.Substring(0, nullIndex);
                }
                return hostname;
            }
        }
        catch
        {
            // Ошибка при получении имени хоста
        }
        return "Unknown";
    }

    // Метод для получения имени текущего пользователя
    private string GetUserName()
    {
        try
        {
            // Получаем ID пользователя и информацию о нем
            uint uid = getuid();
            IntPtr pwd = getpwuid(uid);
            if (pwd != IntPtr.Zero)
            {
                return Environment.UserName;
            }
        }
        catch
        {
            // Ошибка при получении информации о пользователе
        }
        return Environment.UserName;
    }

    // Метод для вывода информации об оперативной памяти и swap
    private void PrintMemoryInfo()
    {
        try
        {
            SysInfo_t info = new SysInfo_t();
            // Получаем информацию о памяти через sysinfo()
            if (sysinfo(ref info) == 0)
            {
                // Переводим байты в мегабайты
                long totalRamMB = (long)(info.totalram * info.mem_unit) / (1024 * 1024);
                long freeRamMB = (long)(info.freeram * info.mem_unit) / (1024 * 1024);

                Console.WriteLine("RAM: " + freeRamMB + "MB free / " + totalRamMB + "MB total");

                // Выводим информацию о swap если он используется
                if (info.totalswap > 0)
                {
                    long totalSwapMB = (long)(info.totalswap * info.mem_unit) / (1024 * 1024);
                    long freeSwapMB = (long)(info.freeswap * info.mem_unit) / (1024 * 1024);
                    Console.WriteLine("Swap: " + totalSwapMB + "MB total / " + freeSwapMB + "MB free");
                }
            }
        }
        catch
        {
            Console.WriteLine("Memory information unavailable");
        }
    }

    // Метод для получения средней загрузки системы
    private string GetLoadAverage()
    {
        // Первый способ: через sysinfo
        try
        {
            SysInfo_t info = new SysInfo_t();
            if (sysinfo(ref info) == 0)
            {
                // Значения загрузки хранятся умноженными на 65536
                double load1 = (double)info.loads1 / 65536;
                double load5 = (double)info.loads5 / 65536;
                double load15 = (double)info.loads15 / 65536;
                return load1.ToString("F2") + ", " + load5.ToString("F2") + ", " + load15.ToString("F2");
            }
        }
        catch
        {
            // Ошибка при получении через sysinfo
        }

        // Второй способ: читаем из файла /proc/loadavg
        try
        {
            string loadavgText = File.ReadAllText("/proc/loadavg");
            string[] parts = loadavgText.Split(' ');
            if (parts.Length >= 3)
            {
                return parts[0] + ", " + parts[1] + ", " + parts[2];
            }
        }
        catch
        {
            // Ошибка при чтении файла
        }
        return "Unknown";
    }

    // Метод для получения информации о виртуальной памяти
    private string GetVirtualMemory()
    {
        try
        {
            // Читаем информацию о памяти из файла
            string[] lines = File.ReadAllLines("/proc/meminfo");
            foreach (string line in lines)
            {
                // Ищем строку с общей виртуальной памятью
                if (line.StartsWith("VmallocTotal:"))
                {
                    // Извлекаем только цифры из строки
                    string numbers = "";
                    foreach (char c in line)
                    {
                        if (char.IsDigit(c))
                        {
                            numbers += c;
                        }
                    }

                    // Конвертируем в число и переводим в мегабайты
                    if (long.TryParse(numbers, out long value))
                    {
                        return (value / 1024).ToString();
                    }
                    break;
                }
            }
        }
        catch
        {
            // Ошибка при чтении файла
        }
        return "Unknown";
    }

    // Метод для вывода информации о жестких дисках
    private void PrintDrivesInfo()
    {
        try
        {
            // Получаем список всех дисков
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                // Показываем только готовые к работе фиксированные диски
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    long totalGB = drive.TotalSize / (1024 * 1024 * 1024);
                    long freeGB = drive.TotalFreeSpace / (1024 * 1024 * 1024);
                    Console.WriteLine("  " + drive.RootDirectory + "     " + drive.DriveFormat + "     " + freeGB + "GB free / " + totalGB + "GB total");
                }
            }
        }
        catch
        {
            Console.WriteLine("  Drives information unavailable");
        }
    }

    // Вспомогательный метод для получения структуры Utsname
    private Utsname GetUname()
    {
        // Выделяем память для буфера
        IntPtr buf = Marshal.AllocHGlobal(1024);
        try
        {
            // Вызываем uname() для получения информации о системе
            if (uname(buf) == 0)
            {
                return Marshal.PtrToStructure<Utsname>(buf);
            }
        }
        finally
        {
            // Всегда освобождаем память
            Marshal.FreeHGlobal(buf);
        }
        return new Utsname();
    }
}