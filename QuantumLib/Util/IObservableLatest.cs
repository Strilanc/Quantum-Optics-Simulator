using System;

///<summary>An observable that has a current value.</summary>
public interface IObservableLatest<out T> : IObservable<T> {
    T Current { get; }
}