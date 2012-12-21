using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

static class Program {
    static void Main(string[] args) {
        Circuit2();
    }
    public class CircuitNode {
        public string type;
        public Tuple<CircuitNode, string>[] Children;
    }
    public class Splitter<T> {
        public T Input;
        public T Pass;
        public T Reflect;
    }
    public struct CircuitState {
        public string Wire;
        public bool Detect1;
        public bool Detect2;
        public bool Detect3;
        public CircuitState With(string wire = null, bool? d1 = null, bool? d2 = null, bool? d3 = null) {
            return new CircuitState {
                Wire = wire ?? Wire,
                Detect1 = d1 ?? this.Detect1,
                Detect2 = d2 ?? this.Detect2,
                Detect3 = d3 ?? this.Detect3
            };
        }
        public override string ToString() {
            return this.ReflectToString();
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

        var Ne = new CircuitNode { type = "end" };
        var Nf = new CircuitNode { type = "end" };
        var Nd0 = new CircuitNode { type = "splitter", Children = new[] { Tuple.Create(Ne, "DE"), Tuple.Create(Nf, "DF") } };
        var Nd1 = new CircuitNode { type = "splitter", Children = new[] { Tuple.Create(Nf, "DF"), Tuple.Create(Ne, "DE") } };
        var Nb = new CircuitNode { type = "mirror", Children = new[] { Tuple.Create(Nd1, "BD") } };
        var Nc = new CircuitNode { type = "mirror", Children = new[] { Tuple.Create(Nd0, "CD") } };
        var Na = new CircuitNode { type = "splitter", Children = new[] { Tuple.Create(Nc, "AC"), Tuple.Create(Nb, "AB") } };
        var Ns = new CircuitNode { type = "source", Children = new[] { Tuple.Create(Na, "SA") } };

        var numAmps = 1 << 7;
        var z = Complex.Zero.Repeat(numAmps);

        Func<string, string, Func<CircuitState, Superposition<CircuitState>>> reflect = (i, r) => w => {
            if (w.Wire != i) return w;
            return w.With(wire: r).Super() * Complex.ImaginaryOne;
        };
        Func<string, string, string, Func<CircuitState, Superposition<CircuitState>>> beamSplitter = (i, p, r) => w => {
            if (w.Wire != i) return w;
            return w.With(wire: p).Super() 
                 + w.With(wire: r).Super()*Complex.ImaginaryOne;
        };
        Func<string, Func<CircuitState, Superposition<CircuitState>>> detector = i => w => 
            w.With(d3: w.Detect3 || w.Wire == i);

        var q = new Queue<Tuple<CircuitNode, string>>();
        q.Enqueue(Tuple.Create(Na, Ns.Children.Single().Item2));
        var reg = Superposition<CircuitState>.FromPureValue(new CircuitState { Wire = "SA" });
        while (q.Count > 0) {
            var p = q.Dequeue();
            var n = p.Item1;
            var i = p.Item2;
            if (n.type == "end") continue;
            if (n.type == "splitter") {
                var reg2 = reg.ApplyTransform(beamSplitter(i, n.Children[0].Item2, n.Children[1].Item2));
                reg = reg2;
            }
            if (n.type == "mirror") {
                var reg2 = reg.ApplyTransform(reflect(i, n.Children.Single().Item2));
                reg = reg2;
            }
            var reg3 = reg.ApplyTransform(detector("BD"));
            reg = reg3;
            if (n.Children != null)
                foreach (var e in n.Children)
                    q.Enqueue(e);
        }
    }
}
