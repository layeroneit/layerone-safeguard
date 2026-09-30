using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WinRT;

namespace LayerOne.Safeguard.Capture;

internal static class GraphicsCaptureInterop
{
    private static readonly Guid GraphicsCaptureItemIid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid InteropIid = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        [PreserveSig]
        int CreateForWindow(IntPtr window, [In] ref Guid iid, out IntPtr result);

        [PreserveSig]
        int CreateForMonitor(IntPtr monitor, [In] ref Guid iid, out IntPtr result);
    }

    public static GraphicsCaptureItem CreateItemForWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("Window handle is zero.", nameof(hwnd));
        }

        var factory = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        var interopIid = InteropIid;
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(factory.ThisPtr, ref interopIid, out var interopPtr));
        try
        {
            var interop = (IGraphicsCaptureItemInterop)Marshal.GetTypedObjectForIUnknown(
                interopPtr,
                typeof(IGraphicsCaptureItemInterop));

            var itemIid = GraphicsCaptureItemIid;
            Marshal.ThrowExceptionForHR(interop.CreateForWindow(hwnd, ref itemIid, out var itemPtr));
            try
            {
                return MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPtr);
            }
            finally
            {
                Marshal.Release(itemPtr);
            }
        }
        finally
        {
            Marshal.Release(interopPtr);
        }
    }
}
