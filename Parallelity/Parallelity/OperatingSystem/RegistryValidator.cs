using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Parallelity.OperatingSystem
{
    public static class RegistryValidator
    {
        private static List<Tuple<String, String, Object>> keys = new List<Tuple<String, String, Object>>()
        {
            new Tuple<String, String, Object>(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrLevel", 0),
            new Tuple<String, String, Object>(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\DCI", "Timeout", 0)
        };

        public static bool IsRegistryCompatible()
        {
            bool isCompatible = true;

            foreach (Tuple<String, String, Object> tuple in keys)
            {
                if (!Registry.GetValue(tuple.Item1, tuple.Item2, tuple.Item3).Equals(tuple.Item3))
                {
                    isCompatible = false;
                    break;
                }
            }

            return isCompatible;
        }

        public static void FixRegistry()
        {
            foreach (Tuple<String, String, Object> tuple in keys)
            {
                if (!Registry.GetValue(tuple.Item1, tuple.Item2, tuple.Item3).Equals(tuple.Item3))
                    Registry.SetValue(tuple.Item1, tuple.Item2, tuple.Item3);
            }
        }
    }
}
