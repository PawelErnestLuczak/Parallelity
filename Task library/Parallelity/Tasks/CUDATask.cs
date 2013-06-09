using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using ManagedCuda;
using ManagedCuda.VectorTypes;
using Parallelity.Converters;
using Parallelity.OperatingSystem;

namespace Parallelity.Tasks
{
    public class CUDATaskParams : OpenCLTaskParams
    {
        [DisplayName("Rozmiar bloku")]
        [Browsable(true), Category("CUDA")]
        public Size BlockSize { get; set; }

        [DisplayName("Rozmiar siatki")]
        [Browsable(true), Category("CUDA")]
        public Size GridSize { get; set; }

        [DisplayName("Architektura")]
        [Browsable(true), Category("CUDA")]
        public ArchitectureType Architecture { get; set; }

        [DisplayName("Urządzenie")]
        [Browsable(true), Category("CUDA")]
        [TypeConverter(typeof(CUDADeviceTypeConverter))]
        public CudaDeviceProperties CudaDevice { get; set; }

        public CUDATaskParams()
            : base()
        {
            BlockSize = new Size(1, 1);
            GridSize = new Size(1, 1);
            Architecture = ArchitectureType.x64;
            CudaDevice = CUDADevices.All.FirstOrDefault();
        }
    }

    public class CUDATask : OpenCLTask
    {
        private static List<Tuple<Object, IDisposable>> WrapDeviceVariables(Object[] kernelParams, bool arraise)
        {
            return kernelParams.Select(obj =>
            {
                dynamic variable = null;

                if (EnclosesInType<int>(obj))
                {
                    if (obj.GetType().IsArray || arraise)
                        variable = (CudaDeviceVariable<int>)EncloseInType<int>(obj);
                }
                else if (EnclosesInType<float>(obj))
                {
                    if (obj.GetType().IsArray || arraise)
                        variable = (CudaDeviceVariable<float>)EncloseInType<float>(obj);
                }
                else if (EnclosesInType<char>(obj))
                {
                    if (obj.GetType().IsArray || arraise)
                        variable = (CudaDeviceVariable<char>)EncloseInType<char>(obj);
                }
                else
                    throw new NotImplementedException("Type " + obj.GetType() + " is unhandled.");

                if (variable != null)
                    return new Tuple<Object, IDisposable>(variable.DevicePointer, variable);
                else
                    return new Tuple<Object, IDisposable>(obj, null);
            }).ToList();
        }

        private static CudaContext ContextWithDevice(CudaDeviceProperties device)
        {
            int deviceIndex = Enumerable
                .Range(0, CudaContext.GetDeviceCount())
                .Where(i => CudaContext.GetDeviceInfo(i).Equals(device))
                .FirstOrDefault();

            return new CudaContext(deviceIndex);
        }

        protected T[] InternalExecuteCuda<T>(
            byte[] kernelBinary,
            String function,
            int bufferSize,
            ParallelTaskParams loaderParams,
            params Object[] kernelParams) where T : struct
        {
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointStart);

            CudaContext context = ContextWithDevice(loaderParams.CudaDevice);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformInit);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelBuild);

            CudaDeviceVariable<T> resultBufferVar = new CudaDeviceVariable<T>(bufferSize);
            resultBufferVar.Memset(0);

            List<Tuple<Object, IDisposable>> vars = new List<Tuple<Object, IDisposable>>();
            vars.Add(new Tuple<Object, IDisposable>(resultBufferVar.DevicePointer, resultBufferVar));
            vars.AddRange(WrapDeviceVariables(kernelParams, true));
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceWrite);

            CudaKernel kernel = context.LoadKernelPTX(kernelBinary, function);
            kernel.BlockDimensions = new dim3(loaderParams.BlockSize.Width, loaderParams.BlockSize.Height);
            kernel.GridDimensions = new dim3(loaderParams.GridSize.Width, loaderParams.GridSize.Height);
            kernel.Run(vars.Select(tuple => tuple.Item1).ToArray());
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelExecute);

            T[] resultBuffer = resultBufferVar;
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceRead);

            vars.Where(tuple => tuple.Item2 != null).ToList().ForEach(tuple => tuple.Item2.Dispose());
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformDeinit);

            return resultBuffer;
        }
    }
}
