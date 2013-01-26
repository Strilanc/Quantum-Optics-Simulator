using System;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using Windows.Foundation;

public sealed class SizedDeviceResources : IDisposable {
    public readonly RenderTargetView RenderTargetView;
    public readonly Rect RenderTargetBounds;
    public readonly DepthStencilView DepthStencilView;
    public readonly Texture2D BackBuffer;
    public readonly Bitmap1 BitmapTarget2D;

    public SizedDeviceResources(RenderTargetView renderTargetView, Rect renderTargetBounds, DepthStencilView depthStencilView, Texture2D backBuffer, Bitmap1 bitmapTarget2D) {
        if (renderTargetView == null) throw new ArgumentNullException("renderTargetView");
        if (depthStencilView == null) throw new ArgumentNullException("depthStencilView");
        if (backBuffer == null) throw new ArgumentNullException("backBuffer");
        if (bitmapTarget2D == null) throw new ArgumentNullException("bitmapTarget2D");
        this.RenderTargetView = renderTargetView;
        this.RenderTargetBounds = renderTargetBounds;
        this.DepthStencilView = depthStencilView;
        this.BackBuffer = backBuffer;
        this.BitmapTarget2D = bitmapTarget2D;
    }

    public static SizedDeviceResources From(DevicesAndContexts devicesAndContexts, DeviceResources deviceResources, Tuple<int, int> swapChainWidthHeight, double dpi) {
        if (devicesAndContexts == null) throw new ArgumentNullException("devicesAndContexts");
        if (deviceResources == null) throw new ArgumentNullException("deviceResources");
        if (swapChainWidthHeight == null) throw new ArgumentNullException("swapChainWidthHeight");

        devicesAndContexts.ContextDirect2D.Target = null;
        deviceResources.SwapChain.ResizeBuffers(2, swapChainWidthHeight.Item1, swapChainWidthHeight.Item2, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

        // Obtain the backbuffer for this window which will be the final 3D rendertarget.
        var backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(deviceResources.SwapChain, 0);

        // Create a view interface on the rendertarget to use on bind.
        var renderTargetView = new RenderTargetView(devicesAndContexts.DeviceDirect3D, backBuffer);

        // Cache the rendertarget dimensions in our helper class for convenient use.
        var renderTargetBounds = new Rect(0, 0, backBuffer.Description.Width, backBuffer.Description.Height);

        // Set current viewport to a descriptor of the full window size
        devicesAndContexts.ContextDirect3D.Rasterizer.SetViewports(
            new ViewportF((float)renderTargetBounds.X,
                          (float)renderTargetBounds.Y,
                          (float)renderTargetBounds.Width,
                          (float)renderTargetBounds.Height,
                          0,
                          1));

        // Create a descriptor for the depth/stencil buffer.
        // Allocate a 2-D surface as the depth/stencil buffer.
        // Create a DepthStencil view on this surface to use on bind.
        DepthStencilView depthStencilView;
        using (var depthBuffer = new Texture2D(devicesAndContexts.DeviceDirect3D,
                                               new Texture2DDescription {
                                                   Format = Format.D24_UNorm_S8_UInt,
                                                   ArraySize = 1,
                                                   MipLevels = 1,
                                                   Width = (int)renderTargetBounds.Width,
                                                   Height = (int)renderTargetBounds.Height,
                                                   SampleDescription = new SampleDescription(1, 0),
                                                   BindFlags = BindFlags.DepthStencil,
                                               }))
            depthStencilView = new DepthStencilView(
                devicesAndContexts.DeviceDirect3D,
                depthBuffer,
                new DepthStencilViewDescription {
                    Dimension = DepthStencilViewDimension.Texture2D
                });

        // Now we set up the Direct2D render target bitmap linked to the swapchain. 
        // Whenever we render to this bitmap, it will be directly rendered to the 
        // swapchain associated with the window.
        var bitmapProperties = new BitmapProperties1(
            new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
            (float)dpi,
            (float)dpi,
            BitmapOptions.Target | BitmapOptions.CannotDraw);

        // Direct2D needs the dxgi version of the backbuffer surface pointer.
        // Get a D2D surface from the DXGI back buffer to use as the D2D render target.
        Bitmap1 bitmapTarget2D;
        using (var dxgiBackBuffer = deviceResources.SwapChain.GetBackBuffer<Surface>(0))
            bitmapTarget2D = new Bitmap1(devicesAndContexts.ContextDirect2D, dxgiBackBuffer, bitmapProperties);
        devicesAndContexts.ContextDirect2D.Target = bitmapTarget2D;
        // Set D2D text anti-alias mode to Grayscale to ensure proper rendering of text on intermediate surfaces.
        devicesAndContexts.ContextDirect2D.TextAntialiasMode = TextAntialiasMode.Grayscale;

        return new SizedDeviceResources(
            renderTargetView,
            renderTargetBounds,
            depthStencilView,
            backBuffer,
            bitmapTarget2D);
    }
    public void Dispose() {
        this.RenderTargetView.Dispose();
        this.DepthStencilView.Dispose();
        this.BitmapTarget2D.Dispose();
        this.BackBuffer.Dispose();
    }
}