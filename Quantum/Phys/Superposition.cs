using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;

[DebuggerDisplay("{ToString()}")]
public struct Superposition<T> {
    private readonly IReadOnlyDictionary<T, Complex> _state;
    public IReadOnlyDictionary<T, Complex> Amplitudes { get { return _state ?? new Dictionary<T, Complex> {{default(T), 1}}; } }
    public IReadOnlyDictionary<T, double> Probabilities { get { return Amplitudes.Select(e => e.Value.SquaredMagnitude()); } }

    public Superposition(IReadOnlyDictionary<T, Complex> state) {
        if (state == null) throw new ArgumentNullException("state");
        if (Math.Abs(state.Values.Select(e => e.SquaredMagnitude()).Sum() - 1) > 0.0001)
            throw new ArgumentException("Not unitary");
        this._state = state;
    }
    public static Superposition<T> FromPureValue(T value) {
        return new Superposition<T>(new Dictionary<T, Complex> {{value, 1}});
    }
    public static Superposition<T> FromFragments(IEnumerable<KeyValuePair<T, Complex>> fragments) {
        return new Superposition<T>(
            fragments
            .GroupBy(e => e.Key, e => e.Value)
            .Select(e => new KeyValuePair<T, Complex>(e.Key, e.Sum()))
            .Where(e => e.Value != 0)
            .ToDictionary(e => e.Key, e => e.Value));
    } 

    public static implicit operator Superposition<T>(T value) {
        return FromPureValue(value);
    }

    public static Superposition<T> operator *(Superposition<T> value, Complex factor) {
        return new Superposition<T>(value.Amplitudes.Select(e => e.Value * factor));
    }
    public static Superposition<T> operator +(Superposition<T> value1, Superposition<T> value2) {
        return FromFragments(
            value1
            .Amplitudes
            .Concat(value2.Amplitudes)
            .Select(e => new KeyValuePair<T, Complex>(e.Key, e.Value * Math.Sqrt(0.5))));
    }

    public Superposition<T> Transform(Func<T, Superposition<T>> transitions) {
        return FromFragments(
            Amplitudes
            .SelectMany(e => 
                transitions(e.Key)
                .Amplitudes
                .Select(f => f.Value * e.Value)));
    }

    public override string ToString() {
        return String.Join(
            " + ",
            Amplitudes
                .Select(pair => {
                    if (pair.Value == 0) return null;
                    if (pair.Value == 1) return String.Format("|{0}>", pair.Key);
                    var s = pair.Value.ToPrettyString();
                    if (s.Contains('+') || s.Contains('-')) s = "(" + s + ")";
                    return String.Format("{1} |{0}>", pair.Key, s);
                })
                .Values
                .Where(e => e != null));
    }
}
