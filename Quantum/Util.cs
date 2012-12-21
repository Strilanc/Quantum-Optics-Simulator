using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Strilanc.LinqToCollections;

internal static class Util {
    private const double Tau = Math.PI*2;
    public static Complex Sum(this IEnumerable<Complex> sequence) {
        return sequence.Aggregate(Complex.Zero, (a, e) => a + e);
    }
    public static Complex Dot(this IReadOnlyList<Complex> vector1, IReadOnlyList<Complex> vector2) {
        return vector1.Zip(vector2, (e1, e2) => e1*e2).Sum();
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
    public static V? TryGetValue<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key) where V : struct {
        V v;
        if (!dictionary.TryGetValue(key, out v)) return null;
        return v;
    }
}