using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;

[DebuggerDisplay("{ToString()}")]
public struct ClassicalRegister : IEquatable<ClassicalRegister>, IComparable<ClassicalRegister> {
    private const int BitsPerWord = 32;
    private readonly int _bitCount;
    private readonly UInt32[] _words;
    
    public IReadOnlyList<bool> Bits {
        get {
            if (this._bitCount == 0) 
                return ReadOnlyList.Empty<bool>();

            var s = this._words;
            return new AnonymousReadOnlyList<bool>(
                this._bitCount,
                index => {
                    var wordIndex = index / BitsPerWord;
                    var bitIndex = index % BitsPerWord;
                    return (s[wordIndex] & (1u << bitIndex)) != 0;
                });
        }
    }
    public ClassicalRegister(IReadOnlyList<bool> bits) {
        if (bits == null) throw new ArgumentNullException("bits");

        this._bitCount = bits.Count;
        this._words = 
            bits
                .Partition(BitsPerWord)
                .Select(wordBits => wordBits.Aggregate(
                    0u,
                    (a, e) => (a << 1) | (e ? 1u : 0)))
                .ToArray();
    }
    public static ClassicalRegister Zero(int bitCount) {
        if (bitCount < 0) throw new ArgumentOutOfRangeException("bitCount", "bitCount < 0");
        return new ClassicalRegister(ReadOnlyList.Repeat(false, bitCount));
    }
    public static ClassicalRegister FromUnsignedValue(int bitCount, BigInteger value) {
        if (bitCount < 0) throw new ArgumentOutOfRangeException("bitCount", "bitCount < 0");
        if (value >> bitCount != 0) throw new ArgumentOutOfRangeException("bitCount", "bitCount < value.BitCount");
        if (value < 0) throw new ArgumentOutOfRangeException("value", "value < 0");
        return new ClassicalRegister(bitCount.Range().Select(i => !(value >> i).IsEven));
    }
    public bool Equals(ClassicalRegister other) {
        return other._bitCount == this._bitCount
               && (this._bitCount == 0 || other._words.SequenceEqual(this._words));
    }
    public override int GetHashCode() {
        if (this._bitCount == 0) return 0;
        var r = this._bitCount.GetHashCode();
        foreach (var e in this._words) {
            unchecked {
                r *= 7;
                r += e.GetHashCode();
            }
        }
        return r;
    }
    public BigInteger UnsignedValue { 
        get {
            return this.Bits.Aggregate(BigInteger.Zero, (a, e) => a*2 + (e ? 1 : 0));
        }
    }
    public override bool Equals(object obj) {
        return obj is ClassicalRegister && Equals((ClassicalRegister)obj);
    }
    public int CompareTo(ClassicalRegister other) {
        return this.UnsignedValue.CompareTo(other.UnsignedValue);
    }
    public override string ToString() {
        return String.Join("", this.Bits.Select(e => e ? 1 : 0));
    }
    public static IReadOnlyList<ClassicalRegister> AllOfSize(int bitCount) {
        var count = 1 << bitCount;
        return count.Range().Select(i => FromUnsignedValue(bitCount, i));
    }
    public ClassicalRegister ApplyNot(int bit) {
        return new ClassicalRegister(this.Bits.Select((e, i) => i == bit ? !e : e));
    }
}