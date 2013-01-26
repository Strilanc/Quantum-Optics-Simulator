using System.Collections.Immutable;
using System.Linq;
using Strilanc.Value;

public struct CircuitState {
    public readonly May<Photon> Photon;
    private readonly ImmutableDictionary<object, ImmutableList<object>> _detections;
    public ImmutableDictionary<object, ImmutableList<object>> Detections { get { return _detections ?? ImmutableDictionary<object, ImmutableList<object>>.Empty; } }
    public CircuitState(May<Photon> photon, ImmutableDictionary<object, ImmutableList<object>> detections = null) : this() {
        Photon = photon;
        _detections = detections;
    }
    public CircuitState WithDetection(object key, int time, bool destroy) {
        var cur = Detections.ContainsKey(key) ? Detections[key] : ImmutableList<object>.Empty;
        return new CircuitState(Photon.Where(_ => !destroy), Detections.SetItem(key, cur.Add(new { Photon, time })));
    }
    public CircuitState WithPhoton(May<Photon> photon) {
        return new CircuitState(photon, Detections);
    }
    public override string ToString() {
        return string.Format(
            "{0}, {1}",
            Photon,
            Detections.AsEnumerable().Select(e => 
                string.Format(
                    "{0}: {1}", 
                    e.Key, 
                    e.Value.StringJoin(","))).StringJoin("; "));
    }
    public object Identity { get { return new {Photon, Det = Detections.Select(e => new { e.Key, Val = e.Value.ToEquatable()}).ToEquatable()}; } }
    public override int GetHashCode() {
        return Identity.GetHashCode();
    }
    public override bool Equals(object obj) {
        if (!(obj is CircuitState)) return false;
        var other = (CircuitState)obj;
        return Equals(Identity, other.Identity);
    }
}