using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AcousticVoid.Downloader;

internal static class MarkArt
{
    /// <summary>Magnifier + trend chart, drawn in a 256x256 design box scaled to the target rectangle.</summary>
    public static void Draw(Graphics graphics, Rectangle bounds)
    {
        float size = Math.Min(bounds.Width, bounds.Height);
        if (size < 6f) return;

        var state = graphics.Save();
        graphics.TranslateTransform(bounds.X + (bounds.Width - size) / 2f, bounds.Y + (bounds.Height - size) / 2f);
        float u = size / 256f;

        using var white = new SolidBrush(Theme.White);
        using var handle = new Pen(Theme.White, 32f * u)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        graphics.DrawLine(handle, 112f * u, 142f * u, 28f * u, 226f * u);

        var lens = new RectangleF(86f * u, 22f * u, 144f * u, 144f * u);
        using (var clip = new GraphicsPath())
        {
            clip.AddEllipse(lens);
            var previous = graphics.Clip;
            graphics.SetClip(clip, CombineMode.Intersect);

            graphics.FillRectangle(white, 106f * u, 78f * u, 16f * u, 68f * u);
            graphics.FillRectangle(white, 134f * u, 106f * u, 16f * u, 40f * u);
            graphics.FillRectangle(white, 162f * u, 82f * u, 16f * u, 64f * u);

            using (var trend = new Pen(Theme.White, 9f * u)
                   {
                       LineJoin = LineJoin.Round,
                       StartCap = LineCap.Round,
                       EndCap = LineCap.Round,
                   })
            {
                graphics.DrawLines(trend, new[]
                {
                    new PointF(106f * u, 78f * u),
                    new PointF(128f * u, 108f * u),
                    new PointF(154f * u, 84f * u),
                    new PointF(184f * u, 56f * u),
                });
            }

            foreach (var node in new[]
                     {
                         new PointF(106f * u, 78f * u),
                         new PointF(128f * u, 108f * u),
                         new PointF(154f * u, 84f * u),
                         new PointF(184f * u, 56f * u),
                     })
            {
                graphics.FillEllipse(white, node.X - 9f * u, node.Y - 9f * u, 18f * u, 18f * u);
            }

            graphics.Clip = previous;
        }

        using var ring = new Pen(Theme.White, 18f * u);
        graphics.DrawEllipse(ring, 76f * u, 12f * u, 164f * u, 164f * u);

        graphics.Restore(state);
    }
}

internal sealed class BrandMark : Control
{
    public BrandMark()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(150, 150);
        Margin = new Padding(0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        MarkArt.Draw(e.Graphics, ClientRectangle);
    }
}

internal static class NoiseTexture
{
    private static readonly Lazy<Bitmap> Tile = new(Create);

    public static void Paint(Graphics graphics, Rectangle bounds)
    {
        using var brush = new TextureBrush(Tile.Value);
        graphics.FillRectangle(brush, bounds);
    }

    private static Bitmap Create()
    {
        var bitmap = new Bitmap(180, 180, PixelFormat.Format32bppArgb);
        var random = new Random(0x4D41_5253);
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                int alpha = random.Next(0, 13);
                bitmap.SetPixel(x, y, Color.FromArgb(alpha, 255, 255, 255));
            }
        }
        return bitmap;
    }
}

internal sealed class HeroPanel : Panel
{
    public HeroPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Fill;
        BackColor = Theme.Ink;
        Margin = new Padding(0);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Theme.Ink);
        NoiseTexture.Paint(graphics, ClientRectangle);

        int height = Math.Max(1, Height);
        using var glow = new LinearGradientBrush(
            new Rectangle(0, 0, Math.Max(1, Width), height),
            Color.FromArgb(12, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            LinearGradientMode.Vertical);
        graphics.FillRectangle(glow, 0, 0, Width, height);
    }
}

internal sealed class MonoCheck : Control
{
    private bool _checked;

    public MonoCheck()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.Selectable, true);
        BackColor = Color.Transparent;
        ForeColor = Theme.Muted;
        Font = Theme.Ui9;
        Cursor = Cursors.Hand;
        AutoSize = true;
        TabStop = true;
    }

    public event EventHandler? CheckedChanged;

    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = TextRenderer.MeasureText(Text, Font);
        return new Size(text.Width + 26, Math.Max(18, text.Height + 2));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        const int box = 14;
        int top = (Height - box) / 2;
        var rect = new Rectangle(1, top, box, box);

        using (var border = new Pen(_checked && Enabled ? Theme.White : Theme.Dim))
        {
            graphics.DrawRectangle(border, rect);
        }

        if (_checked)
        {
            using (var fill = new SolidBrush(Enabled ? Theme.White : Theme.Muted))
            {
                graphics.FillRectangle(fill, rect.X + 1, rect.Y + 1, box - 1, box - 1);
            }
            using var tick = new Pen(Theme.Ink, 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            graphics.DrawLines(tick, new[]
            {
                new Point(rect.X + 3, rect.Y + 7),
                new Point(rect.X + 6, rect.Y + 10),
                new Point(rect.X + 11, rect.Y + 4),
            });
        }

        TextRenderer.DrawText(graphics, Text, Font,
            new Rectangle(26, 0, Math.Max(1, Width - 26), Height),
            Enabled ? ForeColor : Theme.Dim,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        if (Focused)
        {
            using var focus = new Pen(Theme.Line) { DashStyle = DashStyle.Dot };
            graphics.DrawRectangle(focus, 0, 0, Width - 1, Height - 1);
        }
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode != Keys.Space) return;
        Checked = !Checked;
        e.Handled = true;
    }
}
