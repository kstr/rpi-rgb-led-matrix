namespace RPiRgbLEDMatrix;

/// <summary>
/// Represents a canvas whose pixels can be manipulated.
/// </summary>
public class RGBLedCanvas
{
    // This is a wrapper for canvas no need to implement IDisposable here 
    // because RGBLedMatrix has ownership and takes care of disposing canvases
    public IntPtr _canvas;

    // this is not called directly by the consumer code,
    // consumer uses factory methods in RGBLedMatrix
    public RGBLedCanvas(IntPtr canvas)
    {
        _canvas = canvas;
        led_canvas_get_size(_canvas, out var width, out var height);
        Width = width;
        Height = height;
    }

    /// <summary>
    /// The width of the canvas in pixels.
    /// </summary>
    public virtual int Width { get; protected set; }

    /// <summary>
    /// The height of the canvas in pixels.
    /// </summary>
    public virtual int Height { get; protected set; }

    /// <summary>
    /// Sets the color of a specific pixel.
    /// </summary>
    /// <param name="x">The X coordinate of the pixel.</param>
    /// <param name="y">The Y coordinate of the pixel.</param>
    /// <param name="color">New pixel color.</param>
    public virtual void SetPixel(int x, int y, Color color) => led_canvas_set_pixel(_canvas, x, y, color.R, color.G, color.B);

    /// <summary>
    /// Copies the colors from the specified buffer to a rectangle on the canvas.
    /// </summary>
    /// <param name="x">The X coordinate of the top-left pixel of the rectangle.</param>
    /// <param name="y">The Y coordinate of the top-left pixel of the rectangle.</param>
    /// <param name="width">Width of the rectangle.</param>
    /// <param name="height">Height of the rectangle.</param>
    /// <param name="colors">Buffer containing the colors to copy.</param>
    public virtual void SetPixels(int x, int y, int width, int height, Span<Color> colors)
    {
        if (colors.Length < width * height)
            throw new ArgumentOutOfRangeException(nameof(colors));

        // Instead of calling P/Invoke directly, call SetPixel for each pixel
        // This allows derived classes to intercept and transform
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                int index = py * width + px;
                SetPixel(x + px, y + py, colors[index]);
            }
        }
    }

    /// <summary>
    /// Sets the color of the entire canvas.
    /// </summary>
    /// <param name="color">New canvas color.</param>
    public virtual void Fill(Color color)
    {
        // Use SetPixel so derived classes can intercept
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                SetPixel(x, y, color);
            }
        }
    }

    /// <summary>
    /// Sets the color of the given section of the canvas.
    /// </summary>
    /// <param name="color">New canvas color.</param>
    public virtual void SubFill(int x, int y, int width, int height, Color color)
    {
        // Use SetPixel so derived classes can intercept
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                SetPixel(px, py, color);
            }
        }
    }

    /// <summary>
    /// Cleans the entire canvas.
    /// </summary>
    public virtual void Clear() => led_canvas_clear(_canvas);

    /// <summary>
    /// Draws a circle of the specified color.
    /// </summary>
    /// <param name="x">The X coordinate of the center.</param>
    /// <param name="y">The Y coordinate of the center.</param>
    /// <param name="radius">The radius of the circle, in pixels.</param>
    /// <param name="color">The color of the circle.</param>
    public virtual void DrawCircle(int x, int y, int radius, Color color)
    {
        // Midpoint circle algorithm - calls SetPixel so derived classes can intercept
        int px = radius;
        int py = 0;
        int err = 0;

        while (px >= py)
        {
            SetPixel(x + px, y + py, color);
            SetPixel(x + py, y + px, color);
            SetPixel(x - py, y + px, color);
            SetPixel(x - px, y + py, color);
            SetPixel(x - px, y - py, color);
            SetPixel(x - py, y - px, color);
            SetPixel(x + py, y - px, color);
            SetPixel(x + px, y - py, color);

            if (err <= 0)
            {
                py += 1;
                err += 2 * py + 1;
            }
            if (err > 0)
            {
                px -= 1;
                err -= 2 * px + 1;
            }
        }
    }

    /// <summary>
    /// Draws a line of the specified color.
    /// </summary>
    /// <param name="x0">The X coordinate of the first point.</param>
    /// <param name="y0">The Y coordinate of the first point.</param>
    /// <param name="x1">The X coordinate of the second point.</param>
    /// <param name="y1">The Y coordinate of the second point.</param>
    /// <param name="color">The color of the line.</param>
    public virtual void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        // Bresenham's line algorithm - calls SetPixel so derived classes can intercept
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// Draws the text with the specified color.
    /// </summary>
    /// <param name="font">Font to draw text with.</param>
    /// <param name="x">The X coordinate of the starting point.</param>
    /// <param name="y">The Y coordinate of the starting point.</param>
    /// <param name="color">The color of the text.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="spacing">Additional spacing between characters.</param>
    /// <param name="vertical">Whether to draw the text vertically.</param>
    /// <returns>How many pixels was advanced on the screen.</returns>
    public virtual int DrawText(RGBLedFont font, int x, int y, Color color, string text, int spacing = 0, bool vertical = false)
    {
        // This now calls DrawTextToCanvas which will use SetPixel
        return font.DrawTextToCanvas(this, x, y, color, text, spacing, vertical);
    }

    public virtual void DrawRect(int x, int y, int width, int height, Color color)
    {
        // Draw top and bottom
        for (int i = x; i < x + width; i++)
        {
            SetPixel(i, y, color);
            SetPixel(i, y + height - 1, color);
        }

        // Draw left and right
        for (int i = y; i < y + height; i++)
        {
            SetPixel(x, i, color);
            SetPixel(x + width - 1, i, color);
        }
    }

    public virtual void DrawImage(int x, int y, int width, int height, Span<Color> pixels)
    {
        if (pixels.Length < width * height)
            throw new ArgumentException("Pixel array too small");

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                SetPixel(x + i, y + j, pixels[j * width + i]);
            }
        }
    }
}
