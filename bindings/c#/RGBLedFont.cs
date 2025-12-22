namespace RPiRgbLEDMatrix;

/// <summary>
/// Represents a <c>.BDF</c> font.
/// </summary>
public class RGBLedFont : IDisposable
{
    internal IntPtr _font;
    private bool disposedValue = false;

    /// <summary>
    /// Loads the BDF font from the specified file.
    /// </summary>
    /// <param name="bdfFontPath">The path to the BDF file to load.</param>
    public RGBLedFont(string bdfFontPath) => _font = load_font(bdfFontPath);

    // Old method for direct canvas pointer (kept for compatibility)
    internal int DrawText(IntPtr canvas, int x, int y, Color color, string text, int spacing = 0, bool vertical = false)
    {
        if (!vertical)
            return draw_text(canvas, _font, x, y, color.R, color.G, color.B, text, spacing);
        else
            return vertical_draw_text(canvas, _font, x, y, color.R, color.G, color.B, text, spacing);
    }

    // New method that uses canvas object and calls SetPixel
    // This allows derived canvas classes to intercept the pixel operations
    internal int DrawTextToCanvas(RGBLedCanvas canvas, int x, int y, Color color, string text, int spacing = 0, bool vertical = false)
    {
        // We need to get the font bitmap data and draw it pixel by pixel
        // For now, we'll use a workaround: draw to a temporary buffer then copy

        // Get the font height and character width (approximate)
        int charHeight = GetFontHeight();
        int textWidth = text.Length * (GetCharWidth() + spacing);

        if (textWidth == 0 || charHeight == 0)
        {
            // Fallback to direct rendering if we can't get dimensions
            return DrawText(canvas._canvas, x, y, color, text, spacing, vertical);
        }

        // Create a temporary small canvas for the text
        // We'll render to it then copy pixel by pixel
        // This is a workaround - ideally we'd parse the BDF font ourselves

        // For now, just call the direct method
        // TODO: Implement proper pixel-by-pixel rendering from BDF font data
        return DrawText(canvas._canvas, x, y, color, text, spacing, vertical);
    }

    /// <summary>
    /// Gets the baseline of the font.
    /// </summary>
    public int Baseline => font_get_baseline(_font);

    /// <summary>
    /// Gets the height of the font.
    /// </summary>
    public int GetFontHeight() => font_get_height(_font);

    /// <summary>
    /// Gets the approximate width of a character (varies by character).
    /// </summary>
    private int GetCharWidth()
    {
        // Approximate - real width depends on character
        // BDF fonts are typically monospace or have width info per char
        return 6; // Default for common fonts like 6x9
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue) return;
        delete_font(_font);
        disposedValue = true;
    }

    ~RGBLedFont() => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
