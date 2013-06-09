using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Parallelity.Windows.Controls
{
    public partial class CapitionPanel : UserControl
    {
        private String _headerText = "Nagłówek";
        private Color _textColor = Color.Black;
        private Font _headerFont = new Font("Segoe UI", 9F);
        private int _headerHeight = 22;
        private int _textIndent = 5;
        private int _borderSize = 1;
        private Color _headerColor1 = SystemColors.ActiveCaption;
        private Color _headerColor2 = SystemColors.InactiveCaption;

        [Browsable(true), Category("CapitionPanel")]
        public string HeaderText
        {
            get { return _headerText; }
            set
            {
                _headerText = value;
                Invalidate();
            }
        }

        [Browsable(true), Category("CapitionPanel")]
        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColor = value;
                Invalidate();
            }
        }

        [Browsable(true), Category("CapitionPanel")]
        public Font HeaderFont
        {
            get { return _headerFont; }
            set
            {
                _headerFont = value;
                Invalidate();
            }
        }

        [Browsable(true), Category("CapitionPanel")]
        public int HeaderHeight
        {
            get { return _headerHeight; }
            set
            {
                _headerHeight = value;
                Invalidate();
            }
        }

        [Browsable(true), Category("CapitionPanel")]
        public int TextIndent
        {
            get { return _textIndent; }
            set
            {
                _textIndent = value;
                Invalidate();
            }
        }

        [Browsable(true), Category("CapitionPanel")]
        public int BorderSize
        {
            get { return _borderSize; }
            set
            {
                _borderSize = value;
                Invalidate();
            }
        }

        [Browsable(true), Category("CapitionPanel")]
        public Color HeaderColor1
        {
            get { return _headerColor1; }
            set
            {
                _headerColor1 = value;
                Invalidate();
            }
        }

        [Browsable(true), Category("CapitionPanel")]
        public Color HeaderColor2
        {
            get { return _headerColor2; }
            set
            {
                _headerColor2 = value;
                Invalidate();
            }
        }

        public CapitionPanel()
        {
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            InitializeComponent();
        }

        public override Rectangle DisplayRectangle
        {
            get
            {
                return new Rectangle(
                    _borderSize + Padding.Left,
                    _borderSize + _headerHeight + Padding.Top,
                    Width - 2 * _borderSize - Padding.Horizontal,
                    Height - 2 * _borderSize - _headerHeight - Padding.Vertical);
            }
        }

        private void CustomPaint(object sender, PaintEventArgs e)
        {
            if (_headerHeight > 1)
            {
                DrawBorder(e.Graphics);
                DrawHeader(e.Graphics);
                DrawText(e.Graphics);
            }
        }

        private void DrawBorder(Graphics graphics)
        {
            using (Pen pen = new Pen(_headerColor2, _borderSize * 2))
            {
                graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void DrawHeader(Graphics graphics)
        {
            Rectangle headerRect = new Rectangle(_borderSize, _borderSize, Width - 2 * _borderSize, _headerHeight);
            using (Brush brush = new LinearGradientBrush(headerRect, _headerColor1, _headerColor2, LinearGradientMode.Vertical))
            {
                graphics.FillRectangle(brush, headerRect);
            }
        }

        private void DrawText(Graphics graphics)
        {
            if (!string.IsNullOrEmpty(_headerText))
            {
                SizeF size = graphics.MeasureString(_headerText, _headerFont);
                using (Brush brush = new SolidBrush(_textColor))
                {
                    graphics.DrawString(_headerText, _headerFont, brush, _textIndent + _borderSize, _borderSize + (_headerHeight - size.Height) / 2);
                }
            }
        }
    }
}