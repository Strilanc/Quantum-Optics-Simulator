using System;
using System.Collections.Immutable;
using Circuit.Phys;
using Strilanc.Value;

public struct CircuitState {
    public readonly TimeSpan Time;
    public readonly May<Photon> Photon;
    private readonly ImmutableList<RecordedEvent<object>> _absorptions;
    public ImmutableList<RecordedEvent<object>> Absorptions { get { return _absorptions ?? ImmutableList<RecordedEvent<object>>.Empty; } }
    public CircuitState(TimeSpan time, May<Photon> photon, ImmutableList<RecordedEvent<object>> absorptions = null) {
        Photon = photon;
        _absorptions = absorptions;
        Time = time;
    }
    public CircuitState WithDetection(object detection) {
        return new CircuitState(Time, Photon, Absorptions.Add(new RecordedEvent<object>(detection, Time)));
    }
    public CircuitState WithPhoton(May<Photon> photon) {
        var s = this;
        return photon.Select(p => new CircuitState(s.Time, p, s.Absorptions))
            .Else(
                Photon.Select(p => new CircuitState(s.Time, May.NoValue, s.Absorptions).WithDetection(p)).
                Else(this));
    }
    public CircuitState WithTime(TimeSpan time) {
        return new CircuitState(time, Photon, Absorptions);
    }
    public CircuitState WithTick() {
        return new CircuitState(
            Time + TimeSpan.FromMilliseconds(1),
            Photon.Select(e => new Photon(e.Pos + e.Vel, e.Vel, e.Pol)),
            Absorptions);
    }
    public override string ToString() {
        return string.Format(
            "{0}, {1}",
            Photon,
            Absorptions.StringJoin(","));
    }
    public object Identity { get { return new {Photon, Abs = Absorptions}; } }
    public override int GetHashCode() {
        return Identity.GetHashCode();
    }
    public override bool Equals(object obj) {
        if (!(obj is CircuitState)) return false;
        var other = (CircuitState)obj;
        return Equals(Identity, other.Identity);
    }
}