using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;

///<summary>A superposition of integer values in the range [0, n), for some positive n.</summary>
[DebuggerDisplay("{ToString()}")]
public struct QuantumInteger {
    private readonly ComplexVector _superposition;
    public ComplexVector Superposition { get { return _superposition.Values.Count == 0 ? new ComplexVector(Complex.One.SingletonList()) : _superposition; } }

    public IReadOnlyList<Complex> ValueAmplitudes { get { return Superposition.Values; } }
    public IReadOnlyList<double> ValueProbabilities { get { return ValueAmplitudes.Select(e => e.SquaredMagnitude()); } }

    public QuantumInteger(ComplexVector superposition) {
        if (Math.Abs(superposition.Values.Select(e => e.SquaredMagnitude()).Sum() - 1) > 0.0001)
            throw new ArgumentException("Not unitary");
        this._superposition = superposition;
    }
    public static QuantumInteger FromPureValue(int value, int valuePossibleRangeCount) {
        if (value < 0) throw new ArgumentOutOfRangeException("value", "value < 0");
        if (valuePossibleRangeCount < value) throw new ArgumentOutOfRangeException("valuePossibleRangeCount", "valuePossibleRangeCount < value");
        return new QuantumInteger(new ComplexVector(
            Complex.Zero
            .Repeat(valuePossibleRangeCount)
            .Impose(Complex.One, value)));
    }
    public static QuantumInteger FromPureZero(int valuePossibleRangeCount) {
        return FromPureValue(0, valuePossibleRangeCount);
    }

    private ComplexVector Subset(IReadOnlyList<int> bits) {
        var v = ValueAmplitudes;
        return new ComplexVector(bits.Select(i => v[i]));
    }
    private QuantumInteger Impose(ComplexVector subset, IReadOnlyList<int> bits) {
        var r = ValueAmplitudes.ToArray();
        foreach (var i in bits.Count.Range())
            r[bits[i]] = subset.Values[i];
        return new QuantumInteger(new ComplexVector(r));
    }

    public QuantumInteger ApplyOperation(ComplexMatrix op) {
        return new QuantumInteger(Superposition*op);
    }
    public QuantumInteger ApplyOperationToSubset(ComplexMatrix op, IReadOnlyList<int> bits) {
        return Impose(Subset(bits) * op, bits);
    }
    public QuantumInteger ApplyTransform(Func<int, ComplexVector> bits) {
        return new QuantumInteger(ValueAmplitudes.Select((e, i) => bits(i) * e).Aggregate((e1, e2) => e1 + e2));
    }
    public QuantumInteger ApplyTransform(Func<int, IReadOnlyList<Complex>> bits) {
        return ApplyTransform(e => new ComplexVector(bits(e)));
    }

    public override string ToString() {
        return String.Join(
            " + ",
            ValueAmplitudes
                .Select((amplitude, state) => {
                    if (amplitude == 0) return null;
                    if (amplitude == 1) return String.Format("|{0}>", state);
                    var s = amplitude.ToPrettyString();
                    if (s.Contains('+') || s.Contains('-')) s = "(" + s + ")";
                    return String.Format("{1} |{0}>", state, s);
                })
                .Where(e => e != null));
    }
}
