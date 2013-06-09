﻿using System;
using System.CodeDom;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CSharp;
using Parallelity.OperatingSystem;

namespace Parallelity.Tasks
{
    public class MPITaskParams
    {
        [DisplayName("Procesy")]
        [Browsable(true), Category("MPI")]
        public int ProcessCount { get; set; }

        public MPITaskParams()
            : base()
        {
            ProcessCount = 5;
        }
    }

    public class MPITask : ParallelTimeTask
    {
        private static String TypeName(Type t)
        {
            CSharpCodeProvider compiler = new CSharpCodeProvider();
            CodeTypeReference type = new CodeTypeReference(t);

            return compiler.GetTypeOutput(type);
        }

        protected T[] InternalExecuteMPI<T>(
            byte[] kernelBinary,
            String function,
            int bufferSize,
            ParallelTaskParams loaderParams,
            params Object[] kernelParams) where T : struct
        {
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointStart);

            String binaryPath = Path.GetTempFileName();
            File.WriteAllBytes(binaryPath, kernelBinary);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformInit);

            Process mpirunProcess = new Process();
            mpirunProcess.StartInfo.CreateNoWindow = true;
            mpirunProcess.StartInfo.UseShellExecute = false;
            mpirunProcess.StartInfo.RedirectStandardOutput = true;
            mpirunProcess.StartInfo.EnvironmentVariables["PATH"] += @";C:\Program Files (x86)\OpenMPI_v1.5.5-x64\bin";
            mpirunProcess.StartInfo.FileName = SystemArchitecture.ProgramFolder(ArchitectureType.x86, @"OpenMPI*") + @"\bin\mpirun.exe";
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelBuild);

            mpirunProcess.StartInfo.Arguments = String.Format("-n {0} \"{1}\" {2} {3} {4}",
                loaderParams.ProcessCount,
                binaryPath,
                TypeName(typeof(T)),
                function,
                bufferSize);

            foreach (Object param in kernelParams)
                mpirunProcess.StartInfo.Arguments += " " + param.ToString();

            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceWrite);

            mpirunProcess.Start();
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointKernelExecute);

            using (MemoryStream resultStream = new MemoryStream())
            {
                mpirunProcess.StandardOutput.BaseStream.CopyTo(resultStream);
                TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointDeviceRead);

                byte[] processOutput = resultStream.ToArray();
                T[] result = new T[(int)Math.Ceiling((float)processOutput.Length / Marshal.SizeOf(typeof(T)))];
                Buffer.BlockCopy(processOutput, 0, result, 0, processOutput.Length);
                TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointPlatformDeinit);

                return result;
            }
        }
    }
}
