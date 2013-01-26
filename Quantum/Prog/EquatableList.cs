using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Strilanc.LinqToCollections;

public struct EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>> {
    private readonly IReadOnlyList<T> _items;
    public IReadOnlyList<T> Items { get { return this._items ?? ReadOnlyList.Empty<T>(); } }

    public EquatableList(IReadOnlyList<T> items) {
        if (items == null) throw new ArgumentNullException("items");
        this._items = items;
    }
    public IEnumerator<T> GetEnumerator() {
        return this._items.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
    public int Count { get { return this._items.Count; } }
    public T this[int index] { get { return this._items[index]; } }
    public bool Equals(EquatableList<T> other) {
        return this.Items.SequenceEqual(other.Items);
    }
    public override bool Equals(object obj) {
        return obj is EquatableList<T> && Equals((EquatableList<T>)obj);
    }
    public override int GetHashCode() {
        return this.Items.Aggregate(
            this.Items.Count.GetHashCode(),
            (a, e) => {
                unchecked {
                    return a * 13 + e.GetHashCode();
                }
            });
    }
}