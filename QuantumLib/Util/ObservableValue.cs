using System;

///<summary>A value that can be changed and observed.</summary>
public class ObservableValue<T> : IObservableLatest<T> {
    private T _current;
    private event Action<T> Updated;
    private readonly object _lock = new object();
    public T Current { get { lock (_lock) return _current; } }
    
    public ObservableValue(T initialValue = default(T)) {
        this._current = initialValue;
    }
    
    public IDisposable Subscribe(IObserver<T> observer) {
        if (observer == null) throw new ArgumentNullException("observer");
        lock (_lock) {
            Updated += observer.OnNext;
            observer.OnNext(_current);
        }
        return new AnonymousDisposable(() => Updated -= observer.OnNext);
    }
    public void Update(T newValue, bool skipIfEqual = true) {
        lock (_lock) {
            if (skipIfEqual && Equals(_current, newValue)) return;
            _current = newValue;
            var u = Updated;
            if (u != null) u(newValue);
        }
    }
    public void Adjust(Func<T, T> adjustment, bool skipIfEqual = true) {
        if (adjustment == null) throw new ArgumentNullException("adjustment");
        lock (_lock) {
            Update(adjustment(_current), skipIfEqual);
        }
    }
}
