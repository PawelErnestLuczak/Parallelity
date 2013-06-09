using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Numerics;
using Fractals.Properties;
using Parallelity.Converters;
using Parallelity.Drawing;
using Parallelity.OperatingSystem;

namespace Parallelity.Tasks.Fractals
{
    public class JuliaSetParams : ParallelTaskParams
    {
        [DisplayName("x")]
        [Browsable(true), Category("Zbiór Julii")]
        public float X { get; set; }

        [DisplayName("y")]
        [Browsable(true), Category("Zbiór Julii")]
        public float Y { get; set; }

        [DisplayName("Powiększenie")]
        [Browsable(true), Category("Zbiór Julii")]
        public float Scale { get; set; }

        [DisplayName("Rozmiar")]
        [Browsable(true), Category("Zbiór Julii")]
        public Size Size { get; set; }

        [DisplayName("Stała")]
        [Browsable(true), Category("Zbiór Julii")]
        public Complex Constant { get; set; }

        [DisplayName("Iteracje")]
        [Browsable(true), Category("Zbiór Julii")]
        public int Iterations { get; set; }

        [DisplayName("Próg")]
        [Browsable(true), Category("Zbiór Julii")]
        public float Threshold { get; set; }

        [DisplayName("Paleta kolorów")]
        [Browsable(true), Category("Zbiór Julii")]
        [Editor(typeof(GradientTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(GradientTypeConverter))]
        public Gradient Gradient { get; set; }

        public JuliaSetParams()
        {
            X = 0.0f;
            Y = 0.0f;
            Scale = 1.0f;
            Size = new Size(800, 600);
            Constant = new Complex(-0.8f, 0.156f);
            Iterations = 500;
            Threshold = 25.0f;
            Gradient = Gradient.Grass;
        }
    }

    public class JuliaSet : ParallelTask<JuliaSetParams, Bitmap>
    {
        protected override Bitmap RunMpi(JuliaSetParams p)
        {
            float[] result = InternalExecuteMPI<float>(
                Resources.Fractals_MPI,
                "julia",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                1.0f / p.Scale,
                2.0f * p.X / p.Size.Width,
                2.0f * p.Y / p.Size.Height,
                (float)p.Constant.Real,
                (float)p.Constant.Imaginary,
                p.Threshold,
                p.Iterations);

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }

        protected override Bitmap RunCuda(JuliaSetParams p)
        {
            float[] result = InternalExecuteCuda<float>(
                (p.Architecture == ArchitectureType.x64) ? Resources.FractalsCuda_x64 : Resources.FractalsCuda_x86,
                "julia",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                1.0f / p.Scale,
                2.0f * p.X / p.Size.Width,
                2.0f * p.Y / p.Size.Height,
                (float)p.Constant.Real,
                (float)p.Constant.Imaginary,
                p.Threshold,
                p.Iterations);

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }

        protected override Bitmap RunOpencl(JuliaSetParams p)
        {
            float[] result = InternalExecuteOpencl<float>(
                Resources.Fractals,
                "julia",
                p.Size.Width * p.Size.Height,
                p,
                p.Size.Width,
                p.Size.Height,
                1.0f / p.Scale,
                2.0f * p.X / p.Size.Width,
                2.0f * p.Y / p.Size.Height,
                (float)p.Constant.Real,
                (float)p.Constant.Imaginary,
                p.Threshold,
                p.Iterations);

            Bitmap bmp = p.Gradient.CreateBitmap(p.Size.Width, p.Size.Height, result);
            TriggerCheckpoint(ParallelExecutionCheckpointType.CheckpointResultPostProcess);

            return bmp;
        }
    }
}
