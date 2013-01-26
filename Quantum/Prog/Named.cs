using System;
using System.Diagnostics;

[DebuggerDisplay("{ToString()}")]
public struct Named<T> {
    private readonly T _value;
    private readonly string _name;
    public T Value { get { return _value; } }
    public string Name { get { return _name ?? String.Format("default({0})", typeof(Named<>).Name); } }

    public Named(T value, string name) {
        if (name == null) throw new ArgumentNullException("name");
        this._value = value;
        this._name = name;
    }

    public Named<R> Select<R>(Func<T, R> proj) {
        return new Named<R>(proj(_value), Name);
    }

    public override bool Equals(object obj) {
        if (!(obj is Named<T>)) return false;
        var other = (Named<T>)obj;
        return Equals(_value, other._value)
            && Equals(_name, other._name);
    }
    public override int GetHashCode() {
        unchecked {
            return Value.GetHashCode() + (Name.GetHashCode() * 3);
        }
    }
    public override string ToString() {
        return string.Format("{0}: {1}", Name, Value);
    }
}
