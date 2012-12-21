using System.Collections.Generic;
using System.Numerics;

public sealed class BeamSplitter<T> : ICircuitElement<T> {
    public readonly Wire Input;
    public readonly Wire OutputPass;
    public readonly Wire OutputReflect;
    public readonly MockProperty<T, Wire> WireProperty;
    public BeamSplitter(Wire input, Wire outputPass, Wire outputReflect, MockProperty<T, Wire> wireProperty) {
        this.Input = input;
        this.OutputPass = outputPass;
        this.OutputReflect = outputReflect;
        this.WireProperty = wireProperty;
    }
    public Superposition<T> Apply(T state) {
        if (state.Get(this.WireProperty) == this.Input)
            return state.With(this.WireProperty, this.OutputPass).Super()
                   + state.With(this.WireProperty, this.OutputReflect).Super() * Complex.ImaginaryOne;
        return state;
    }
    public IReadOnlyList<Wire> Inputs { get { return this.Input.SingletonList(); } }
}