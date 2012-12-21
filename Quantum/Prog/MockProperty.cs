using System;

public sealed class MockProperty<TInstance, TField> {
    private readonly Func<TInstance, TField> _getter;
    private readonly Func<TInstance, TField, TInstance> _setter;
    public MockProperty(Func<TInstance, TField> getter, Func<TInstance, TField, TInstance> setter) {
        this._getter = getter;
        this._setter = setter;
    }
    public TField GetValue(TInstance instance) {
        return this._getter(instance);
    }
    public TInstance WithValue(TInstance instance, TField value) {
        return this._setter(instance, value);
    }
}