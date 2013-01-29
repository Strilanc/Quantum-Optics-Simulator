using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using TwistedOak.Collections;
using TwistedOak.Util;

public static class Util2 {
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
    public static int FloorInt(this double d) {
        return (int)Math.Floor(d);
    }
    public static IObservable<T> Overlap<T>(this IObservable<T> observable, IObservable<T> other) {
        if (observable == null) throw new ArgumentNullException("observable");
        if (other == null) throw new ArgumentNullException("other");
        return new[] { observable, other }.ToObservable().SelectMany(e => e);
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
        return observable.Where(e => e.HasValue).Select(e => e.GetValueOrDefault());
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