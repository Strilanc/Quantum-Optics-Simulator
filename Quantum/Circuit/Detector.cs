using System.Collections.Generic;

public sealed class Detector<T> : ICircuitElement<T> {
    public readonly Wire Input;
    public readonly Wire Output;
    public readonly MockProperty<T, bool> Detected;
    public readonly MockProperty<T, Wire> WireProperty;
    public Detector(Wire input, Wire output, MockProperty<T, bool> detected, MockProperty<T, Wire> wireProperty) {
        this.Input = input;
        this.Output = output;
        this.Detected = detected;
        this.WireProperty = wireProperty;
    }
    public Superposition<T> Apply(T state) {
        if (state.Get(this.WireProperty) == this.Input)
            return state.With(this.WireProperty, this.Output).With(this.Detected, true);
        return state;
    }
    public IReadOnlyList<Wire> Inputs { get { return this.Input.SingletonList(); } }
}