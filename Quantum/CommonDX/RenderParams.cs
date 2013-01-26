using System;

public struct RenderParams {
    public readonly SizedDeviceResources SizedDeviceResources;
    public readonly DeviceResources DeviceResources;
    public readonly DirectXResources DirectXResources;
    public readonly DevicesAndContexts DevicesAndContexts;
    public RenderParams(SizedDeviceResources sizedDeviceResources, DeviceResources deviceResources, DirectXResources directXResources, DevicesAndContexts devicesAndContexts) {
        if (sizedDeviceResources == null) throw new ArgumentNullException("sizedDeviceResources");
        if (deviceResources == null) throw new ArgumentNullException("deviceResources");
        if (directXResources == null) throw new ArgumentNullException("directXResources");
        if (devicesAndContexts == null) throw new ArgumentNullException("devicesAndContexts");
        this.SizedDeviceResources = sizedDeviceResources;
        this.DeviceResources = deviceResources;
        this.DirectXResources = directXResources;
        this.DevicesAndContexts = devicesAndContexts;
    }
}