using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Cloo;
using OpenCLTemplate;
using Parallelity.Converters;
using System.Drawing;

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

        private static CLCalc.Program.Variable[] WrapDeviceVariables(Object[] kernelParams)
        {
            return kernelParams.Select(obj =>
            {
                if (EnclosesInType<int>(obj))
                    return new CLCalc.Program.Variable(EncloseInType<int>(obj));
                else if (EnclosesInType<float>(obj))
                    return new CLCalc.Program.Variable(EncloseInType<float>(obj));
                else if (EnclosesInType<char>(obj))
                    return new CLCalc.Program.Variable(EncloseInType<char>(obj));
                else
                    throw new NotImplementedException("Type " + obj.GetType() + " is unhandled.");
            }).ToArray();
        }

        private static T[] ReadResultBuffer<T>(CLCalc.Program.Variable var, int resultBufferSize)
        {
            Object buffer = new T[resultBufferSize];
            if (typeof(T) == typeof(int))
                var.ReadFromDeviceTo((int[])buffer);
            else if (typeof(T) == typeof(float))
                var.ReadFromDeviceTo((float[])buffer);
            else if (typeof(T) == typeof(char))
                var.ReadFromDeviceTo((char[])buffer);
            else
                throw new NotImplementedException("Type " + typeof(T) + " is unhandled.");

            return (T[])buffer;
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
        {
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointStart);

            ComputeCommandQueue queue = QueueWithDevice(loaderParams.OpenCLDevice);
            CLCalc.InitCL(ComputeDeviceTypes.Gpu, queue.Context, queue);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformInit);

            source = "#define OpenCL\r\n" + source;
            CLCalc.Program.Compile(source);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelBuild);

            CLCalc.Program.Variable resultBufferVar = new CLCalc.Program.Variable(typeof(T), bufferSize);
            List<CLCalc.Program.Variable> vars = new List<CLCalc.Program.Variable>();
            vars.Add(resultBufferVar);
            vars.AddRange(WrapDeviceVariables(kernelParams));
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceWrite);

            int[] workersGlobal = new int[2] { loaderParams.GlobalWorkers.Width, loaderParams.GlobalWorkers.Height };
            CLCalc.Program.Kernel kernel = new CLCalc.Program.Kernel(function);
            kernel.Execute(vars.ToArray(), workersGlobal, new int[2] { 1, 1 });
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelExecute);

            T[] resultBuffer = ReadResultBuffer<T>(resultBufferVar, bufferSize);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceRead);

            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformDeinit);

            return resultBuffer;
        }
    }
}
