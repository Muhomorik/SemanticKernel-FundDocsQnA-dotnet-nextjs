using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ImageMagick;
using Microsoft.Web.WebView2.Core;
using NLog;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Reusable service for capturing WebView2 screenshots and applying privacy filters.
/// </summary>
/// <remarks>
/// <para>
/// WebView2 is an HWND control with airspace issues — WPF overlays cannot render on top of it.
/// The privacy workflow is: capture screenshot → apply distortion filter → hide WebView2 → show filtered image.
/// </para>
/// <para>
/// Uses ImageMagick's Spread + Blur + OilPaint pipeline to create a painted, smeared
/// appearance that displaces pixels and removes detail, making text unreadable.
/// </para>
/// </remarks>
public static class PrivacyFilterService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Captures a WebView2 screenshot and applies the oil-paint privacy filter.
    /// Returns a frozen <see cref="BitmapImage"/> ready for UI binding.
    /// </summary>
    /// <param name="coreWebView2">The CoreWebView2 instance to capture.</param>
    /// <param name="dispatcher">The UI dispatcher for creating the BitmapImage.</param>
    /// <returns>A frozen BitmapImage with the privacy filter applied, or <see langword="null"/> on failure.</returns>
    public static async Task<BitmapImage?> CaptureAndFilterAsync(CoreWebView2 coreWebView2, Dispatcher dispatcher)
    {
        try
        {
            Logger.Debug("Capturing WebView2 screenshot for privacy filter");

            using var stream = new MemoryStream();
            await coreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            stream.Position = 0;

            var processedBytes = await Task.Run(() => ApplyPrivacyEffect(stream));

            BitmapImage? result = null;
            await dispatcher.InvokeAsync(() =>
            {
                using var outputStream = new MemoryStream(processedBytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = outputStream;
                image.EndInit();
                image.Freeze();
                result = image;
            });

            Logger.Debug("Privacy filter screenshot captured successfully");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to capture privacy filter screenshot");
            return null;
        }
    }

    /// <summary>
    /// Applies Spread + Blur + OilPaint effect using ImageMagick for privacy distortion.
    /// </summary>
    /// <param name="inputStream">The input image stream (PNG).</param>
    /// <returns>Byte array of the processed PNG image.</returns>
    /// <remarks>
    /// Equivalent to CLI: <c>magick input.png -spread 12 -blur 0x3 -paint 6 output.png</c>
    /// <para><b>Spread 12</b> — randomly displaces pixels within a 12px radius, breaking text structure.</para>
    /// <para><b>Blur 0x3</b> — Gaussian blur with sigma 3, smooths the spread noise.</para>
    /// <para><b>OilPaint 6</b> — oil-painting effect with radius 6, smears remaining detail.</para>
    /// </remarks>
    public static byte[] ApplyPrivacyEffect(Stream inputStream)
    {
        using var image = new MagickImage(inputStream);

        image.Spread(12);
        image.GaussianBlur(0, 3);
        image.OilPaint(6, 1);

        using var outputStream = new MemoryStream();
        image.Write(outputStream, MagickFormat.Png);
        return outputStream.ToArray();
    }
}
