using System;
using System.Collections.Generic;
using System.Linq;

namespace Parallelity.Tasks
{
    public enum ParallelPlatform
    {
        PlatformMPI,
        PlatformCUDA,
        PlatformOpenCL
    }

    public static class ParallelPlatformExtension
    {
        public static String DisplayName(this ParallelPlatform platform)
        {
            return Enum
                .GetName(typeof(ParallelPlatform), platform)
                .Replace("Platform", String.Empty);
        }

        public static ParallelPlatform Parse(String displayName)
        {
            return Enum
                .GetValues(typeof(ParallelPlatform))
                .Cast<ParallelPlatform>()
                .Where(platform => platform.DisplayName().Equals(displayName))
                .SingleOrDefault();
        }

        public static List<ParallelPlatform> Platforms
        {
            get
            {
                return Enum
                    .GetValues(typeof(ParallelPlatform))
                    .Cast<ParallelPlatform>()
                    .ToList();
            }
        }
    }

    public class ParallelTaskParams : CUDATaskParams
    {
    }

    public abstract class ParallelTask<InType, OutType> : CUDATask
        where InType : ParallelTaskParams, new()
    {
        public ParallelTask()
        {
            Params = new InType();
            Result = default(OutType);
        }

        public OutType Run(ParallelPlatform platform, InType p = null)
        {
            switch (platform)
            {
                case ParallelPlatform.PlatformMPI:
                    return Result = RunMpi(p ?? Params);
                case ParallelPlatform.PlatformCUDA:
                    return Result = RunCuda(p ?? Params);
                case ParallelPlatform.PlatformOpenCL:
                    return Result = RunOpencl(p ?? Params);
                default:
                    throw new NotImplementedException("Platform " + platform + " is not implemented.");
            }
        }

        public InType Params { get; set; }
        public OutType Result { get; private set; }

        protected abstract OutType RunMpi(InType p);
        protected abstract OutType RunCuda(InType p);
        protected abstract OutType RunOpencl(InType p);
    }
}
