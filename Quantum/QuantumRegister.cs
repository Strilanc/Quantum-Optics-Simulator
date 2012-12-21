using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;

/// <summary>
/// Holds the quantum state of multiple bits.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class QuantumRegister {
    private readonly int _bitCount;
    private readonly IReadOnlyDictionary<ClassicalRegister, Complex> _sparseStateAmplitudes;

    public IReadOnlyDictionary<ClassicalRegister, Complex> StateAmplitudes {
        get {
            var s = ClassicalRegister.AllOfSize(this._bitCount);
            return new AnonymousReadOnlyDictionary<ClassicalRegister, Complex>(
                s.Count,
                s,
                (ClassicalRegister k, out Complex v) => {
                    if (k.Bits.Count != this._bitCount) {
                        v = default(Complex);
                        return false;
                    }
                    v = MagnitudeOfState(k);
                    return true;
                });
        }
    } 
    public Complex MagnitudeOfState(ClassicalRegister state) {
        if (state.Bits.Count != this._bitCount) throw new ArgumentException("state.Bits.Count != BitCount", "state");
        if (this._bitCount == 0) return Complex.One;
        return this._sparseStateAmplitudes.TryGetValue(state) ?? 0;
    }
    public int BitCount { get { return this._bitCount; } }
    public QuantumRegister(IReadOnlyDictionary<ClassicalRegister, Complex> sparseStateAmplitudes) {
        if (sparseStateAmplitudes == null)
            throw new ArgumentNullException("sparseStateAmplitudes");
        if (sparseStateAmplitudes.Count == 0)
            throw new ArgumentOutOfRangeException("sparseStateAmplitudes", "sparseStateAmplitudes.Count == 0");
        if (sparseStateAmplitudes.Select(e => e.Key.Bits.Count).Distinct().Count() > 1)
            throw new ArgumentException("Inconsistent bit counts", "sparseStateAmplitudes");
        if (Math.Abs(sparseStateAmplitudes.Values.Select(e => e.SquaredMagnitude()).Sum() - 1) > 0.00001)
            throw new ArgumentException("Inconsistent magnitudes don't have total probability 1", "sparseStateAmplitudes");
        this._bitCount = sparseStateAmplitudes.First().Key.Bits.Count;
        this._sparseStateAmplitudes = sparseStateAmplitudes
            .Where(e => e.Value != 0)
            .ToDictionary(e => e.Key, e => e.Value);
    }
    public static QuantumRegister Pure(ClassicalRegister state) {
        return new QuantumRegister(new Dictionary<ClassicalRegister, Complex> {
            {state, 1}
        });
    }
    public static QuantumRegister Zero(int bitCount) {
        return Pure(ClassicalRegister.Zero(bitCount));
    }
    public override string ToString() {
        if (this._bitCount == 0) return "|>";
        return String.Join(
            " + ",
            this._sparseStateAmplitudes
                .OrderBy(e => e.Key.UnsignedValue)
                .Select(e => {
                    if (e.Value == 1) return e.Key.ToString();
                    var s = e.Value.ToPrettyString();
                    if (s.Contains('+') || s.Contains('-')) s = "(" + s + ")";
                    return String.Format("|{0}>*{1}", e.Key, s);
                }));
    }

    //public QuantumRegister ApplyOperation(ComplexMatrix op) {
    //    return this*op;
    //}
    //public QuantumRegister ApplyOperationToSubSet(IReadOnlyList<ClassicalRegister> bits, ComplexMatrix op) {
    //    var p = ClassicalRegister.AllOfSize(bits.Count);
    //    var vec = p.Select(e => MagnitudeOfState(bits[(int)e.UnsignedValue]));
    //    var tvec = op.Columns.Select(vec.Dot);
    //    var d = new Dictionary<ClassicalRegister, Complex>();
    //    foreach (var e in this._sparseStateAmplitudes)
    //        d[e.Key] = e.Value;
    //    foreach (var i in bits.Count.Range())
    //        d[bits[i]] = tvec[i];
    //    return new QuantumRegister(d);
    //}
    //public QuantumRegister ApplyPauliXGate(int bitIndex) {
    //    return new QuantumRegister(
    //        this._sparseStateAmplitudes.ToDictionary(
    //            e => e.Key.ApplyNot(bitIndex),
    //            e => e.Value));
    //}
    //public QuantumRegister ApplyPauliYGate(int bitIndex) {
    //    return new QuantumRegister(
    //        this._sparseStateAmplitudes.ToDictionary(
    //            e => e.Key.ApplyNot(bitIndex),
    //            e => e.Value * (e.Key.Bits[bitIndex] ? -1 : 1) * Complex.ImaginaryOne));
    //}
    //public QuantumRegister ApplyPauliZGate(int bitIndex) {
    //    return ApplyPhaseGate(bitIndex, Math.PI);
    //}
    //public QuantumRegister ApplyPhaseGate(int bitIndex, double theta) {
    //    return new QuantumRegister(
    //        this._sparseStateAmplitudes.ToDictionary(
    //            e => e.Key,
    //            e => e.Value * (e.Key.Bits[bitIndex] ? Complex.Exp(Complex.ImaginaryOne * theta) : 1)));
    //}
    //public QuantumRegister ApplyHadamardGate(int bitIndex) {
    //    return ApplyPauliXGate(bitIndex).ApplyPauliZGate(bitIndex);
    //}
}