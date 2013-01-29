using System;
using System.Diagnostics;

[DebuggerStepThrough]
public sealed class AnonymousObservableLatest<T> : IObservableLatest<T> {
    private readonly Func<IObserver<T>, IDisposable> _subsribe;
    private readonly Func<T> _current;
    public AnonymousObservableLatest(Func<IObserver<T>, IDisposable> subsribe, Func<T> current) {
        if (subsribe == null) throw new ArgumentNullException("subsribe");
        if (current == null) throw new ArgumentNullException("current");
        _subsribe = subsribe;
        _current = current;
    }
    public IDisposable Subscribe(IObserver<T> observer) {
        if (observer == null) throw new ArgumentNullException("observer");
        return _subsribe(observer);
    }
    public T Current { get { return _current(); } }
}