using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Cloo;
using Parallelity.Converters;

namespace Parallelity.Tasks
{
    public class OpenCLTaskParams : MPITaskParams
    {
        [DisplayName("Robotnicy globalni")]
        [Browsable(true), Category("OpenCL")]
        public Size GlobalWorkers { get; set; }

        [DisplayName("Urządzenie")]
        [Browsable(true), Category("OpenCL")]
        [TypeConverter(typeof(OpenCLDeviceTypeConverter))]
        public ComputeDevice OpenCLDevice { get; set; }

        public OpenCLTaskParams()
            : base()
        {
            OpenCLDevice = OpenCLDevices.All.FirstOrDefault();
            GlobalWorkers = new Size(50, 50);
        }
    }

    public class OpenCLTask : MPITask
    {
        protected static bool EnclosesInType<T>(Object obj)
        {
            return obj.GetType().Equals(typeof(T)) || obj.GetType().MakeArrayType().Equals(typeof(T));
        }

        protected static T[] EncloseInType<T>(Object obj)
        {
            return obj.GetType().Equals(typeof(T)) ?
                new T[] { (T)obj } :
                (T[])obj;
        }

        private static ComputeMemory[] WrapDeviceVariables(Object[] kernelParams, ComputeContext context)
        {
            ComputeMemoryFlags flags = ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer;

            return kernelParams.Select<Object, ComputeMemory>(obj =>
            {
                if (EnclosesInType<int>(obj))
                    return new ComputeBuffer<int>(context, flags, EncloseInType<int>(obj));
                else if (EnclosesInType<float>(obj))
                    return new ComputeBuffer<float>(context, flags, EncloseInType<float>(obj));
                else if (EnclosesInType<char>(obj))
                    return new ComputeBuffer<char>(context, flags, EncloseInType<char>(obj));
                else
                    throw new NotImplementedException("Type " + obj.GetType() + " is unhandled.");
            }).ToArray();
        }

        private static ComputeCommandQueue QueueWithDevice(ComputeDevice device)
        {
            List<ComputeDevice> devices = new List<ComputeDevice>(new ComputeDevice[] { device });
            ComputePlatform platform = device.Platform;
            ComputeContextPropertyList properties = new ComputeContextPropertyList(platform);
            ComputeContext context = new ComputeContext(device.Type, properties, null, IntPtr.Zero);
            return new ComputeCommandQueue(context, context.Devices.FirstOrDefault(), context.Devices.FirstOrDefault().CommandQueueFlags);
        }

        protected T[] InternalExecuteOpencl<T>(
            String source,
            String function,
            int bufferSize,
            ParallelTaskParams loaderParams,
            params Object[] kernelParams)
            where T : struct
        {
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointStart);

            ComputeCommandQueue queue = QueueWithDevice(loaderParams.OpenCLDevice);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformInit);

            String updatedSource = "#define OpenCL\r\n" + source;
            ComputeProgram program = new ComputeProgram(queue.Context, updatedSource);
            program.Build(new ComputeDevice[] { queue.Device }, null, null, IntPtr.Zero);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelBuild);

            T[] resultBuffer = new T[bufferSize];

            ComputeBuffer<T> resultBufferVar = new ComputeBuffer<T>(queue.Context, ComputeMemoryFlags.WriteOnly, bufferSize);
            List<ComputeMemory> vars = new List<ComputeMemory>();
            vars.Add(resultBufferVar);
            vars.AddRange(WrapDeviceVariables(kernelParams, queue.Context));

            ComputeKernel kernel = program.CreateKernel(function);

            for (int i = 0; i < vars.Count; i++)
                kernel.SetMemoryArgument(i, vars[i]);

            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceWrite);

            long[] workersGlobal = new long[2] { loaderParams.GlobalWorkers.Width, loaderParams.GlobalWorkers.Height };
            queue.Execute(kernel, null, workersGlobal, null, null);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelExecute);

            queue.ReadFromBuffer<T>(resultBufferVar, ref resultBuffer, false, null);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceRead);

            queue.Finish();
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformDeinit);

            return resultBuffer;
        }
    }
}
