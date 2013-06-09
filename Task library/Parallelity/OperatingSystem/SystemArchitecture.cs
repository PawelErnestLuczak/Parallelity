using System;
using System.IO;
using System.Linq;

namespace Parallelity.OperatingSystem
{
    public enum ArchitectureType
    {
        x86,
        x64
    }

    public static class SystemArchitecture
    {
        public static ArchitectureType Type
        {
            get
            {
                return (8 == IntPtr.Size ||
                    !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
                    ? ArchitectureType.x64 : ArchitectureType.x86;
            }
        }

        public static String ProgramFilesx86
        {
            get
            {
                if (Type == ArchitectureType.x64)
                    return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                else
                    return Environment.GetEnvironmentVariable("ProgramFiles");
            }
        }

        public static String ProgramFilesx64
        {
            get
            {
                if (Type == ArchitectureType.x64)
                    return Environment.GetEnvironmentVariable("ProgramFiles");
                else
                    throw new InvalidOperationException("Program Files (x64) is unavailable on current 32-bit system.");
            }
        }

        public static String ProgramFolder(ArchitectureType type, String pattern)
        {
            String path = (type == ArchitectureType.x86) ? ProgramFilesx86 : ProgramFilesx64;
            return Directory.GetDirectories(path, pattern).FirstOrDefault();
        }
    }
}
