using System.Collections.Generic;

public sealed class Propagater<T> : ICircuitElement<T> {
    public readonly Wire Input;
    public readonly Wire Output;
    public readonly MockProperty<T, Wire> WireProperty;
    public Propagater(Wire input, Wire output, MockProperty<T, Wire> wireProperty) {
        this.Input = input;
        this.Output = output;
        this.WireProperty = wireProperty;
    }
    public Superposition<T> Apply(T state) {
        if (state.Get(this.WireProperty) == this.Input)
            return state.With(this.WireProperty, this.Output);
        return state;
    }
    public IReadOnlyList<Wire> Inputs { get { return this.Input.SingletonList(); } }
}