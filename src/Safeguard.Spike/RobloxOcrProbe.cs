using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Globalization;
using WinRT;

namespace LayerOne.Safeguard.Spike;

internal static class RobloxOcrProbe
{
    public static async Task<ProbeResult> RunAsync()
    {
        var windows = ProcessFinder.FindRoblox();
        if (windows.Count == 0)
        {
            return ProbeResult.Skipped("Roblox is not running with a top-level window.");
        }

        var target = windows.FirstOrDefault(w => !NativeWindows.IsMinimized(w.Hwnd)) ?? windows[0];
        var hwnd = target.Hwnd;
        if (hwnd == IntPtr.Zero)
        {
            return ProbeResult.Fail($"Roblox PID {target.ProcessId} has no window handle.");
        }

        if (NativeWindows.IsMinimized(hwnd))
        {
            NativeWindows.TryRestore(hwnd);
            await Task.Delay(600);
        }

        if (NativeWindows.IsMinimized(hwnd))
        {
            return ProbeResult.Fail(
                "Roblox is minimized and could not be restored. Click the Roblox window, then run again.");
        }

        try
        {
            var item = GraphicsCaptureInterop.CreateItemForWindow(hwnd);
            using var device = Direct3DDeviceFactory.CreateDevice();
            using var bitmap = await CaptureOneBitmapAsync(item, device);
            if (bitmap is null)
            {
                return ProbeResult.Fail("Graphics.Capture started, but no frame arrived within 8 seconds.");
            }

            using var bgra = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var engine = OcrEngine.TryCreateFromUserProfileLanguages()
                ?? OcrEngine.TryCreateFromLanguage(new Language("en-US"));
            if (engine is null)
            {
                return ProbeResult.Fail("Windows OCR is not available for the current language pack.");
            }

            var result = await engine.RecognizeAsync(bgra);
            var text = (result.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return ProbeResult.Fail(
                    $"Captured a {item.Size.Width}×{item.Size.Height} frame from Roblox (PID {target.ProcessId}), but OCR returned no text. Open chat and retry.");
            }

            var preview = text.Length > 600 ? text[..600] + "…" : text;
            return ProbeResult.Ok(
                $"OCR read {text.Length} character(s) from Roblox (PID {target.ProcessId}, {item.Size.Width}×{item.Size.Height}).",
                preview);
        }
        catch (Exception ex)
        {
            return ProbeResult.Fail($"Roblox capture/OCR failed: {ex.Message}");
        }
    }

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

        var winner = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(8)));
        if (winner != tcs.Task)
        {
            tcs.TrySetResult(null);
        }

        return await tcs.Task;
    }
}

internal static class Direct3DDeviceFactory
{
    private const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;
    private const int D3D_DRIVER_TYPE_HARDWARE = 1;
    private static Guid DxgiDeviceIid = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");

    [DllImport("d3d11.dll")]
    private static extern int D3D11CreateDevice(
        IntPtr adapter,
        int driverType,
        IntPtr software,
        uint flags,
        IntPtr featureLevels,
        uint featureLevelsCount,
        uint sdkVersion,
        out IntPtr device,
        out int featureLevel,
        out IntPtr context);

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    public static IDirect3DDevice CreateDevice()
    {
        const uint sdkVersion = 7;
        var hr = D3D11CreateDevice(
            IntPtr.Zero,
            D3D_DRIVER_TYPE_HARDWARE,
            IntPtr.Zero,
            D3D11_CREATE_DEVICE_BGRA_SUPPORT,
            IntPtr.Zero,
            0,
            sdkVersion,
            out var d3dDevice,
            out _,
            out var context);

        if (hr < 0 || d3dDevice == IntPtr.Zero)
        {
            throw new InvalidOperationException($"D3D11CreateDevice failed (HRESULT 0x{hr:X8}).");
        }

        try
        {
            hr = Marshal.QueryInterface(d3dDevice, ref DxgiDeviceIid, out var dxgiDevice);
            if (hr < 0 || dxgiDevice == IntPtr.Zero)
            {
                throw new InvalidOperationException($"IDXGIDevice query failed (HRESULT 0x{hr:X8}).");
            }

            try
            {
                hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out var winrtDevice);
                if (hr < 0 || winrtDevice == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"CreateDirect3D11DeviceFromDXGIDevice failed (HRESULT 0x{hr:X8}).");
                }

                try
                {
                    return MarshalInterface<IDirect3DDevice>.FromAbi(winrtDevice);
                }
                finally
                {
                    Marshal.Release(winrtDevice);
                }
            }
            finally
            {
                Marshal.Release(dxgiDevice);
            }
        }
        finally
        {
            Marshal.Release(d3dDevice);
            if (context != IntPtr.Zero)
            {
                Marshal.Release(context);
            }
        }
    }
}
