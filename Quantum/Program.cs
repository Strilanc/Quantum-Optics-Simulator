using System;
using System.Collections.Generic;
using System.Linq;
using Strilanc.LinqToCollections;

static class Program {
    static void Main() {
        Circuit3();
    }

    public static void Circuit3() {
        // *>--A\-------\\B   
        //      |       |           
        //      |       |           
        //      |       |           
        //    C\\------D\--1--E\-------\\F
        //              |      |       |
        //              |      |       |
        //              |      |       |
        //              0    G\\---2--H\------3
        //                             |
        //                             |
        //                             |
        //                             4

        var _A = new Wire("_A");
        var AB = new Wire("AB");
        var AC = new Wire("AC");
        var BD = new Wire("BD");
        var CD = new Wire("CD");
        var D0 = new Wire("D0");
        var D1 = new Wire("D1");
        var _1E = new Wire("1E");
        var EF = new Wire("EF");
        var EG = new Wire("EG");
        var FH = new Wire("FH");
        var G2 = new Wire("G2");
        var _2H = new Wire("_2H");
        var H3 = new Wire("H3");
        var H4 = new Wire("H4");

        var w = CircuitState.WireProp;
        Func<int, MockProperty<CircuitState, bool>> d = CircuitState.DetectorProp;

        var elements = new ICircuitElement<CircuitState>[] {
            w.Reflect(AB, BD),
            w.Reflect(AC, CD),
            w.Reflect(EF, FH),
            w.Reflect(EG, G2),

            w.Split(input: _A, throughput: AB, reflectput: AC),
            w.Split(input: _1E, throughput: EF, reflectput: EG),

            w.CrossSplit(inputH: CD, inputV: BD, outputH: D1, outputV: D0),
            w.CrossSplit(inputH: _2H, inputV: FH, outputH: H3, outputV: H4),

            w.Detect(D0, d(0)),
            w.Detect(D1, d(1), _1E),
            w.Detect(G2, d(2), _2H),
            w.Detect(H3, d(3)),
            w.Detect(H4, d(4))
        };

        var initialState = new CircuitState {
            Wire = _A, 
            Detections = new EquatableList<bool>(ReadOnlyList.Repeat(false, 5))
        };
        var state = initialState.Super();
        while (true) {
            var activeWires = new HashSet<Wire>(state.Amplitudes.Keys.Select(e => e.Wire).Where(e => e != null));
            var activeElements = elements.Where(e => e.Inputs.Any(activeWires.Contains)).ToArray();
            if (activeElements.Length == 0) break;
            var newState = activeElements.Aggregate(state, (a, e) => a.Transform(e.Apply));
            state = newState;
        }
        var finalState = state;
    }
}
