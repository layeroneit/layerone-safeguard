using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Globalization;
using Windows.Storage.Streams;

namespace LayerOne.Safeguard.Capture;

public static class RobloxCapture
{
    private static ulong _lastHash;
    private static string? _lastText;

    public static async Task<CaptureRead> TryReadAsync()
    {
        if (!NativeWindows.AnyProcess(NativeWindows.RobloxProcessNames))
        {
            _lastHash = 0;
            _lastText = null;
            return CaptureRead.Idle("Roblox is not open.");
        }

        var windows = NativeWindows.FindRoblox();
        var target = windows.FirstOrDefault(w => !NativeWindows.IsMinimized(w.Hwnd));
        if (target is null)
        {
            return CaptureRead.Idle("Roblox is running in the background. Watching resumes when the window is on screen.");
        }

        try
        {
            var item = GraphicsCaptureInterop.CreateItemForWindow(target.Hwnd);
            using var device = Direct3DDeviceFactory.CreateDevice();
            using var bitmap = await CaptureOneBitmapAsync(item, device);
            if (bitmap is null)
            {
                return CaptureRead.Fail("Could not take a picture of the Roblox window.");
            }

            using var bgra = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var hash = HashBitmap(bgra);
            if (hash == _lastHash && _lastText is not null)
            {
                return CaptureRead.Unchanged("Roblox screen has not changed — skipped extra work.", _lastText);
            }

            using var scaled = await ScaleDownAsync(bgra, 960);
            var engine = OcrEngine.TryCreateFromUserProfileLanguages()
                ?? OcrEngine.TryCreateFromLanguage(new Language("en-US"));
            if (engine is null)
            {
                return CaptureRead.Fail("Windows text reading is not installed on this PC.");
            }

            var result = await engine.RecognizeAsync(scaled);
            var text = (result.Text ?? string.Empty).Trim();
            _lastHash = hash;
            _lastText = text;

            if (string.IsNullOrWhiteSpace(text))
            {
                return CaptureRead.Fail("Roblox is open, but no on-screen text was readable. Open chat if you want messages.");
            }

            var summary = LooksLikeHome(text)
                ? "Roblox is on the home screen. Join a game to watch chat."
                : "Reading on-screen text from Roblox.";

            var preview = text.Length > 600 ? text[..600] + "…" : text;
            return CaptureRead.OkText(summary, preview);
        }
        catch (Exception ex)
        {
            return CaptureRead.Fail($"Could not read Roblox: {ex.Message}");
        }
    }

    private static bool LooksLikeHome(string text) =>
        text.Contains("Recommended For You", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Roblox Home", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Charts", StringComparison.OrdinalIgnoreCase);

    private static async Task<SoftwareBitmap?> CaptureOneBitmapAsync(
        GraphicsCaptureItem item,
        IDirect3DDevice device)
    {
        var tcs = new TaskCompletionSource<SoftwareBitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var pool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            device,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            item.Size);

        pool.FrameArrived += (sender, _) =>
        {
            if (tcs.Task.IsCompleted)
            {
                return;
            }

            try
            {
                using var next = sender.TryGetNextFrame();
                if (next is null)
                {
                    return;
                }

                var copy = SoftwareBitmap.CreateCopyFromSurfaceAsync(next.Surface)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                if (!tcs.TrySetResult(copy))
                {
                    copy.Dispose();
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };

        using var session = pool.CreateCaptureSession(item);
        session.IsCursorCaptureEnabled = false;
        session.StartCapture();

        var winner = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(4)));
        if (winner != tcs.Task)
        {
            tcs.TrySetResult(null);
        }

        return await tcs.Task;
    }

    private static async Task<SoftwareBitmap> ScaleDownAsync(SoftwareBitmap source, uint maxWidth)
    {
        if (source.PixelWidth <= maxWidth)
        {
            return SoftwareBitmap.Copy(source);
        }

        var height = (uint)Math.Max(1, source.PixelHeight * (maxWidth / (double)source.PixelWidth));
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetSoftwareBitmap(source);
        encoder.BitmapTransform.ScaledWidth = maxWidth;
        encoder.BitmapTransform.ScaledHeight = height;
        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
        await encoder.FlushAsync();
        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }

    private static ulong HashBitmap(SoftwareBitmap bitmap)
    {
        var length = bitmap.PixelWidth * bitmap.PixelHeight * 4;
        var buffer = new Windows.Storage.Streams.Buffer((uint)length);
        bitmap.CopyToBuffer(buffer);
        var reader = DataReader.FromBuffer(buffer);
        var bytes = new byte[buffer.Length];
        reader.ReadBytes(bytes);

        ulong hash = 14695981039346656037;
        var step = Math.Max(16, bytes.Length / 4096);
        for (var i = 0; i < bytes.Length; i += step)
        {
            hash ^= bytes[i];
            hash *= 1099511628211;
        }

        hash ^= (ulong)bitmap.PixelWidth;
        hash ^= (ulong)bitmap.PixelHeight << 16;
        return hash;
    }
}
