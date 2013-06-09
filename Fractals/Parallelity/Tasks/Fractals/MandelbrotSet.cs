using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using Fractals.Properties;
using Parallelity.Converters;
using Parallelity.Drawing;
using Parallelity.OperatingSystem;

namespace Parallelity.Tasks.Fractals
{
    public class MandelbrotSetParams : ParallelTaskParams
    {
        [DisplayName("x")]
        [Browsable(true), Category("Zbiór Mendelbrota")]
        public float X { get; set; }

        [DisplayName("y")]
        [Browsable(true), Category("Zbiór Mendelbrota")]
        public float Y { get; set; }

        [DisplayName("Powiększenie")]
        [Browsable(true), Category("Zbiór Mendelbrota")]
        public float Scale { get; set; }

        [DisplayName("Rozmiar")]
        [Browsable(true), Category("Zbiór Mendelbrota")]
        public Size Size { get; set; }

        [DisplayName("Iteracje")]
        [Browsable(true), Category("Zbiór Mendelbrota")]
        public int Iterations { get; set; }

        [DisplayName("Próg")]
        [Browsable(true), Category("Zbiór Mendelbrota")]
        public float Threshold { get; set; }

        [DisplayName("Paleta kolorów")]
        [Browsable(true), Category("Zbiór Mendelbrota")]
        [Editor(typeof(GradientTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(GradientTypeConverter))]
        public Gradient Gradient { get; set; }

        public MandelbrotSetParams()
        {
            X = 500.0f;
            Y = 0.0f;
            Scale = -0.6f;
            Size = new Size(1600, 1200);
            Iterations = 500;
            Threshold = 25.0f;
            Gradient = Gradient.Flame;
        }
    }

    public class MandelbrotSet : ParallelTask<MandelbrotSetParams, Bitmap>
    {
        protected override Bitmap RunMpi(MandelbrotSetParams p)
        {
            float[] result = InternalExecuteMPI<float>(
                Resources.Fractals_MPI,
                "mandelbrot",
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

        protected override Bitmap RunCuda(MandelbrotSetParams p)
        {
            float[] result = InternalExecuteCuda<float>(
                (p.Architecture == ArchitectureType.x64) ? Resources.FractalsCuda_x64 : Resources.FractalsCuda_x86,
                "mandelbrot",
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

        protected override Bitmap RunOpencl(MandelbrotSetParams p)
        {
            float[] result = InternalExecuteOpencl<float>(
                Resources.Fractals,
                "mandelbrot",
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