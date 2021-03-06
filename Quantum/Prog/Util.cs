﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;
using Strilanc.Value;

public static class Util {
    public static Complex Sum(this IEnumerable<Complex> sequence) {
        if (sequence == null) throw new ArgumentNullException("sequence");
        return sequence.Aggregate(Complex.Zero, (a, e) => a + e);
    }
    public static Complex Dot(this IReadOnlyList<Complex> vector1, IReadOnlyList<Complex> vector2) {
        return vector1.Zip(vector2, (e1, e2) => e1*e2).Sum();
    }
    public static IReadOnlyList<T> PaddedTo<T>(this IReadOnlyList<T> items, int minCount, T padding = default(T)) {
        return new AnonymousReadOnlyList<T>(
            () => Math.Min(items.Count, minCount),
            i => i < items.Count ? items[i] : padding);
    }
    public static string StringJoin<T>(this IEnumerable<T> items, string separator) {
        if (items == null) throw new ArgumentNullException("items");
        return string.Join(separator, items);
    }
    public static object ToEquatable<T>(this IEnumerable<T> items) {
        return new EquatableList<T>(items.ToArray());
    }
    public static object ToEquatable<TKey, TVal>(this IReadOnlyDictionary<TKey, TVal> dic) {
        return new EquatableList<object>(dic.OrderBy(e => e.Key.GetHashCode()).Select(e => (object)e).ToArray());
    }
    public static string ToPrettyString(this Complex c) {
        var r = c.Real;
        var i = c.Imaginary;
        if (i == 0) return String.Format("{0:0.###}", r);
        if (r == 0)
            return i == 1 ? "i"
                 : i == -1 ? "-i"
                 : String.Format("{0:0.###}i", i);
        return String.Format(
            "{0:0.###}{1}{2}",
            r == 0 ? (object)"" : r,
            i < 0 ? "-" : "+",
            i == 1 || i == -1 ? "i" : String.Format("{0:0.###}i", Math.Abs(i)));
    }
    public static double SquaredMagnitude(this Complex complex) {
        return complex.Magnitude * complex.Magnitude;
    }
    public static int? IndexOf<T>(this IReadOnlyList<T> list, T item) {
        return list.Count
                   .Range()
                   .Where(i => Equals(list[i], item))
                   .Cast<int?>()
                   .FirstOrDefault();
    }
    public static IReadOnlyList<T> Concat<T>(this IReadOnlyList<T> prefix, IReadOnlyList<T> suffix) {
        return new AnonymousReadOnlyList<T>(
            () => prefix.Count + suffix.Count,
            i => {
                var j = i - prefix.Count;
                return j < 0 ? prefix[i] : suffix[j];
            },
            Enumerable.Concat(prefix, suffix));
    }
    public static IReadOnlyList<T> Impose<T>(this IReadOnlyList<T> list, T item, int index) {
        return new AnonymousReadOnlyList<T>(
            () => list.Count,
            i => i == index ? item : list[i]);
    }
    public static Superposition<T> Super<T>(this T value) {
        return value;
    }
    public static IReadOnlyList<T> Repeat<T>(this T item, int count) {
        return ReadOnlyList.Repeat(item, count);
    }
    public static IReadOnlyList<T> SingletonList<T>(this T item) {
        return ReadOnlyList.Singleton(item);
    }
    
    public static May<TVal> MayGetValue<TKey, TVal>(this IReadOnlyDictionary<TKey, TVal> dictionary, TKey key) {
        if (dictionary == null) throw new ArgumentNullException("dictionary");
        TVal v;
        if (!dictionary.TryGetValue(key, out v)) return May.NoValue;
        return v;
    }
    public static May<TVal> MayGetValue<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, TKey key) {
        if (dictionary == null) throw new ArgumentNullException("dictionary");
        TVal v;
        if (!dictionary.TryGetValue(key, out v)) return May.NoValue;
        return v;
    }

    public static double LerpTo(this double d1, double d2, double p) {
        return d1 * (1 - p) + d2 * p;
    }
    public static float LerpTo(this float d1, float d2, float p) {
        return d1 * (1 - p) + d2 * p;
    }
}