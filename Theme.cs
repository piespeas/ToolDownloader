namespace AcousticVoid.Downloader;

internal static class Theme
{
    public static readonly Color Ink = Color.FromArgb(10, 10, 10);
    public static readonly Color InkSoft = Color.FromArgb(13, 13, 13);
    public static readonly Color Card = Color.FromArgb(16, 16, 16);
    public static readonly Color Hover = Color.FromArgb(28, 28, 28);
    public static readonly Color Line = Color.FromArgb(36, 36, 36);
    public static readonly Color White = Color.FromArgb(244, 244, 244);
    public static readonly Color Muted = Color.FromArgb(140, 140, 140);
    public static readonly Color Dim = Color.FromArgb(88, 88, 88);
    public static readonly Color Error = Color.FromArgb(228, 82, 76);

    public static readonly Font Ui8 = new("Segoe UI", 8f);
    public static readonly Font Ui8Bold = new("Segoe UI", 8f, FontStyle.Bold);
    public static readonly Font Ui9 = new("Segoe UI", 9f);
    public static readonly Font Ui9Bold = new("Segoe UI", 9f, FontStyle.Bold);
    public static readonly Font Ui10 = new("Segoe UI", 10f);
    public static readonly Font ButtonFont = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font Brand = new("Segoe UI", 12.5f, FontStyle.Bold);
    public static readonly Font Title = new("Segoe UI", 26f, FontStyle.Bold);
    public static readonly Font Mono9 = new("Cascadia Mono", 9f);

    public static Button MakePrimaryButton(string text, int width = 340, int height = 54)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = White,
            ForeColor = Ink,
            Font = ButtonFont,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            TabStop = true,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 226, 226);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(202, 202, 202);
        return button;
    }

    public static Button MakeGhostButton(string text, int width = 118, int height = 34)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = InkSoft,
            ForeColor = White,
            Font = Ui9,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            TabStop = false,
        };
        button.FlatAppearance.BorderColor = Line;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Hover;
        button.FlatAppearance.MouseDownBackColor = Card;
        return button;
    }

    public static Label MakeLabel(string text, Font font, Color color) => new()
    {
        Text = text,
        AutoSize = true,
        Font = font,
        ForeColor = color,
        BackColor = Color.Transparent,
        Margin = new Padding(0),
    };

    public static void DrawTracked(Graphics graphics, string text, Font font, Brush brush,
        float x, float y, float tracking)
    {
        foreach (char character in text)
        {
            string glyph = character.ToString();
            graphics.DrawString(glyph, font, brush, x, y, StringFormat.GenericTypographic);
            x += graphics.MeasureString(glyph, font, PointF.Empty, StringFormat.GenericTypographic).Width + tracking;
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "—";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.0} {units[unit]}";
    }
}
