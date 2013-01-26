using System;
using SharpDX;
using SharpDX.DXGI;
using Windows.UI.Xaml.Controls;

public sealed class DeviceResources : IDisposable {
    public readonly ISwapChainBackgroundPanelNative NativePanel;
    public readonly SwapChain1 SwapChain;

    public DeviceResources(ISwapChainBackgroundPanelNative nativePanel, SwapChain1 swapChain) {
        if (nativePanel == null) throw new ArgumentNullException("nativePanel");
        if (swapChain == null) throw new ArgumentNullException("swapChain");
        this.NativePanel = nativePanel;
        this.SwapChain = swapChain;
    }

    public static DeviceResources From(SwapChainBackgroundPanel panel, DevicesAndContexts devicesAndContexts, DirectXResources dxResources, SwapChainDescription1 desc) {
        if (panel == null) throw new ArgumentNullException("panel");
        if (devicesAndContexts == null) throw new ArgumentNullException("devicesAndContexts");
        if (dxResources == null) throw new ArgumentNullException("dxResources");

        var nativePanel = ComObject.As<ISwapChainBackgroundPanelNative>(panel);

        // create new swap chain for d3d device
        SwapChain1 swapChain;
        using (var dxgiDevice2 = devicesAndContexts.DeviceDirect3D.QueryInterface<Device2>()) {
            dxgiDevice2.MaximumFrameLatency = 1; // queue at most one frame at a time, to reduce latency and power consumption
            using (var dxgiAdapter = dxgiDevice2.Adapter) {
                using (var dxgiFactory2 = dxgiAdapter.GetParent<Factory2>()) {
                    swapChain = dxgiFactory2.CreateSwapChainForComposition(devicesAndContexts.DeviceDirect3D, ref desc, null); // a swap chain for XAML composition
                }
            }
        }
        nativePanel.SwapChain = swapChain; // associate the SwapChainBackgroundPanel with the swap chain

        return new DeviceResources(
            nativePanel,
            swapChain);
    }

    public void Dispose() {
        this.SwapChain.Dispose();
    }
}