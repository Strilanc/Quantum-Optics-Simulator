using System;
using System.Reactive;
using System.Reactive.Subjects;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using ResultCode = SharpDX.DXGI.ResultCode;
using System.Reactive.Linq;

///<summary>Renders to a <see cref="SwapChainBackgroundPanel"/>, for efficient DirectX-XAML interop.</summary>
public sealed class SwapChainBackgroundPanelTarget : IDisposable {
    private readonly Subject<Unit> _initializations = new Subject<Unit>();

    private readonly DirectXResources _directXResources;
    public readonly IObservableLatest<DevicesAndContexts> DevicesAndContexts;
    private readonly IObservableLatest<SizedDeviceResources> _sizedDeviceResources;
    private readonly IObservableLatest<DeviceResources> _deviceResources;

    public IObservableLatest<double> DPI { get { return UIUtil.ObservableDisplayPropertiesLogicalDpi; } }
    public IObservableLatest<Rect> Bounds { get { return UIUtil.ObservableCoreWindowBounds; } }
    private static Tuple<int, int> SwapChainWidthHeight(Rect bounds, double dpi) {
        return Tuple.Create(
            (int)Math.Floor(bounds.Width * dpi / 96),
            (int)Math.Floor(bounds.Height * dpi / 96));
    }

    private readonly Subject<RenderParams> _onRender = new Subject<RenderParams>();
    public IObservable<RenderParams> OnRender { get { return _onRender; } }
    public void RenderAll() {
        _onRender.OnNext(new RenderParams(_sizedDeviceResources.Current, _deviceResources.Current, _directXResources, DevicesAndContexts.Current));
    }

    public SwapChainBackgroundPanelTarget(SwapChainBackgroundPanel panel, DebugLevel debugLevel) {
        this._directXResources = DirectXResources.Create(debugLevel);

        this.DevicesAndContexts = _initializations.Consume(e => global::DevicesAndContexts.CreateFromResources(_directXResources));
        _initializations.OnNext(default(Unit));

        this._deviceResources = DevicesAndContexts.Consume(e => 
            DeviceResources.From(
                panel, 
                e, 
                _directXResources, 
                CreateSwapChainDescription(Tuple.Create(1, 1))));

        // keep d2d DPI synced with display dpi
        DPI.CombineLatest(DevicesAndContexts.SelectDefaultIfNull(e => e.ContextDirect2D), (v, c) => new { v, c })
           .Where(e => e.c != null)
           .Subscribe(e => e.c.DotsPerInch = new DrawingSizeF((float)e.v, (float)e.v));

        this._sizedDeviceResources = 
            this._deviceResources
            .SelectManyUntilNext(e => {
                if (ReferenceEquals(e, null)) return new SizedDeviceResources[1].ToObservable();
                return
                    Bounds.CombineLatest(DPI, (b, d) => new { b, d })
                    .SelectMany(r => new[] {null, r}) // clear out dependents before resizing
                    .SelectDefaultIfNull(r => SizedDeviceResources.From(DevicesAndContexts.Current, e, SwapChainWidthHeight(r.b, r.d), r.d));
            })
            .WithInlineDisposalOnChange()
            .SubscribeObserveLatest();
    }


    /// <summary>
    /// Present the results to the swap chain.
    /// </summary>
    public void Present() {
        try {
            // 1 to block until next VSync, to avoid rendering frames that will never be displayed
            this._deviceResources.Current.SwapChain.Present(1, PresentFlags.None, new PresentParameters());
        } catch (SharpDXException ex) {
            // If the device was removed either by a disconnect or a driver upgrade, we must completely reinitialize the renderer.
            if (ex.ResultCode == ResultCode.DeviceRemoved || ex.ResultCode == ResultCode.DeviceReset) {
                _initializations.OnNext(default(Unit));
                return;
            }
            throw;
        }
    }

    private static SwapChainDescription1 CreateSwapChainDescription(Tuple<int, int> wh) {
        return new SwapChainDescription1 {
            Width = wh.Item1,
            Height = wh.Item2,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
            BufferCount = 2, // two buffers to enable flip effect
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
        };
    }

    public void Dispose() {
        _initializations.OnCompleted(); // disposes device resources and so forth
        _directXResources.Dispose();
    }
}
