using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace Parallelity.Drawing
{
    public class Gradient
    {
        [DisplayName("Kolory")]
        [Browsable(true)]
        public ObservableCollection<Color> Colors { get; set; }
        
        private bool _Inverted;

        [DisplayName("Odwrotnie")]
        [Browsable(true)]
        public bool Inverted
        {
            get
            {
                return _Inverted;
            }

            set
            {
                _Inverted = value;
                UpdateColorsModified();
            }
        }

        private int _Repeats;

        [DisplayName("Powtórzenia")]
        [Browsable(true)]
        public int Repeats
        {
            get
            {
                return _Repeats;
            }

            set
            {
                _Repeats = value;
                UpdateColorsModified();
            }
        }

        [Browsable(false)]
        public List<Color> ColorsModified { get; private set; }

        private void UpdateColorsModified()
        {
            ColorsModified = new List<Color>();

            for (int i = 0; i < Repeats; i++)
                ColorsModified.AddRange(Colors);

            if (Inverted)
                ColorsModified.Reverse();
        }

        [Browsable(false)]
        public String Name { get; private set; }

        public Gradient(Color[] colors, String name)
        {
            Colors = new ObservableCollection<Color>(colors);
            Inverted = false;
            Repeats = 1;
            Name = name;

            Colors.CollectionChanged += (sender, e) => UpdateColorsModified();
        }

        private static byte ByteInterpolation(byte a, byte b, float factor)
        {
            return (byte)(a * (1 - factor) + b * factor);
        }

        public Color LinearInerpolation(float factor)
        {
            int pos = (int)Math.Max(Math.Ceiling(factor * (ColorsModified.Count - 1)) - 1, 0);

            Color a = ColorsModified[pos];
            Color b = ColorsModified[pos + 1];

            float sub = (factor - (float)pos / (ColorsModified.Count - 1)) * (ColorsModified.Count - 1);

            return Color.FromArgb(
                Gradient.ByteInterpolation(a.A, b.A, sub),
                Gradient.ByteInterpolation(a.R, b.R, sub),
                Gradient.ByteInterpolation(a.G, b.G, sub),
                Gradient.ByteInterpolation(a.B, b.B, sub));
        }

        public static Gradient Flame { get { return _Flame; } }
        private static Gradient _Flame =
            new Gradient(new Color[]
                {
                    Color.Black,
                    Color.DarkRed,
                    Color.Red,
                    Color.DarkOrange,
                    Color.Yellow,
                    Color.LemonChiffon
                }, "Ogień");

        public static Gradient Water { get { return _Water; } }
        private static Gradient _Water =
            new Gradient(new Color[]
                {
                    Color.Black,
                    Color.DarkBlue,
                    Color.DeepSkyBlue,
                    Color.Azure
                }, "Woda");

        public static Gradient Grass { get { return _Grass; } }
        private static Gradient _Grass =
            new Gradient(new Color[]
                {
                    Color.Black,
                    Color.DarkGreen,
                    Color.Chartreuse,
                    Color.MintCream
                }, "Trawa");

        public static Gradient Gray { get { return _Gray; } }
        private static Gradient _Gray =
            new Gradient(new Color[]
                {
                    Color.Black,
                    Color.Gray,
                    Color.DarkGray,
                    Color.LightGray
                }, "Szary");

        public static Gradient Violet { get { return _Violet; } }
        private static Gradient _Violet =
            new Gradient(new Color[]
                {
                    Color.Black,
                    Color.DarkViolet,
                    Color.Violet,
                    Color.PaleVioletRed
                }, "Fioletowy");

        public static Gradient Rainbow { get { return _Rainbow; } }
        private static Gradient _Rainbow =
            new Gradient(new Color[]
                {
                    Color.Blue,
                    Color.Green,
                    Color.Yellow,
                    Color.Red
                }, "Tęczowy");
    }

    public static class GradientExtension
    {
        public unsafe static Bitmap CreateBitmap(this Gradient gradient, int width, int height, float[] raw)
        {
            Bitmap bmp = new Bitmap(width, height);
            int bpp = Bitmap.GetPixelFormatSize(bmp.PixelFormat);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte* ptr = (byte*)bmpData.Scan0.ToPointer();

            //for (int y = 0; y < bmpData.Height; y++)
            Parallel.For(0, bmpData.Height, y =>
            {
                for (int x = 0; x < bmpData.Width; x++)
                {
                    byte* pixel = ptr + y * bmpData.Stride + x * bpp / 8;
                    int i = y * bmpData.Width + x;

                    Color interpolated = gradient.LinearInerpolation(raw[i]);

                    pixel[0] = interpolated.B;
                    pixel[1] = interpolated.G;
                    pixel[2] = interpolated.R;
                    pixel[3] = interpolated.A;
                }
            });

            bmp.UnlockBits(bmpData);

            return bmp;
        }
    }
}
