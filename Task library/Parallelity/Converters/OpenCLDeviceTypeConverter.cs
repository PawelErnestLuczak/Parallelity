using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using Cloo;

namespace Parallelity.Converters
{
    public static class OpenCLDevices
    {
        public static List<ComputeDevice> All
        {
            get
            {
                return ComputePlatform.Platforms
                    .SelectMany(platform => platform.Devices)
                    .ToList();
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
