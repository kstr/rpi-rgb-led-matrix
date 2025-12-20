using System.Buffers;
using System.Runtime.InteropServices;

namespace RPiRgbLEDMatrix;

/// <summary>
/// Represents a RGB matrix.
/// </summary>
public class RGBLedMatrix : IDisposable
{
    private IntPtr matrix;
    private bool disposedValue = false;

    /// <summary>
    /// Initializes a new matrix.
    /// </summary>
    /// <param name="rows">Size of a single module. Can be 32, 16 or 8.</param>
    /// <param name="chained">How many modules are connected in a chain.</param>
    /// <param name="parallel">How many modules are connected in a parallel.</param>
    public RGBLedMatrix(int rows, int chained, int parallel)
    {
        matrix = led_matrix_create(rows, chained, parallel);
        if (matrix == (IntPtr)0)
            throw new ArgumentException("Could not initialize a new matrix");
    }

    /// <summary>
    /// Initializes a new matrix.
    /// </summary>
    /// <param name="options">A configuration of a matrix.</param>
    public RGBLedMatrix(RGBLedMatrixOptions options)
    {
        InternalRGBLedMatrixOptions opt = default;
        try
        {
            opt = new(options);
            var args = Environment.GetCommandLineArgs();

            Console.WriteLine("Initializing RGB LED Matrix with the following options:");
            Console.WriteLine($" Rows: {options.Rows}");
            Console.WriteLine($" Columns: {options.Cols}");
            Console.WriteLine($" Chain Length: {options.ChainLength}");
            Console.WriteLine($" Parallel: {options.Parallel}");
            Console.WriteLine($" Brightness: {options.Brightness}");
            Console.WriteLine($" Hardware Mapping: {options.HardwareMapping ?? "default"}");
            Console.WriteLine($" LED RGB Sequence: {options.LedRgbSequence ?? "default"}");
            Console.WriteLine($" Pixel Mapper Config: {options.PixelMapperConfig ?? "default"}");
            Console.WriteLine($" Panel Type: {options.PanelType ?? "default"}");
            Console.WriteLine($" Disable Hardware Pulsing: {options.DisableHardwarePulsing}");
            Console.WriteLine($" Show Refresh Rate: {options.ShowRefreshRate}");
            Console.WriteLine($" Inverse Colors: {options.InverseColors}");
            Console.WriteLine($" Limit Refresh Rate (Hz): {options.LimitRefreshRateHz}");
            Console.WriteLine($" GPIO Slowdown: {options.GpioSlowdown}");


            // Because gpio-slowdown is not provided in the options struct,
            // we manually add it.
            // Let's add it first to the command-line we pass to the
            // matrix constructor, so that it can be overridden with the
            // users' commandline.
            // As always, as the _very_ first, we need to provide the
            // program name argv[0].
            // Count extra flags
            int extraFlags = 1; // for --led-slowdown-gpio
            if (options.DisableHardwarePulsing)
                extraFlags++; // for --led-no-hardware-pulse

            // Allocate argv array
            var argv = new string[args.Length + extraFlags];

            argv[0] = args[0];

            // Add optional flags
            int index = 1;
            argv[index++] = $"--led-slowdown-gpio={options.GpioSlowdown}";
            if (options.DisableHardwarePulsing)
                argv[index++] = "--led-no-hardware-pulse";

            // Copy remaining user args
            Array.Copy(args, 1, argv, index, args.Length - 1);


            matrix = led_matrix_create_from_options_const_argv(ref opt, argv.Length, argv);
            if (matrix == (IntPtr)0)
                throw new ArgumentException("Could not initialize a new matrix");
        }
        finally
        {
            if(options.HardwareMapping is not null) Marshal.FreeHGlobal(opt.hardware_mapping);
            if(options.LedRgbSequence is not null) Marshal.FreeHGlobal(opt.led_rgb_sequence);
            if(options.PixelMapperConfig is not null) Marshal.FreeHGlobal(opt.pixel_mapper_config);
            if(options.PanelType is not null) Marshal.FreeHGlobal(opt.panel_type);
        }
    }

    /// <summary>
    /// Creates a new backbuffer canvas for drawing on.
    /// </summary>
    /// <returns>An instance of <see cref="RGBLedCanvas"/> representing the canvas.</returns>
    public RGBLedCanvas CreateOffscreenCanvas() => new(led_matrix_create_offscreen_canvas(matrix));

    /// <summary>
    /// Returns a canvas representing the current frame buffer.
    /// </summary>
    /// <returns>An instance of <see cref="RGBLedCanvas"/> representing the canvas.</returns>
    /// <remarks>Consider using <see cref="CreateOffscreenCanvas"/> instead.</remarks>
    public RGBLedCanvas GetCanvas() => new(led_matrix_get_canvas(matrix));

    /// <summary>
    /// Swaps this canvas with the currently active canvas. The active canvas
    /// becomes a backbuffer and is mapped to <paramref name="canvas"/> instance.
    /// <br/>
    /// This operation guarantees vertical synchronization.
    /// </summary>
    /// <param name="canvas">Backbuffer canvas to swap.</param>
    public void SwapOnVsync(RGBLedCanvas canvas) =>
        canvas._canvas = led_matrix_swap_on_vsync(matrix, canvas._canvas);

    /// <summary>
    /// The general brightness of the matrix.
    /// </summary>
    public byte Brightness
    {
        get => led_matrix_get_brightness(matrix);
        set => led_matrix_set_brightness(matrix, value);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue) return;

        led_matrix_delete(matrix);
        disposedValue = true;
    }

    ~RGBLedMatrix() => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
