using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.Angle;
using Strilanc.LinqToCollections;

static class Program {
    static void Main(string[] args) {
        var VerticalPolarizer = new Polarizer {Angle = Dir.FromVector(0, 1)};
        var HorizontalPolarizer = new Polarizer {Angle = Dir.FromVector(1, 0)};


        Circuit0();
        Circuit1();
        Circuit2();
        Circuit3();
        //Func<double, ComplexMatrix> beamSplitterForeSlashLRUD = theta => ComplexMatrix.FromCellData(
        ////i:L  R  U  D  //out
        //    0, s, t, 0, //L
        //    s, 0, 0, t, //R
        //    t, 0, 0, s, //U
        //    0, t, s, 0);//D

        var x1 = ComplexMatrix.FromCellData(
            1, 0,
            0, 1);

        var counters = 1000.Range()
            .Select(e => new LeftRightCounter {Ground = Complex.One, Triggered = Complex.Zero})
            .ToArray();
        for (var j = 0; j < 1000; j++) {
            var photon = new PolarizerOutput {
                Pass = new Polarization(Complex.One, Complex.Zero),
                Absorb = new Polarization(Complex.Zero, Complex.Zero)
            };
            photon = new Polarizer {Angle = Dir.FromVector(1, 1)}.Polarize(photon.Pass, photon.Absorb);
            photon = HorizontalPolarizer.Polarize(photon.Pass, photon.Absorb);

            for (var i = 0; i < counters.Length - 1; i++) {
                var r = counters[i].Count(photon.Absorb, photon.Pass);
                photon = new PolarizerOutput {
                    Pass = r.Photon,
                    Absorb = r.Absorb
                };
                counters[i] = r.Counter;
            }
        }
    }
    public static void Circuit0() {
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

        var SA = 0;
        var AC = 1;
        var AB = 2;
        var BD = 3;
        var CD = 4;
        var DF = 5;
        var DE = 6;
        var EG = 7;
        var FG = 8;
        var GI = 9;
        var GH = 10;
        var expectToStayZero = 11;

        var m = Complex.ImaginaryOne;
        var s = Math.Sqrt(0.5);
        var t = s * m;
        var reg = QuantumInteger.FromPureValue(SA, 12);
        var beamSplitterBackSlashLRUD = ComplexMatrix.FromCellData(
            //i:L  R  U  D  //out
            0, s, 0, t, //L
            s, 0, t, 0, //R
            0, t, 0, s, //U
            t, 0, s, 0);//D
        var mirrorBackSlashLRUD = ComplexMatrix.FromCellData(
            //i:L  R  U  D  //out
            0, 0, 0, m, //L
            0, 0, m, 0, //R
            0, m, 0, 0, //U
            m, 0, 0, 0);//D
        var reg3 = reg.ApplyOperationToSubset(beamSplitterBackSlashLRUD, new[] { SA, AC, expectToStayZero, AB });
        var reg4 = reg3.ApplyOperationToSubset(mirrorBackSlashLRUD, new[] { AC, expectToStayZero, expectToStayZero, CD });
        var reg5 = reg4.ApplyOperationToSubset(mirrorBackSlashLRUD, new[] { expectToStayZero, BD, AB, expectToStayZero });
        var reg6 = reg5.ApplyOperationToSubset(beamSplitterBackSlashLRUD, new[] { BD, DF, CD, DE });
        var reg7 = reg6.ApplyOperationToSubset(mirrorBackSlashLRUD, new[] { expectToStayZero, EG, DE, expectToStayZero });
        var reg8 = reg7.ApplyOperationToSubset(mirrorBackSlashLRUD, new[] { DF, expectToStayZero, expectToStayZero, FG });
        var reg9 = reg8.ApplyOperationToSubset(beamSplitterBackSlashLRUD, new[] { EG, GI, FG, GH });
    }
    public static void Circuit1() {
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

        var SA = 0;
        var AC = 1;
        var AB = 2;
        var BD = 3;
        var CD = 4;
        var DF = 5;
        var DE = 6;
        var expectToStayZero = 7;

        var m = Complex.ImaginaryOne;
        var s = Math.Sqrt(0.5);
        var t = s * m;
        var reg = QuantumInteger.FromPureValue(SA, 8);
        var beamSplitterBackSlashLRUD = ComplexMatrix.FromCellData(
            //i:L  R  U  D  //out
            0, s, 0, t, //L
            s, 0, t, 0, //R
            0, t, 0, s, //U
            t, 0, s, 0);//D
        var mirrorBackSlashLRUD = ComplexMatrix.FromCellData(
            //i:L  R  U  D  //out
            0, 0, 0, m, //L
            0, 0, m, 0, //R
            0, m, 0, 0, //U
            m, 0, 0, 0);//D
        var reg3 = reg.ApplyOperationToSubset(beamSplitterBackSlashLRUD, new[] { SA, AC, expectToStayZero, AB });
        var reg4 = reg3.ApplyOperationToSubset(mirrorBackSlashLRUD, new[] { AC, expectToStayZero, expectToStayZero, CD });
        var reg5 = reg4.ApplyOperationToSubset(mirrorBackSlashLRUD, new[] { expectToStayZero, BD, AB, expectToStayZero });
        var reg6 = reg5.ApplyOperationToSubset(beamSplitterBackSlashLRUD, new[] { BD, DF, CD, DE });
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

        Func<string, string, Func<Tuple<string, bool>, Superposition<Tuple<string, bool>>>> reflect = (i, r) => w => {
            if (!Equals(w.Item1, i)) return w;
            return Tuple.Create(r, w.Item2).Super() * Complex.ImaginaryOne;
        };
        Func<string, string, string, Func<Tuple<string, bool>, Superposition<Tuple<string, bool>>>> beamSplitter = (i, p, r) => w => {
            if (!Equals(w.Item1, i)) return w;
            return Tuple.Create(p, w.Item2).Super() 
                 + Tuple.Create(r, w.Item2).Super()*Complex.ImaginaryOne;
        };
        Func<string, Func<Tuple<string, bool>, Superposition<Tuple<string, bool>>>> detector = (i) => w => {
            return Tuple.Create(w.Item1, w.Item2 || w.Item1 == i);
        };

        var q = new Queue<Tuple<CircuitNode, string>>();
        q.Enqueue(Tuple.Create(Na, Ns.Children.Single().Item2));
        var reg = Superposition<Tuple<string, bool>>.FromPureValue(Tuple.Create("SA", false));
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
    //public static void Circuit2a() {
    //    // S--- 0 -----> A ---- 1 -----> C
    //    //               |               |
    //    //               2               4
    //    //               |               |
    //    //               v               v
    //    //               B ------ 3 ---> D --- 5 --> F
    //    //                               |
    //    //                               6
    //    //                               |
    //    //                               v
    //    //                               E

    //    var Ne = new CircuitNode { type = "detector" };
    //    var Nf = new CircuitNode { type = "detector" };
    //    var Nd0 = new CircuitNode { type = "splitter", Children = new[] { Tuple.Create(Ne, 0), Tuple.Create(Nf, 1) } };
    //    var Nd1 = new CircuitNode { type = "splitter", Children = new[] { Tuple.Create(Nf, 1), Tuple.Create(Ne, 0) } };
    //    var Nb = new CircuitNode { type = "mirror", Children = new[] { Tuple.Create(Nd1, 2) } };
    //    var Nc = new CircuitNode { type = "mirror", Children = new[] { Tuple.Create(Nd0, 3) } };
    //    var Na = new CircuitNode { type = "splitter", Children = new[] { Tuple.Create(Nc, 4), Tuple.Create(Nb, 5) } };
    //    var Ns = new CircuitNode { type = "source", Children = new[] { Tuple.Create(Na, 6) } };

    //    var numAmps = 1 << 7;
    //    var z = Complex.Zero.Repeat(numAmps);

    //    Func<int, int, Func<int, IReadOnlyList<Complex>>> reflect = (i, r) => w => {
    //        if ((w & i) == 0) return z.Impose(1, w);
    //        w &= ~i;
    //        return z.Impose(Complex.ImaginaryOne, w | r);
    //    };
    //    Func<int, int, int, Func<int, IReadOnlyList<Complex>>> beamSplitter = (i, p, r) => w => {
    //        if ((w & i) == 0) return z.Impose(1, w);
    //        w &= ~i;
    //        //  w 0 0 0 0 1 1 1 1
    //        //  p 0 0 1 1 0 0 1 1
    //        //  r 0 1 0 1 0 1 0 1
    //        //wpr ---------------
    //        //000 1 0 0 0 0 0 0 0 
    //        //001 0 1 0 0 0 0 0 0
    //        //010 0 0 1 0 0 0 0 0
    //        //011 0 0 0 1 0 0 0 0
    //        //100 0 j s 0 0 0 0 0
    //        //101 0 j 0 s 0 0 0 0
    //        //110 0 0 s j 0 0 0 0
    //        //111 0 0 0 ? 0 0 0 0
    //        return z
    //            .Impose(Math.Sqrt(0.5), w | p)
    //            .Impose(Math.Sqrt(0.5) * Complex.ImaginaryOne, w | r);
    //    };
    //    Func<int, int, Func<int, IReadOnlyList<Complex>>> detector = (i, d) => w => {
    //        if ((w & i) == 0) return z.Impose(1, w);
    //        return z.Impose(1, w | d);
    //    };

    //    var q = new Queue<Tuple<CircuitNode, int>>();
    //    q.Enqueue(Tuple.Create(Na, Ns.Children.Single().Item2));
    //    var reg = QuantumInteger.FromPureValue(1 << Ns.Children.Single().Item2, numAmps);
    //    while (q.Count > 0) {
    //        var p = q.Dequeue();
    //        var n = p.Item1;
    //        var i = p.Item2;
    //        if (n.type == "detector") continue;
    //        if (n.type == "splitter") {
    //            var reg2 = reg.ApplyTransform(beamSplitter(1 << i, 1 << n.Children[0].Item2, 1 << n.Children[1].Item2));
    //            reg = reg2;
    //        }
    //        if (n.type == "mirror") {
    //            var reg2 = reg.ApplyTransform(reflect(1 << i, 1 << n.Children.Single().Item2));
    //            reg = reg2;
    //        }
    //        if (n.Children != null)
    //            foreach (var e in n.Children)
    //                q.Enqueue(e);
    //    }
    //}
    public static void Circuit3() {
        // S--- 0 -----> A ---- 1 -----> C
        //               |               |
        //               2               4
        //               |               |
        //               v               v
        //               B ------ 3 ---> D --- 5 --> F
        //                    Detect     |
        //                               6
        //                               |
        //                               v
        //                               E

        var Detect = 1 << 0;
        var SA = 1 << 1;
        var AC = 1 << 2;
        var AB = 1 << 3;
        var BD = 1 << 4;
        var CD = 1 << 5;
        var DF = 1 << 6;
        var DE = 1 << 7;
        var numAmps = 1 << 8;

        var z = Complex.Zero.Repeat(numAmps);
        Func<int, int, Func<int, IReadOnlyList<Complex>>> reflect = (i, r) => w => {
            if ((w & i) == 0) return z.Impose(1, w);
            w &= ~i;
            return z.Impose(Complex.ImaginaryOne, w | r);
        };
        Func<int, int, int, Func<int, IReadOnlyList<Complex>>> beamSplitter = (i, p, r) => w => {
            if ((w & i) == 0) return z.Impose(1, w);
            w &= ~i;
            return z.Impose(Math.Sqrt(0.5), w | p).Impose(Math.Sqrt(0.5) * Complex.ImaginaryOne, w | r);
        };
        Func<int, int, Func<int, IReadOnlyList<Complex>>> detector = (i, d) => w => {
            if ((w & i) == 0) return z.Impose(1, w);
            return z.Impose(1, w | d);
        };

        var reg0 = QuantumInteger.FromPureValue(SA, numAmps);
        var reg1 = reg0.ApplyTransform(beamSplitter(SA, AC, AB));
        var reg2 = reg1.ApplyTransform(reflect(AB, BD));
        var reg3 = reg2.ApplyTransform(detector(BD, Detect));
        var reg4 = reg3.ApplyTransform(reflect(AC, CD));
        var reg5 = reg4.ApplyTransform(beamSplitter(CD, DE, DF));
        var reg6 = reg5.ApplyTransform(beamSplitter(BD, DF, DE));
    }
    public static double SquaredMagnitude(this Complex complex) {
        return complex.Magnitude * complex.Magnitude;
    }
    public static string ToPrettyString(this Complex c) {
        var r = c.Real;
        var i = c.Imaginary;
        if (i == 0) return String.Format("{0:0.###}", r);
        if (r == 0)
            return i == 1 ? "i"
                 : i == -1 ? "-i"
                 : String.Format("{0:0.###}i", i);
        return String.Format(
            "{0:0.###}{1}{2}",
            r == 0 ? (object)"" : r,
            i < 0 ? "-" : "+",
            i == 1 || i == -1 ? "i" : String.Format("{0:0.###}i", Math.Abs(i)));
    }
}
[DebuggerDisplay("{ToString()}")]
struct LeftRightCounter {
    public Complex Ground;
    public Complex Triggered;
    public struct CountResult {
        public LeftRightCounter Counter;
        public Polarization Photon;
        public Polarization Absorb;
    }
    public CountResult Count(Polarization photon, Polarization absence) {
        return new CountResult {
            Counter = new LeftRightCounter {
                Ground = Ground*(photon.V + absence.V + absence.H),
                Triggered = Triggered
                          + Ground*photon.H
            },
            Photon = new Polarization(Triggered*photon.H, photon.V),
            Absorb = absence
        };
    }
    public override string ToString() {
        return String.Format(
            "Ground: {0}, Triggered: {1}",
            Ground.ToPrettyString(),
            Triggered.ToPrettyString());
    }
}
struct Polarizer {
    public Dir Angle;
    public PolarizerOutput Polarize(Polarization polarization, Polarization absence) {
        var t = Turn.FromNaturalAngle(Angle.UnsignedNaturalAngle);
        var r = polarization.RotatedView(t);
        return new PolarizerOutput {
            Pass = new Polarization(r.H, 0).RotatedView(-t),
            Absorb = absence + new Polarization(0, r.V).RotatedView(-t)
        };
    }
}
[DebuggerDisplay("{ToString()}")]
struct PolarizerOutput {
    public Polarization Pass;
    public Polarization Absorb;
    public override string ToString() {
        return String.Format(
             "Pass: ({0}), Absorb: ({1})",
             Pass,
             Absorb);
    }
}

[DebuggerDisplay("{ToString()}")]
internal struct Polarization : IEquatable<Polarization> {
    public readonly Complex H;
    public readonly Complex V;
    public Polarization(Complex h, Complex v) {
        this.H = h;
        this.V = v;
    }
    public Polarization RotatedView(Turn turn) {
        var s = Math.Sin(turn.NaturalAngle);
        var c = Math.Cos(turn.NaturalAngle);
        return new Polarization(
            H * c + V * s,
            V * c - H * s);
    }
    public static Polarization operator *(Polarization p, Complex c) {
        return new Polarization(p.H*c, p.V*c);
    }
    public static Polarization operator +(Polarization p, Polarization c) {
        return new Polarization(p.H + c.H, p.V + c.V);
    }
    public bool Equals(Polarization other) {
        return Equals((object)other);
    }
    public double Measure(Dir angle) {
        return (H*angle.UnitX + V*angle.UnitY).SquaredMagnitude();
    }
    public override string ToString() {
        return String.Format(
            "H: {0}, V: {1}",
            H.ToPrettyString(),
            V.ToPrettyString());
    }
}

struct Configuration : IEquatable<Configuration> {
    private readonly IReadOnlyList<Complex> _state;
    public IReadOnlyList<Complex> State { get { return _state ?? ReadOnlyList.Empty<Complex>(); } }
    public Configuration(IReadOnlyList<Complex> state) {
        this._state = state;
    }

    public bool Equals(Configuration other) {
        return State.SequenceEqual(other.State);
    }
    public override bool Equals(object obj) {
        return obj is Configuration && Equals((Configuration)obj);
    }
    public override int GetHashCode() {
        return _state.Aggregate(0, (a, e) => {
            unchecked {
                return a*7 + e.GetHashCode();
            }
        });
    }
}