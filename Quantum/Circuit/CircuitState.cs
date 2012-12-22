using System;
using System.Collections.Generic;
using System.Linq;
using Strilanc.LinqToCollections;

public struct CircuitState {
    public static MockProperty<CircuitState, Wire> WireProp {
        get {
            return new MockProperty<CircuitState, Wire>(
                e => e.Wire,
                (e, v) => new CircuitState {
                    Wire = v,
                    Detections = e.Detections
                });
        }
    }
    public static MockProperty<CircuitState, bool> DetectorProp(int i) {
        return new MockProperty<CircuitState, bool>(
            e => e.Detections[i],
            (e, b) => new CircuitState {
                Wire = e.Wire,
                Detections = new EquatableList<bool>(e.Detections.Impose(b, i).ToArray())
            });
    }

    public Wire Wire;
    public EquatableList<bool> Detections;
    public override string ToString() {
        var s = new List<string>();
        var d = this.Detections;
        if (this.Wire != null) s.Add(this.Wire.ToString());
        s.AddRange(this.Detections.Count.Range().Where(e => d[e]).Select(e => e + ""));
        return String.Join(",", s);
    }
}