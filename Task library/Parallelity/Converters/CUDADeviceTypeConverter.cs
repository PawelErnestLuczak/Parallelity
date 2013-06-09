using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedCuda;

namespace Parallelity.Converters
{
    public static class CUDADevices
    {
        public static List<CudaDeviceProperties> All
        {
            get
            {
                return Enumerable.Range(0, CudaContext.GetDeviceCount())
                    .Select(i => CudaContext.GetDeviceInfo(i))
                    .ToList();
            }
        }
    }

    public class CUDADeviceTypeConverter : GenericTypeConverter<CudaDeviceProperties>
    {
        protected override List<CudaDeviceProperties> GetAll()
        {
            return CUDADevices.All;
        }

        protected override String GetName(CudaDeviceProperties instance)
        {
            return instance.DeviceName.Trim();
        }
    }
}
