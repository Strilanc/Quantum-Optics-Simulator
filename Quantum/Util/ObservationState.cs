using System;

public struct ObservationState<T> {
    public T Item;
    public Exception Error;
    public bool Completion;
    public T ThrowIfDone() {
        if (Completion || Error != null) throw new InvalidOperationException();
        return Item;
    }
}