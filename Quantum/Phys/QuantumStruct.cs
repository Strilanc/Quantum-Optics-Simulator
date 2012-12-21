using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;

[DebuggerDisplay("{ToString()}")]
public struct QuantumStruct {
    private readonly IReadOnlyList<IReadOnlyList<object>> _rangeCounts;
    private readonly QuantumInteger _superposition;

    public QuantumStruct(IReadOnlyList<IReadOnlyList<object>> rangeCounts, QuantumInteger superposition) {
        this._rangeCounts = rangeCounts;
        this._superposition = superposition;
    }
    public static IReadOnlyList<object> ValueToValues(IReadOnlyList<IReadOnlyList<object>> _rangeCounts, int n) {
        var x = new object[_rangeCounts.Count];
        foreach (var i in _rangeCounts.Count.Range().Reverse()) {
            x[i] = _rangeCounts[i][n%_rangeCounts[i].Count];
            n /= _rangeCounts[i].Count;
        }
        return x;
    }
    public static int ValuesToValue(IReadOnlyList<IReadOnlyList<object>> _rangeCounts, IReadOnlyList<object> v) {
        var x = 0;
        foreach (var i in _rangeCounts.Count.Range()) {
            if (i > 0) x *= _rangeCounts[i].Count;
            x += _rangeCounts[i].IndexOf(v[i]).Value;
        }
        return x;
    }

    public static QuantumStruct FromPureValue(IReadOnlyList<object> value, IReadOnlyList<IReadOnlyList<object>> rangeCounts) {
        return new QuantumStruct(
            rangeCounts, 
            QuantumInteger.FromPureValue(
                ValuesToValue(rangeCounts, value),
                rangeCounts.Aggregate(1, (e1, e2) => e1 * e2.Count)));
    }

    public QuantumStruct ApplyTransform(Func<IReadOnlyList<object>, IEnumerable<Tuple<IReadOnlyList<object>, Complex>>> bits) {
        var r = _rangeCounts;
        var s = _superposition;
        return new QuantumStruct(
            _rangeCounts,
            new QuantumInteger(
                _superposition.ValueAmplitudes
                .Select((e, i) => 
                    bits(ValueToValues(r, i))
                    .Select(x => 
                        QuantumInteger.FromPureValue(
                            ValuesToValue(r, x.Item1),
                            s.ValueAmplitudes.Count
                        ).Superposition * x.Item2)
                    .Aggregate((e1, e2) => e1 + e2) * e)
                .Aggregate((e1, e2) => e1 + e2)));
    }

    public override string ToString() {
        var r = _rangeCounts;
        return String.Join(
            " + ",
            _superposition.ValueAmplitudes
                .Select((amplitude, state) => {
                    if (amplitude == 0) return null;
                    var states = String.Join(",",ValueToValues(r, state));
                    if (amplitude == 1) return String.Format("|{0}>", states);
                    var s = amplitude.ToPrettyString();
                    if (s.Contains('+') || s.Contains('-')) s = "(" + s + ")";
                    return String.Format("{1} |{0}>", states, s);
                })
                .Where(e => e != null));
    }
}
