using System;
using System.Collections.Generic;

namespace Parallelity.Tasks
{
    public enum ParallelExecutionCheckpointType
    {
        CheckpointStart,
        CheckpointPlatformInit,
        CheckpointKernelBuild,
        CheckpointDeviceWrite,
        CheckpointKernelExecute,
        CheckpointDeviceRead,
        CheckpointPlatformDeinit,
        CheckpointResultPostProcess
    }

    public static class ParallelExecutionCheckpointTypeExtension
    {
        public static String DisplayName(this ParallelExecutionCheckpointType checkpoint)
        {
            switch (checkpoint)
            {
                case ParallelExecutionCheckpointType.CheckpointStart:
                    return "rozpoczęto przetwarzanie";
                case ParallelExecutionCheckpointType.CheckpointPlatformInit:
                    return "zinicjalizowano platformę";
                case ParallelExecutionCheckpointType.CheckpointKernelBuild:
                    return "skompilowano jądro";
                case ParallelExecutionCheckpointType.CheckpointDeviceWrite:
                    return "zapisano pamięć";
                case ParallelExecutionCheckpointType.CheckpointKernelExecute:
                    return "uruchomiono jądro";
                case ParallelExecutionCheckpointType.CheckpointDeviceRead:
                    return "odczytano pamięć";
                case ParallelExecutionCheckpointType.CheckpointPlatformDeinit:
                    return "uwolniono zasoby";
                case ParallelExecutionCheckpointType.CheckpointResultPostProcess:
                    return "przetworzono wyniki";
                default:
                    throw new NotImplementedException("Checkpoint type " + checkpoint + " is not implemented.");
            }
        }
    }

    public class ParallelExecutionCheckpoint
    {
        public DateTime Date { get; private set; }
        public TimeSpan Elapsed { get; private set; }

        public ParallelExecutionCheckpoint()
        {
            Date = DateTime.Now;
            Elapsed = TimeSpan.Zero;
        }

        public ParallelExecutionCheckpoint(DateTime zeroDate)
        {
            Date = DateTime.Now;
            Elapsed = Date - zeroDate;
        }
    }

    public class ParallelCheckpoints : Dictionary<ParallelExecutionCheckpointType, ParallelExecutionCheckpoint>
    {
    }
    
    public class ParallelTimeTask
    {
        public event Action<ParallelTimeTask, ParallelExecutionCheckpointType> CheckpointTriggered;
        private ParallelCheckpoints Checkpoints;

        public ParallelTimeTask()
        {
            Checkpoints = new ParallelCheckpoints();
        }

        protected void TriggerCheckpoint(ParallelExecutionCheckpointType checkpoint)
        {
            if (checkpoint == ParallelExecutionCheckpointType.CheckpointStart)
                Checkpoints.Clear();

            Checkpoints[checkpoint] = Checkpoints.ContainsKey(ParallelExecutionCheckpointType.CheckpointStart) ?
                new ParallelExecutionCheckpoint(Checkpoints[ParallelExecutionCheckpointType.CheckpointStart].Date) :
                new ParallelExecutionCheckpoint();

            if (CheckpointTriggered != null)
                CheckpointTriggered(this, checkpoint);
        }

        public TimeSpan this[ParallelExecutionCheckpointType checkpoint]
        {
            get
            {
                return Checkpoints.ContainsKey(checkpoint) ?
                    Checkpoints[checkpoint].Elapsed :
                    TimeSpan.Zero;
            }
        }
    }
}
