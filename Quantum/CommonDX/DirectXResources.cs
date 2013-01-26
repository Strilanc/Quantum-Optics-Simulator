using System;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using Factory = SharpDX.DirectWrite.Factory;
using FactoryType = SharpDX.DirectWrite.FactoryType;

public sealed class DirectXResources : IDisposable {
    public readonly Factory1 FactoryDirect2D;
    public readonly Factory FactoryDirectWrite;
    public readonly ImagingFactory2 WICFactory;

    public DirectXResources(Factory1 factoryDirect2D, Factory factoryDirectWrite, ImagingFactory2 wicFactory) {
        if (factoryDirect2D == null) throw new ArgumentNullException("factoryDirect2D");
        if (factoryDirectWrite == null) throw new ArgumentNullException("factoryDirectWrite");
        if (wicFactory == null) throw new ArgumentNullException("wicFactory");
        this.FactoryDirect2D = factoryDirect2D;
        this.FactoryDirectWrite = factoryDirectWrite;
        this.WICFactory = wicFactory;
    }

    public static DirectXResources Create(DebugLevel debugLevel) {
        return new DirectXResources(
            new Factory1(SharpDX.Direct2D1.FactoryType.SingleThreaded, debugLevel),
            new Factory(FactoryType.Shared),
            new ImagingFactory2());
    }


    public void Dispose() {
        this.FactoryDirect2D.Dispose();
        this.FactoryDirectWrite.Dispose();
        this.WICFactory.Dispose();
    }
}