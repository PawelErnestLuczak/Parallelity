using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using Fractals.Properties;
using Parallelity.Converters;
using Parallelity.Drawing;
using Parallelity.OperatingSystem;

namespace Parallelity.Tasks.Fractals
{
    public class BurningShipParams : ParallelTaskParams
    {
        [DisplayName("x")]
        [Browsable(true), Category("Płonący statek")]
        public float X { get; set; }

        [DisplayName("y")]
        [Browsable(true), Category("Płonący statek")]
        public float Y { get; set; }

        [DisplayName("Powiększenie")]
        [Browsable(true), Category("Płonący statek")]
        public float Scale { get; set; }

        [DisplayName("Rozmiar")]
        [Browsable(true), Category("Płonący statek")]
        public Size Size { get; set; }

        [DisplayName("Iteracje")]
        [Browsable(true), Category("Płonący statek")]
        public int Iterations { get; set; }

        [DisplayName("Próg")]
        [Browsable(true), Category("Płonący statek")]
        public float Threshold { get; set; }

        [DisplayName("Paleta kolorów")]
        [Browsable(true), Category("Płonący statek")]
        [Editor(typeof(GradientTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(GradientTypeConverter))]
        public Gradient Gradient { get; set; }

        public BurningShipParams()
        {
            X = 2125.0f;
            Y = 50.0f;
            Scale = -10.0f;
            Size = new Size(2500, 2500);
            Iterations = 100;
            Threshold = 6.0f;
            Gradient = Gradient.Water;

            Gradient.Inverted = true;
        }
    }

    public class BurningShip : ParallelTask<BurningShipParams, Bitmap>
    {
        protected override Bitmap RunMpi(BurningShipParams p)
        {
            float[] result = InternalExecuteMPI<float>(
                Resources.Fractals_MPI,
                "ship",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                1.0f / p.Scale,
                2.0f * p.X / p.Size.Width,
                2.0f * p.Y / p.Size.Height,
                p.Threshold,
                p.Iterations);

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }

        protected override Bitmap RunCuda(BurningShipParams p)
        {
            float[] result = InternalExecuteCuda<float>(
                (p.Architecture == ArchitectureType.x64) ? Resources.FractalsCuda_x64 : Resources.FractalsCuda_x86,
                "ship",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                1.0f / p.Scale,
                2.0f * p.X / p.Size.Width,
                2.0f * p.Y / p.Size.Height,
                p.Threshold,
                p.Iterations);

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }

        protected override Bitmap RunOpencl(BurningShipParams p)
        {
            float[] result = InternalExecuteOpencl<float>(
                Resources.Fractals,
                "ship",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                1.0f / p.Scale,
                2.0f * p.X / p.Size.Width,
                2.0f * p.Y / p.Size.Height,
                p.Threshold,
                p.Iterations);

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }
    }
}