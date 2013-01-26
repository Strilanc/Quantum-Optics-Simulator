using System;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using DeviceContext = SharpDX.Direct2D1.DeviceContext;

public sealed class DevicesAndContexts : IDisposable {
    public readonly Device1 DeviceDirect3D;
    public readonly DeviceContext1 ContextDirect3D;
    public readonly SharpDX.Direct2D1.Device DeviceDirect2D;
    public readonly DeviceContext ContextDirect2D;

    public DevicesAndContexts(Device1 deviceDirect3D, DeviceContext1 contextDirect3D, SharpDX.Direct2D1.Device deviceDirect2D, DeviceContext contextDirect2D) {
        if (deviceDirect3D == null) throw new ArgumentNullException("deviceDirect3D");
        if (contextDirect3D == null) throw new ArgumentNullException("contextDirect3D");
        if (deviceDirect2D == null) throw new ArgumentNullException("deviceDirect2D");
        if (contextDirect2D == null) throw new ArgumentNullException("contextDirect2D");
        this.DeviceDirect3D = deviceDirect3D;
        this.ContextDirect3D = contextDirect3D;
        this.DeviceDirect2D = deviceDirect2D;
        this.ContextDirect2D = contextDirect2D;
    }

    public static DevicesAndContexts CreateFromResources(DirectXResources dxResources) {
        if (dxResources == null) throw new ArgumentNullException("dxResources");
        
        // require BgraSupport, attempt VideoSupport
        Device1 deviceDirect3D;
        try {
            using (var defaultDevice = new Device(DriverType.Hardware, DeviceCreationFlags.VideoSupport | DeviceCreationFlags.BgraSupport))
                deviceDirect3D = defaultDevice.QueryInterface<Device1>();
        } catch {
            using (var defaultDevice = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport))
                deviceDirect3D = defaultDevice.QueryInterface<Device1>();
        }

        var contextDirect3D = deviceDirect3D.ImmediateContext.QueryInterface<DeviceContext1>();

        SharpDX.Direct2D1.Device deviceDirect2D;
        using (var dxgiDevice = deviceDirect3D.QueryInterface<SharpDX.DXGI.Device>())
            deviceDirect2D = new SharpDX.Direct2D1.Device(dxResources.FactoryDirect2D, dxgiDevice);

        var contextDirect2D = new DeviceContext(deviceDirect2D, SharpDX.Direct2D1.DeviceContextOptions.None);

        return new DevicesAndContexts(deviceDirect3D, contextDirect3D, deviceDirect2D, contextDirect2D);
    }

    public void Dispose() {
        this.ContextDirect3D.Dispose();
        this.DeviceDirect2D.Dispose();
        this.ContextDirect2D.Dispose();
    }
}