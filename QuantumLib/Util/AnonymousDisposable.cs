using System;
using System.Diagnostics;

/// <summary>
/// A disposable built from delegates. 
/// Guarantees the dispose action is only run once.
/// Exposes whether or not it has been disposed.
/// </summary>
[DebuggerStepThrough]
public sealed class AnonymousDisposable : IDisposable {
    private readonly Action _action;
    private readonly OnetimeLock _canDisposeLock = new OnetimeLock();
    public bool IsDisposed { get { return _canDisposeLock.IsAcquired(); } }
    /// <summary>
    /// Creates an anonymous disposable, that will call the given action, when disposed for the first time.
    /// Defaults to doing nothing on disposal, when a null action is given.
    /// </summary>
    public AnonymousDisposable(Action action = null) {
        this._action = action ?? (() => { });
    }
    public void Dispose() {
        if (!_canDisposeLock.TryAcquire()) return;
        _action();
    }
}
