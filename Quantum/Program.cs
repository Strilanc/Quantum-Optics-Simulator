using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Strilanc.Angle;
using Strilanc.LinqToCollections;

static class Program {
    static void Main(string[] args) {
        Circuit0();
        Circuit1();
        Circuit2();
        //Func<double, ComplexMatrix> beamSplitterForeSlashLRUD = theta => ComplexMatrix.FromCellData(
        ////i:L  R  U  D  //out
        //    0, s, t, 0, //L
        //    s, 0, 0, t, //R
        //    t, 0, 0, s, //U
        //    0, t, s, 0);//D
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
}
