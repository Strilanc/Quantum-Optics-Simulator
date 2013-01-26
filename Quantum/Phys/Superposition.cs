using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;

/// <summary>
/// A quantum superposition of values.
/// Possible values are paired with a complex probability amplitude.
/// The real probability of a value is the squared magnitude of its probability.
/// Values paired with a probability amplitude of 0 are omitted.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public struct Superposition<T> {
    private readonly IReadOnlyDictionary<T, Complex> _state;
    public IReadOnlyDictionary<T, Complex> Amplitudes { get { return _state ?? new Dictionary<T, Complex> {{default(T), 1}}; } }
    public IReadOnlyDictionary<T, double> Probabilities { get { return Amplitudes.SelectValue(e => e.Value.SquaredMagnitude()); } }

    public Superposition(IReadOnlyDictionary<T, Complex> state) {
        if (state == null) throw new ArgumentNullException("state");
        //var s = state.Values.Select(e => e.SquaredMagnitude()).Sum();
        //if (Math.Abs(s - 1) > 0.0001)
        //    throw new ArgumentException("Not unitary");
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
        return new Superposition<T>(value.Amplitudes.SelectValue(e => e.Value * factor));
    }
    public static Superposition<T> operator |(Superposition<T> value1, Superposition<T> value2) {
        return FromFragments(
            value1
            .Amplitudes
            .Concat(value2.Amplitudes)
            .Select(e => new KeyValuePair<T, Complex>(e.Key, e.Value * Math.Sqrt(0.5))));
    }
    public static Superposition<T> operator +(Superposition<T> value1, Superposition<T> value2) {
        return FromFragments(
            value1
            .Amplitudes
            .Concat(value2.Amplitudes)
            .Select(e => new KeyValuePair<T, Complex>(e.Key, e.Value)));
    }

    /// <summary>
    /// Fair warning: this function (if it ran in constant time) is strictly more powerful than a quantum computer.
    /// Try to ensure the defined transition corresponds to a unitary matrix, or at least could be by adding sacrifical bits to trash.
    /// </summary>
    public Superposition<TOut> Transform<TOut>(Func<T, Superposition<TOut>> transitions) {
        return Superposition<TOut>.FromFragments(
            Amplitudes
            .SelectMany(e => 
                transitions(e.Key)
                .Amplitudes
                .SelectValue(f => f.Value * e.Value)));
    }

    public override string ToString() {
        return String.Join(
            " + ",
            Amplitudes
                .Select(pair => {
                    if (pair.Value == 0) return null;
                    if (pair.Value == 1) return String.Format("|{0}>", pair.Key);
                    var s = pair.Value.ToPrettyString();
                    if (s.Contains("+") || s.Contains("-")) s = "(" + s + ")";
                    return String.Format("{1} |{0}>", pair.Key, s);
                })
                .Where(e => e != null));
    }
    public object Identity { get { return Amplitudes.ToEquatable(); } }
    public override int GetHashCode() {
        return Identity.GetHashCode();
    }
    public override bool Equals(object obj) {
        if (!(obj is Superposition<T>)) return false;
        var other = (Superposition<T>)obj;
        return Equals(Identity, other.Identity);
    }
}
