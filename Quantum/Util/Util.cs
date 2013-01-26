using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using SharpDX;
using System.Reactive.Linq;
using Strilanc.Value;
using TwistedOak.Collections;
using TwistedOak.Util;

public static class Util2 {
    public static void DisposeUnlessNull(this IDisposable v) {
        if (v != null) v.Dispose();
    }
    public static void NullSafeRemoveAndDispose<T>(this DisposeCollector collector, ref T r) {
        if (ReferenceEquals(r, null)) return;
        collector.RemoveAndDispose(ref r);
    }
    public static void NullSafeRemoveAndDispose<T>(this DisposeCollector collector, T r) {
        collector.NullSafeRemoveAndDispose(ref r);
    }
    public static void InvokeUnlessNull<T>(this Action<T> action, T arg) {
        if (action != null) action(arg);
    }
    public static void InvokeUnlessNull(this Action action) {
        if (action != null) action();
    }
    public static IObservable<TOut> SelectManyUntilNext<TIn, TOut>(this IObservable<TIn> observable, Func<TIn, IObservable<TOut>> projection) {
        if (observable == null) throw new ArgumentNullException("observable");
        if (projection == null) throw new ArgumentNullException("projection");
        return observable.Select(projection).SelectManyUntilNext();
    }
    public static IObservable<T> SelectManyUntilNext<T>(this IObservable<IObservable<T>> observable) {
        if (observable == null) throw new ArgumentNullException("observable");
        return observable
            .WithInlineLatestLifetime()
            .SelectMany(e => e.Value.TakeUntilDead(e.Lifetime));
    }
    public static IObservable<T> TakeUntilDead<T>(this IObservable<T> observable, Lifetime lifetime) {
        if (observable == null) throw new ArgumentNullException("observable");
        
        // avoid wrapping, when possible
        if (lifetime.IsDead) return new T[0].ToObservable();
        if (lifetime.IsImmortal) return observable;
        
        return new AnonymousObservable<T>(observer => {
            // avoid wrapping, when possible
            if (lifetime.IsImmortal) {
                return observable.Subscribe(observer);
            }
            if (lifetime.IsDead) {
                observer.OnCompleted();
                return new AnonymousDisposable();
            }

            // subscribe to underlying observable
            var incomplete = new LifetimeSource();
            var syncRoot = new object();
            observable.Subscribe(
                e => {
                    lock (syncRoot) {
                        if (incomplete.Lifetime.IsDead) return;
                        observer.OnNext(e);
                    }
                },
                ex => {
                    lock (syncRoot) {
                        if (incomplete.Lifetime.IsDead) return;
                        incomplete.EndLifetime();
                        observer.OnError(ex);
                    }
                },
                () => {
                    lock (syncRoot) {
                        if (incomplete.Lifetime.IsDead) return;
                        incomplete.EndLifetime();
                        observer.OnCompleted();
                    }
                },
                incomplete.Lifetime);

            // end early when lifetime dies
            lifetime.WhenDead(
                () => {
                    lock (syncRoot) {
                        if (incomplete.Lifetime.IsDead) return;
                        incomplete.EndLifetime();
                        observer.OnCompleted();
                    }
                },
                incomplete.Lifetime);

            return new AnonymousDisposable(incomplete.EndLifetime);
        });
    }
    public static IObservable<T> WhereNotNull<T>(this IObservable<T> observable) {
        return observable.Where(e => !ReferenceEquals(e, null));
    }
    public static IObservable<T> WhereNotNull<T>(this IObservable<T?> observable) where T : struct {
        return observable.Where(e => e.HasValue).Select(e => e.Value);
    }
    public static IObservable<T> When<T>(this IObservable<T> observable, Func<bool> filter) {
        return observable.Where(e => filter());
    }
    public static IObservable<T> Overlap<T>(this IObservable<T> observable, IObservable<T> other) {
        return new[] {observable, other}.ToObservable().SelectMany(e => e);
    }
    public static IObservable<Unit> ToUnitValues<T>(this IObservable<T> observable) {
        return observable.Select(e => default(Unit));
    }

    public static IObservableLatest<TOut> Consume<TIn, TOut>(this IObservable<TIn> observable, Func<TIn, TOut> projection) where TOut : IDisposable {
        return observable.SelectDefaultIfNull(projection).ConsumeIntoValueWithDisposal();
    }
    public static IObservableLatest<TOut> Consume<TIn, TOut>(this IObservable<TIn?> observable, Func<TIn, TOut> projection) where TIn : struct where TOut : IDisposable {
        return observable.SelectDefaultIfNull(projection).ConsumeIntoValueWithDisposal();
    }
    public static IObservable<TOut> SelectDefaultIfNull<TIn, TOut>(this IObservable<TIn> observable, Func<TIn, TOut> projection) {
        return observable.Select(e => ReferenceEquals(e, null) ? default(TOut) : projection(e));
    }
    public static IObservable<TOut> SelectDefaultIfNull<TIn, TOut>(this IObservable<TIn?> observable, Func<TIn, TOut> projection) where TIn : struct {
        return observable.Select(e => ReferenceEquals(e, null) ? default(TOut) : projection(e.Value));
    }
    public static IObservableLatest<TOut> Select<TIn, TOut>(this IObservableLatest<TIn> observable, Func<TIn, TOut> projection) {
        if (observable == null) throw new ArgumentNullException("observable");
        if (projection == null) throw new ArgumentNullException("projection");
        return new AnonymousObservableLatest<TOut>(
            observer => Observable.Select(observable, projection).Subscribe(observer),
            () => projection(observable.Current));
    }
    ///<summary>Returns an observable that pairs contiguous items from the given underlying observable.</summary>
    public static IObservable<Transition<T>> Transitions<T>(this IObservable<T> observable) {
        if (observable == null) throw new ArgumentNullException("observable");
        return new AnonymousObservable<Transition<T>>(observer => {
            var prev = May<T>.NoValue;
            return observable.Synchronize().Subscribe(
                newValue => {
                    prev.IfHasValueThenDo(prevValue => observer.OnNext(new Transition<T>(prevValue, newValue)));
                    prev = newValue;
                },
                observer.OnError,
                observer.OnCompleted);
        });
    }
    public static IObservable<T> CutShort<T>(this IObservable<T> observable, Lifetime lifetime) {
        return new AnonymousObservable<T>(observer => {
            var d = observable.Subscribe(observer);
            lifetime.WhenDead(d.Dispose);
            return d;
        });
    }
    public static IObservable<T> WithInlineDisposalOnChange<T>(this IObservable<T> observable) where T : IDisposable {
        if (observable == null) throw new ArgumentNullException("observable");
        return new AnonymousObservable<T>(observer => {
            var last = default(T);
            return observable
                .Synchronize()
                .Select(e => {
                    if (!ReferenceEquals(last, null) && !ReferenceEquals(last, e)) {
                        last.Dispose();
                    }
                    last = e;
                    return e;
                })
                .WithDoAfter(() => { if (!ReferenceEquals(last, null)) last.Dispose(); })
                .Subscribe(observer);
        });
    }
    public static IObservable<Perishable<T>> WithInlineLatestLifetime<T>(this IObservable<T> observable) {
        return new AnonymousObservable<Perishable<T>>(observer => {
            var r = new LifetimeExchanger();
            return observable
                .Synchronize()
                .Select(e => new Perishable<T>(e, r.StartNextAndEndPreviousLifetime()))
                .WithDoAfter(() => r.StartNextAndEndPreviousLifetime())
                .Subscribe(observer);
        });
    }
    public static IObservable<T> WithDoAfter<T>(this IObservable<T> observable, Action onCompleteOrError) {
        return new AnonymousObservable<T>(observer => observable.Subscribe(
            observer.OnNext,
            ex => {
                onCompleteOrError();
                observer.OnError(ex);
            },
            () => {
                onCompleteOrError();
                observer.OnCompleted();
            }));
    }
    public static IObservableLatest<T> ConsumeIntoValueWithDisposal<T>(this IObservable<T> observable, T initialLatest = default(T), Lifetime lifetime = default(Lifetime)) where T : IDisposable {
        return observable.WithInlineDisposalOnChange().SubscribeObserveLatest(initialLatest, lifetime);
    }
    public static IObservableLatest<T> SubscribeObserveLatest<T>(this IObservable<T> observable, T initialLatest = default(T), Lifetime lifetime = default(Lifetime)) {
        var state = new ObservationState<T> { Item = initialLatest };
        var subject = new Subject<T>();
        var syncRoot = new object();

        observable.Synchronize(syncRoot).Subscribe(
            e => {
                state = new ObservationState<T> {Item = e};
                subject.OnNext(e);
            },
            ex => {
                if (ex == null) throw new InvalidOperationException("Null exception propatated through observable.");
                state = new ObservationState<T> {Error = ex};
                subject.OnError(ex);
            },
            () => {
                state = new ObservationState<T> {Completion = true};
                subject.OnCompleted();
            },
            lifetime);

        return new AnonymousObservableLatest<T>(
            observer => {
                lock (syncRoot) {
                    if (state.Completion) {
                        observer.OnCompleted();
                        return new AnonymousDisposable();
                    }

                    if (state.Error != null) {
                        observer.OnError(state.Error);
                        return new AnonymousDisposable();
                    }

                    observer.OnNext(state.Item);
                    return subject.Subscribe(observer);
                }
            },
            () => {
                lock (syncRoot)
                    return state.ThrowIfDone();
            });
    }
}
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
public struct ObservationState<T> {
    public T Item;
    public Exception Error;
    public bool Completion;
    public T ThrowIfDone() {
        if (Completion || Error != null) throw new InvalidOperationException();
        return Item;
    }
}
public struct Transition<T> {
    public readonly T Old;
    public readonly T New;
    public Transition(T old, T @new) {
        this.Old = old;
        this.New = @new;
    }
}
