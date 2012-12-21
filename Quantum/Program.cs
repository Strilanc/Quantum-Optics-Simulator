using System;
using System.Collections.Generic;
using System.Linq;

static class Program {
    static void Main() {
        Circuit2();
        Circuit3();
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
