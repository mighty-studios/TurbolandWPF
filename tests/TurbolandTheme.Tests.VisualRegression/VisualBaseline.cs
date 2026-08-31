using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Renders a control tree off-screen and compares it, pixel for pixel, against a
/// committed baseline PNG.
///
/// <para>
/// Everything else in this project asserts on the <i>logical</i> tree: a brush
/// resolves to the right color, a template binds the right property, a metric
/// scales. That catches a great deal, but it cannot catch a control that computes
/// every value correctly and still draws wrong - a border painted under its own
/// content, a scroll bar overlapping the item it belongs to, a one-pixel seam where
/// two tiled surfaces meet. Only rendering a bitmap and comparing it pixel for pixel
/// catches those.
/// </para>
///
/// <para>
/// <b>No ImageSharp.</b> WPF already ships a PNG decoder and this harness renders with
/// WPF anyway. Using one codec for both ends removes a dependency and, more usefully,
/// removes a class of false positive: see <see cref="Compare"/> for why both sides are
/// compared <i>after</i> a PNG round-trip rather than against the raw render buffer.
/// </para>
///
/// <para>
/// <b>No window.</b> Implicit styles come from <c>Application.Resources</c> and reach
/// any <c>FrameworkElement</c>, connected or not, so a detached tree is styled exactly
/// as a real one. That keeps the harness headless and fast. The cost is that states
/// driven by read-only properties the input system owns - <c>IsMouseOver</c>,
/// <c>IsFocused</c>, <c>IsPressed</c> - cannot be forced here; those remain the job of
/// the trigger-level tests. States that are ordinary settable properties
/// (<c>IsEnabled</c>, <c>IsChecked</c>, <c>IsSelected</c>, <c>IsDefault</c>) are covered.
/// </para>
/// </summary>
internal static class VisualBaseline
{
    /// <summary>
    /// Set to 1 to overwrite baselines with the current render. The run still fails
    /// afterwards: accepting a new baseline is a decision to be looked at and
    /// committed deliberately, never something a green test run can do quietly.
    /// </summary>
    private const string UpdateVariable = "TURBOLAND_BASELINE_UPDATE";

    /// <summary>
    /// The baselines live beside this source file, not beside the compiled test
    /// assembly, so a newly written one lands in the source tree ready to commit.
    /// Resolved from <see cref="CallerFilePathAttribute"/> rather than by walking up
    /// from the output directory, which breaks the moment the build layout changes.
    /// </summary>
    public static string BaselineDirectory { get; } = Locate();

    private static string Locate([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "baselines");

    private static string FailureDirectory => Path.Combine(BaselineDirectory, "_failed");

    /// <summary>
    /// Renders <paramref name="root"/> and asserts it matches <paramref name="name"/>.png.
    /// </summary>
    public static void Verify(string name, FrameworkElement root)
    {
        byte[] actual = Encode(Render(root));
        string baselinePath = Path.Combine(BaselineDirectory, name + ".png");

        Directory.CreateDirectory(BaselineDirectory);

        if (!File.Exists(baselinePath))
        {
            File.WriteAllBytes(baselinePath, actual);
            throw new VisualBaselineException(
                $"No baseline existed for '{name}', so one was written from this render:{Environment.NewLine}" +
                $"  {baselinePath}{Environment.NewLine}" +
                "Look at it. If it is what the control should look like, re-run and it will pass. " +
                "A baseline nobody has looked at is worse than no baseline, because it makes every " +
                "later run agree with a mistake.");
        }

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllBytes(baselinePath, actual);
            throw new VisualBaselineException(
                $"{UpdateVariable}=1, so the baseline for '{name}' was overwritten:{Environment.NewLine}" +
                $"  {baselinePath}{Environment.NewLine}" +
                "Review the change before committing it, then clear the variable.");
        }

        Difference difference = Compare(File.ReadAllBytes(baselinePath), actual);
        if (difference.IsMatch)
            return;

        Directory.CreateDirectory(FailureDirectory);
        string actualPath = Path.Combine(FailureDirectory, name + ".actual.png");
        File.WriteAllBytes(actualPath, actual);

        StringBuilder message = new();
        message.AppendLine($"Render does not match the baseline for '{name}'.");
        message.AppendLine(difference.Summary);
        message.AppendLine($"  baseline: {baselinePath}");
        message.AppendLine($"  actual:   {actualPath}");

        if (difference.DiffImage is not null)
        {
            string diffPath = Path.Combine(FailureDirectory, name + ".diff.png");
            File.WriteAllBytes(diffPath, difference.DiffImage);
            message.AppendLine($"  diff:     {diffPath}  (magenta marks every changed pixel)");
        }

        message.Append(
            $"If the change is intended, re-run with {UpdateVariable}=1 to rewrite the baseline.");

        throw new VisualBaselineException(message.ToString());
    }

    /// <summary>
    /// Measures, arranges and rasterises a detached element at 96 DPI, so one device
    /// independent pixel is one bitmap pixel and the theme's integer cell grid lands
    /// exactly on pixel boundaries.
    /// </summary>
    private static BitmapSource Render(FrameworkElement root)
    {
        Layout(root);

        // Containers of an ItemsControl, and anything else deferred to the dispatcher
        // during the first pass, only exist after the queue drains. Without this an
        // empty ListBox renders and the baseline records the emptiness.
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
        Size size = Layout(root);

        RenderTargetBitmap bitmap = new(
            (int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        return bitmap;
    }

    private static Size Layout(FrameworkElement root)
    {
        root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        // Ceiling, not round: a half-pixel of content still has to be drawn somewhere,
        // and a bitmap one pixel short would clip it and call that the baseline.
        Size size = new(
            Math.Max(1, Math.Ceiling(root.DesiredSize.Width)),
            Math.Max(1, Math.Ceiling(root.DesiredSize.Height)));

        root.Arrange(new Rect(size));
        root.UpdateLayout();
        return size;
    }

    private static byte[] Encode(BitmapSource bitmap)
    {
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using MemoryStream stream = new();
        encoder.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Compares two PNGs by decoding <b>both</b> of them.
    /// </summary>
    /// <remarks>
    /// The render is produced in Pbgra32 (premultiplied) and PNG stores straight
    /// alpha, so encoding is lossy in the low bits of any partly transparent pixel.
    /// Comparing the live render buffer against a decoded baseline would therefore
    /// report differences that no file actually contains. Decoding both sides asks
    /// the only question worth asking: would the file written now be the file
    /// already committed?
    /// </remarks>
    private static Difference Compare(byte[] baselinePng, byte[] actualPng)
    {
        Image baseline = Decode(baselinePng);
        Image actual = Decode(actualPng);

        if (baseline.Width != actual.Width || baseline.Height != actual.Height)
        {
            return new Difference(
                $"  size changed: {baseline.Width}x{baseline.Height} -> {actual.Width}x{actual.Height}. " +
                "A size change is a layout change, so no pixel comparison is meaningful.",
                null);
        }

        byte[] diff = new byte[baseline.Pixels.Length];
        int changed = 0;
        int firstX = -1, firstY = -1;
        long worst = 0;

        for (int y = 0; y < baseline.Height; y++)
        {
            for (int x = 0; x < baseline.Width; x++)
            {
                int i = (y * baseline.Width + x) * 4;
                int delta =
                    Math.Abs(baseline.Pixels[i] - actual.Pixels[i]) +
                    Math.Abs(baseline.Pixels[i + 1] - actual.Pixels[i + 1]) +
                    Math.Abs(baseline.Pixels[i + 2] - actual.Pixels[i + 2]) +
                    Math.Abs(baseline.Pixels[i + 3] - actual.Pixels[i + 3]);

                if (delta == 0)
                {
                    // Keep the unchanged image faintly visible so the marks can be
                    // located, without competing with them for attention.
                    byte grey = (byte)(190 + baseline.Pixels[i + 1] / 8);
                    diff[i] = diff[i + 1] = diff[i + 2] = grey;
                    diff[i + 3] = 255;
                    continue;
                }

                changed++;
                worst = Math.Max(worst, delta);
                if (firstX < 0) { firstX = x; firstY = y; }

                diff[i] = 255; diff[i + 1] = 0; diff[i + 2] = 255; diff[i + 3] = 255;
            }
        }

        if (changed == 0)
            return Difference.Match;

        double percent = 100.0 * changed / (baseline.Width * baseline.Height);
        string summary =
            $"  {changed:N0} of {baseline.Width * baseline.Height:N0} pixels differ ({percent:F3}%), " +
            $"largest channel-sum delta {worst}, first at ({firstX}, {firstY}).";

        return new Difference(summary, EncodeBgra(diff, baseline.Width, baseline.Height));
    }

    private static Image Decode(byte[] png)
    {
        using MemoryStream stream = new(png);
        BitmapFrame frame = BitmapFrame.Create(
            stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

        // Normalise: a PNG may legitimately be saved as 24-bit, indexed or 64-bit and
        // still be the same picture. Convert both sides to one format so the
        // comparison is about color rather than about file encoding.
        FormatConvertedBitmap converted = new(frame, PixelFormats.Bgra32, null, 0);

        int stride = converted.PixelWidth * 4;
        byte[] pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);
        return new Image(converted.PixelWidth, converted.PixelHeight, pixels);
    }

    private static byte[] EncodeBgra(byte[] pixels, int width, int height) =>
        Encode(BitmapSource.Create(
            width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4));

    private readonly record struct Image(int Width, int Height, byte[] Pixels);

    private readonly record struct Difference(string Summary, byte[]? DiffImage)
    {
        public static Difference Match => new(string.Empty, null);

        public bool IsMatch => Summary.Length == 0;
    }
}

/// <summary>
/// Distinguishes a genuine pixel difference from an ordinary assertion failure, so a
/// mismatch reads as "the picture changed" rather than "Assert.Equal failed".
/// </summary>
internal sealed class VisualBaselineException(string message) : Exception(message);
