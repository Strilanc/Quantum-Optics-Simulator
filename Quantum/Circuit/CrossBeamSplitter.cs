using System.Collections.Generic;

public sealed class CrossBeamSplitter<T> : ICircuitElement<T> {
    public readonly Wire InputV;
    public readonly Wire InputH;
    public readonly Wire OutputV;
    public readonly Wire OutputH;
    public readonly MockProperty<T, Wire> WireProperty;
    public CrossBeamSplitter(Wire inputH, Wire inputV, Wire outputH, Wire outputV, MockProperty<T, Wire> wireProperty) {
        this.InputH = inputH;
        this.InputV = inputV;
        this.OutputH = outputH;
        this.OutputV = outputV;
        this.WireProperty = wireProperty;
    }
    public Superposition<T> Apply(T state) {
        if (state.Get(this.WireProperty) == this.InputV)
            return new BeamSplitter<T>(this.InputV, this.OutputV, this.OutputH, this.WireProperty).Apply(state);
        if (state.Get(this.WireProperty) == this.InputH)
            return new BeamSplitter<T>(this.InputH, this.OutputH, this.OutputV, this.WireProperty).Apply(state);
        return state;
    }
    public IReadOnlyList<Wire> Inputs { get { return new[] { this.InputV, this.InputH }; } }
}