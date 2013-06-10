using System;
using System.Collections.Generic;
using System.Linq;
using Cloo;

namespace Parallelity.Converters
{
    public static class OpenCLDevices
    {
        public static List<ComputeDevice> All
        {
            get
            {
                try
                {
                    return ComputePlatform.Platforms
                        .SelectMany(platform => platform.Devices)
                        .ToList();
                }
                catch (Exception)
                {
                    return new List<ComputeDevice>();
                }
            }
        }
    }

    public class OpenCLDeviceTypeConverter : GenericTypeConverter<ComputeDevice>
    {
        protected override List<ComputeDevice> GetAll()
        {
            return OpenCLDevices.All;
        }

        protected override String GetName(ComputeDevice instance)
        {
            return instance.Name.Trim();
        }
    }
}
