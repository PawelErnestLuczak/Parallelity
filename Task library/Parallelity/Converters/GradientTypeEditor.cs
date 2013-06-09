using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using Parallelity.Drawing;

namespace Parallelity.Converters
{
    public class GradientTypeEditor : UITypeEditor
    {
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override void PaintValue(PaintValueEventArgs pe)
        {
            if (pe.Value == null || !(pe.Value is Gradient))
                return;

            Gradient gradient = (Gradient)pe.Value;

            ColorBlend cb = new ColorBlend();
            cb.Colors = gradient.ColorsModified.ToArray();
            cb.Positions = new float[gradient.ColorsModified.Count];
            for (int i = 0; i <= gradient.ColorsModified.Count - 1; i++)
                cb.Positions[i] = (float)i / (gradient.ColorsModified.Count - 1);

            LinearGradientBrush brush = new LinearGradientBrush(pe.Bounds, Color.Black, Color.Black, 0, false);
            brush.InterpolationColors = cb;

            pe.Graphics.FillRectangle(brush, pe.Bounds);
        }
    }
}
