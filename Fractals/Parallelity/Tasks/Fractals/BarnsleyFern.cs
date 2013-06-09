using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using Fractals.Properties;
using Parallelity.Converters;
using Parallelity.Drawing;
using Parallelity.OperatingSystem;

namespace Parallelity.Tasks.Fractals
{
    public class BarnsleyFernParams : ParallelTaskParams
    {
        [DisplayName("Rozmiar")]
        [Browsable(true), Category("Paproć Barnsley'a")]
        public Size Size { get; set; }

        [DisplayName("Iteracje")]
        [Browsable(true), Category("Paproć Barnsley'a")]
        public int Iterations { get; set; }

        [DisplayName("Paleta kolorów")]
        [Browsable(true), Category("Paproć Barnsley'a")]
        [Editor(typeof(GradientTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(GradientTypeConverter))]
        public Gradient Gradient { get; set; }

        public BarnsleyFernParams()
        {
            Size = new Size(1000, 1000);
            Iterations = 50000;
            Gradient = Gradient.Grass;
        }
    }

    public class BarnsleyFern : ParallelTask<BarnsleyFernParams, Bitmap>
    {
        protected override Bitmap RunMpi(BarnsleyFernParams p)
        {
            float[] result = InternalExecuteMPI<float>(
                Resources.Fractals_MPI,
                "fern",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                p.Iterations,
                new Random().Next(),
                new Random().Next());

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }

        protected override Bitmap RunCuda(BarnsleyFernParams p)
        {
            p.BlockSize = new Size(1, 1);
            p.GridSize = new Size(1, 1);

            float[] result = InternalExecuteCuda<float>(
                (p.Architecture == ArchitectureType.x64) ? Resources.FractalsCuda_x64 : Resources.FractalsCuda_x86,
                "fern",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                p.Iterations,
                new Random().Next(),
                new Random().Next());

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }

        protected override Bitmap RunOpencl(BarnsleyFernParams p)
        {
            p.GlobalWorkers = new Size(1, 1);

            float[] result = InternalExecuteOpencl<float>(
                Resources.Fractals,
                "fern",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                p.Iterations,
                new Random().Next(),
                new Random().Next());

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }
    }
}