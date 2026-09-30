using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace LayerOne.Safeguard.Capture;

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
