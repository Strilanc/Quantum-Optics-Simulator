using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

static class Program {
    static void Main(string[] args) {
        Circuit2();
        Circuit3();
    }
    public static F Get<T, F>(this T instance, MockProperty<T, F> property) {
        return property.GetValue(instance);
    }
    public static BeamSplitter<T> Split<T>(this MockProperty<T, Wire> wireProperty, Wire input, Wire throughput, Wire reflectput) {
        return new BeamSplitter<T>(input, throughput, reflectput, wireProperty);
    }
    public static CrossBeamSplitter<T> CrossSplit<T>(this MockProperty<T, Wire> wireProperty, Wire inputH, Wire inputV, Wire outputH, Wire outputV) {
        return new CrossBeamSplitter<T>(inputH, inputV, outputH, outputV, wireProperty);
    }
    public static Reflector<T> Reflect<T>(this MockProperty<T, Wire> wireProperty, Wire input, Wire output) {
        return new Reflector<T>(input, output, wireProperty);
    }
    public static Detector<T> Detect<T>(this MockProperty<T, Wire> wireProperty, MockProperty<T, bool> detectedProperty, Wire input, Wire output = null) {
        return new Detector<T>(input, output, detectedProperty, wireProperty);
    }
    public static T With<T, F>(this T instance, MockProperty<T, F> property, F field) {
        return property.WithValue(instance, field);
    }
    public interface ICircuitElement<T> {
        Superposition<T> Apply(T state);
        IReadOnlyList<Wire> Inputs { get; }
    }
    public sealed class MockProperty<TInstance, TField> {
        private readonly Func<TInstance, TField> _getter;
        private readonly Func<TInstance, TField, TInstance> _setter;
        public MockProperty(Func<TInstance, TField> getter, Func<TInstance, TField, TInstance> setter) {
            _getter = getter;
            _setter = setter;
        }
        public TField GetValue(TInstance instance) {
            return _getter(instance);
        }
        public TInstance WithValue(TInstance instance, TField value) {
            return _setter(instance, value);
        }
    }
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
            if (state.Get(WireProperty) == Input)
                return state.With(WireProperty, Output).With(Detected, true);
            return state;
        }
        public IReadOnlyList<Wire> Inputs { get { return Input.SingletonList(); } }
    }
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
            if (state.Get(WireProperty) == Input)
                return state.With(WireProperty, OutputPass).Super()
                     + state.With(WireProperty, OutputReflect).Super() * Complex.ImaginaryOne;
            return state;
        }
        public IReadOnlyList<Wire> Inputs { get { return Input.SingletonList(); } }
    }
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
            if (state.Get(WireProperty) == InputV)
                return new BeamSplitter<T>(InputV, OutputV, OutputH, WireProperty).Apply(state);
            if (state.Get(WireProperty) == InputH)
                return new BeamSplitter<T>(InputH, OutputH, OutputV, WireProperty).Apply(state);
            return state;
        }
        public IReadOnlyList<Wire> Inputs { get { return new[] {InputV, InputH}; } }
    }
    public sealed class Reflector<T> : ICircuitElement<T> {
        public readonly Wire Input;
        public readonly Wire Output;
        public readonly MockProperty<T, Wire> WireProperty;
        public Reflector(Wire input, Wire output, MockProperty<T, Wire> wireProperty) {
            this.Input = input;
            this.Output = output;
            this.WireProperty = wireProperty;
        }
        public Superposition<T> Apply(T state) {
            if (state.Get(WireProperty) == Input)
                return state.With(WireProperty, Output).Super() * Complex.ImaginaryOne;
            return state;
        }
        public IReadOnlyList<Wire> Inputs { get { return Input.SingletonList(); } }
    }
    public struct CircuitState {
        public Wire Wire;
        public bool Detect1;
        public bool Detect2;
        public bool Detect3;
        public bool Detect4;
        public CircuitState With(bool? d1 = null, bool? d2 = null, bool? d3 = null, bool? d4 = null) {
            return new CircuitState {
                Wire = Wire,
                Detect1 = d1 ?? this.Detect1,
                Detect2 = d2 ?? this.Detect2,
                Detect3 = d3 ?? this.Detect3,
                Detect4 = d4 ?? this.Detect4
            };
        }
        public CircuitState With(Wire wire, bool? d1 = null, bool? d2 = null, bool? d3 = null, bool? d4 = null) {
            return new CircuitState {
                Wire = wire,
                Detect1 = d1 ?? this.Detect1,
                Detect2 = d2 ?? this.Detect2,
                Detect3 = d3 ?? this.Detect3,
                Detect4 = d4 ?? this.Detect4
            };
        }
        public override string ToString() {
            var s = new List<string>();
            if (Wire != null) s.Add(Wire.ToString());
            if (Detect1) s.Add("1");
            if (Detect2) s.Add("2");
            if (Detect3) s.Add("3");
            if (Detect4) s.Add("4");
            return String.Join(",", s);
        }
    }
    public sealed class Wire {
        public readonly string Name;
        public Wire(string name = null) {
            this.Name = name;
        }
        public override string ToString() {
            return Name ?? base.ToString();
        }
    }
    public static void Circuit2() {
        // S--- 0 -----> A ---- 1 -----> C
        //               |               |
        //               2               4
        //               |               |
        //               v               v
        //               B ------ 3 ---> D --- 5 --> F
        //                               |
        //                               6
        //                               |
        //                               v
        //                               E

        var SA = new Wire("SA");
        var AC = new Wire("AC");
        var AB = new Wire("AB");
        var BD = new Wire("BD");
        var CD = new Wire("CD");
        var DE = new Wire("DE");
        var DF = new Wire("DF");

        var wp = new MockProperty<CircuitState, Wire>(e => e.Wire, (e, w) => e.With(w));
        var A = wp.Split(input: SA, throughput: AC, reflectput: AB);
        var B = wp.Reflect(input: AB, output: BD);
        var C = wp.Reflect(input: AC, output: CD);
        var D = wp.CrossSplit(inputH: BD, inputV: CD, outputH: DF, outputV: DE);
        var E = wp.Detect(input: DF, detectedProperty: new MockProperty<CircuitState, bool>(e => e.Detect1, (e, b) => e.With(d1: b)));
        var F = wp.Detect(input: DE, detectedProperty: new MockProperty<CircuitState, bool>(e => e.Detect2, (e, b) => e.With(d2: b)));
        var elements = new ICircuitElement<CircuitState>[] {A, B, C, D, E, F};
        
        var state = new CircuitState { Wire = SA }.Super();
        while (true) {
            var activeWires = new HashSet<Wire>(state.Amplitudes.Keys.Select(e => e.Wire).Where(e => e != null));
            var activeElements = elements.Where(e => e.Inputs.Any(activeWires.Contains)).ToArray();
            if (activeElements.Length == 0) break;
            foreach (var activeElement in elements.Where(e => e.Inputs.Any(activeWires.Contains))) {
                var newState = state.ApplyTransform(activeElement.Apply);
                state = newState;
            }
        }
    }

    public static void Circuit3() {
        // *>--A\-------\\B   
        //      |       |           
        //      |       |           
        //      |       |           
        //    C\\------D\-----E\-------\\F
        //              |      |       |
        //              |      |       |
        //              |      |       |
        //              1    G\\---2--H\------3
        //                             |
        //                             |
        //                             |
        //                             4

        var _A = new Wire("_A");
        var AB = new Wire("AB");
        var AC = new Wire("AC");
        var BD = new Wire("BD");
        var CD = new Wire("CD");
        var DE = new Wire("DE");
        var D1 = new Wire("D1");
        var EF = new Wire("EF");
        var EG = new Wire("EG");
        var FH = new Wire("FH");
        var G2 = new Wire("G2");
        var _2H = new Wire("_2H");
        var H3 = new Wire("H3");
        var H4 = new Wire("H4");

        var wp = new MockProperty<CircuitState, Wire>(e => e.Wire, (e, w) => e.With(w));
        var A = wp.Split(input: _A, throughput: AB, reflectput: AC);
        var B = wp.Reflect(input: AB, output: BD);
        var C = wp.Reflect(input: AC, output: CD);
        var D = wp.CrossSplit(inputH: CD, inputV: BD, outputH: DE, outputV: D1);
        var E = wp.Split(input: DE, throughput: EF, reflectput: EG);
        var F = wp.Reflect(input: EF, output: FH);
        var _1 = wp.Detect(input: D1, detectedProperty: new MockProperty<CircuitState, bool>(e => e.Detect1, (e, b) => e.With(d1: b)));
        var G = wp.Reflect(input: EG, output: G2);
        var _2 = wp.Detect(input: G2, output: _2H, detectedProperty: new MockProperty<CircuitState, bool>(e => e.Detect2, (e, b) => e.With(d2: b)));
        var H = wp.CrossSplit(inputH: _2H, inputV: FH, outputH: H3, outputV: H4);
        var _3 = wp.Detect(input: H3, detectedProperty: new MockProperty<CircuitState, bool>(e => e.Detect3, (e, b) => e.With(d3: b)));
        var _4 = wp.Detect(input: H4, detectedProperty: new MockProperty<CircuitState, bool>(e => e.Detect4, (e, b) => e.With(d4: b)));
        var elements = new ICircuitElement<CircuitState>[] { A, B, C, D, E, F, _1, G, _2, H, _3, _4 };

        var state = new CircuitState { Wire = _A }.Super();
        while (true) {
            var activeWires = new HashSet<Wire>(state.Amplitudes.Keys.Select(e => e.Wire).Where(e => e != null));
            var activeElements = elements.Where(e => e.Inputs.Any(activeWires.Contains)).ToArray();
            if (activeElements.Length == 0) break;
            foreach (var activeElement in elements.Where(e => e.Inputs.Any(activeWires.Contains))) {
                var newState = state.ApplyTransform(activeElement.Apply);
                state = newState;
            }
        }
    }
}
