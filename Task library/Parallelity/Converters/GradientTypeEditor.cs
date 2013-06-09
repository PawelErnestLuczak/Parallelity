using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Drawing.Design;
using Parallelity.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;

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
