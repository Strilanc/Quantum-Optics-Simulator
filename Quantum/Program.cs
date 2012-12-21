using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

static class Program {
    static void Main(string[] args) {
        Circuit2();
    }
    public class CircuitNode {
        public string type;
        public Tuple<CircuitNode, string>[] Children;
    }
    public interface ICircuitElement {
        Superposition<CircuitState> Apply(CircuitState state);
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
    public class Detector : ICircuitElement {
        public Wire Input;
        public Wire Output;
        public MockProperty<CircuitState, bool> Detected;
        public Superposition<CircuitState> Apply(CircuitState state) {
            if (state.Wire != Input) return state;
            return Detected.WithValue(state.With(wire: Output), true);
        }
        public IReadOnlyList<Wire> Inputs { get { return Input.SingletonList(); } }
    }
    public class BeamSplitter : ICircuitElement {
        public readonly Wire Input;
        public readonly Wire OutputPass;
        public readonly Wire OutputReflect;
        public BeamSplitter(Wire input, Wire outputPass, Wire outputReflect) {
            this.Input = input;
            this.OutputPass = outputPass;
            this.OutputReflect = outputReflect;
        }
        public Superposition<CircuitState> Apply(CircuitState state) {
            if (state.Wire != Input) return state;
            return state.With(wire: OutputPass).Super()
                 + state.With(wire: OutputReflect).Super() * Complex.ImaginaryOne;
        }
        public IReadOnlyList<Wire> Inputs { get { return Input.SingletonList(); } }
    }
    public class CrossBeamSplitter : ICircuitElement {
        public readonly Wire InputV;
        public readonly Wire InputH;
        public readonly Wire OutputV;
        public readonly Wire OutputH;
        public CrossBeamSplitter(Wire inputH, Wire inputV, Wire outputH, Wire outputV) {
            this.InputH = inputH;
            this.InputV = inputV;
            this.OutputH = outputH;
            this.OutputV = outputV;
        }
        public Superposition<CircuitState> Apply(CircuitState state) {
            if (state.Wire == InputV)
                return new BeamSplitter(InputV, OutputV, OutputH).Apply(state);
            if (state.Wire == InputH)
                return new BeamSplitter(InputH, OutputH, OutputV).Apply(state);
            return state;
        }
        public IReadOnlyList<Wire> Inputs { get { return new[] {InputV, InputH}; } }
    }
    public class Reflector : ICircuitElement {
        public Wire Input;
        public Wire Output;
        public Superposition<CircuitState> Apply(CircuitState state) {
            if (state.Wire != Input) return state;
            return state.With(wire: Output).Super() * Complex.ImaginaryOne;
        }
        public IReadOnlyList<Wire> Inputs { get { return Input.SingletonList(); } }
    }
    public struct CircuitState {
        public Wire Wire;
        public bool Detect1;
        public bool Detect2;
        public bool Detect3;
        public CircuitState With(bool? d1 = null, bool? d2 = null, bool? d3 = null) {
            return new CircuitState {
                Wire = Wire,
                Detect1 = d1 ?? this.Detect1,
                Detect2 = d2 ?? this.Detect2,
                Detect3 = d3 ?? this.Detect3
            };
        }
        public CircuitState With(Wire wire, bool? d1 = null, bool? d2 = null, bool? d3 = null) {
            return new CircuitState {
                Wire = wire,
                Detect1 = d1 ?? this.Detect1,
                Detect2 = d2 ?? this.Detect2,
                Detect3 = d3 ?? this.Detect3
            };
        }
        public override string ToString() {
            var s = new List<string>();
            if (Wire != null) s.Add(Wire.ToString());
            if (Detect1) s.Add("D1");
            if (Detect2) s.Add("D2");
            if (Detect3) s.Add("D3");
            return String.Join(" ", s);
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
    public struct CircuitWire {
        public Wire Id;
        public ICircuitElement EndPoint;
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

        var A = new BeamSplitter(input: SA, outputPass: AC, outputReflect: AB);
        var B = new Reflector { Input = AB, Output = BD };
        var C = new Reflector { Input = AC, Output = CD };
        var D = new CrossBeamSplitter(inputH: BD, inputV: CD, outputH: DF, outputV: DE);
        var E = new Detector { Input = DF, Output = null, Detected = new MockProperty<CircuitState, bool>(e => e.Detect1, (e, b) => e.With(d1: b)) };
        var F = new Detector { Input = DE, Output = null, Detected = new MockProperty<CircuitState, bool>(e => e.Detect2, (e, b) => e.With(d2: b)) };
        
        var state = new CircuitState { Wire = SA }.Super();
        
        var elements = new ICircuitElement[] {A, B, C, D, E, F};
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
