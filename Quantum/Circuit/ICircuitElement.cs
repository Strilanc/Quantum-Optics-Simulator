using System.Collections.Generic;

public interface ICircuitElement<T> {
    Superposition<T> Apply(T state);
    IReadOnlyList<Wire> Inputs { get; }
}