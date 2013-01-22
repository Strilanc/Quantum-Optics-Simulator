using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Strilanc.LinqToCollections;

namespace Circuit {
    public partial class MainWindow {
        public enum CellState {
            Empty,
            BackSlashSplitter,
            ForeSlashSplitter,
            BackSlashMirror,
            ForeSlashMirror,
            DetectorTerminate,
            DetectorPropagate,
        }
        private class Cell {
            public int X;
            public int Y;
            public CellState State;
            public CircuitElementControl Control;
            public bool Dirty;
            public double Trace;
        }

        private Dictionary<string, Wave> _waveControls = new Dictionary<string, Wave>();
        private Cell[,] _cells;
        
        public MainWindow() {
            InitializeComponent();
            _cells = new Cell[20, 20];
            foreach (var i in 20.Range()) {
                foreach (var j in 20.Range()) {
                    _cells[i, j] = new Cell {X = i, Y = j, State = CellState.Empty, Control = null, Dirty=true};
                    Wave w;

                    w = new Wave() { RenderTransformOrigin=new Point(0.5, 0.5), RenderTransform=new RotateTransform(180)};
                    _waveControls[Tuple.Create("<", i, j).ToString()] = w;
                    canvas.Children.Add(w);
                    
                    w = new Wave() { RenderTransformOrigin=new Point(0.5, 0.5)};
                    _waveControls[Tuple.Create(">", i, j).ToString()] = w;
                    canvas.Children.Add(w);
                    
                    w = new Wave() { RenderTransformOrigin=new Point(0.5, 0.5), RenderTransform=new RotateTransform(270)};
                    _waveControls[Tuple.Create("^", i, j).ToString()] = w;
                    canvas.Children.Add(w);
                    
                    w = new Wave() { RenderTransformOrigin=new Point(0.5, 0.5), RenderTransform=new RotateTransform(90)};
                    _waveControls[Tuple.Create("v", i, j).ToString()] = w;
                    canvas.Children.Add(w);
                }
            }

            
            this.MouseDown += (sender, arg) => {
                var p = arg.GetPosition(canvas);
                var i = (p.X / canvas.ActualWidth * 20).FloorInt();
                var j = (p.Y / canvas.ActualHeight * 20).FloorInt();
                var c = _cells[i, j];
                c.State = (CellState)(((int)c.State + 1) % 7);
                c.Dirty = true;
                ComputeCircuit();
                ShowCells();
            };
            this.Loaded += (sender2, arg2) => {
                ShowCells();
                this.SizeChanged += (sender, arg) => ShowCells();
            };
        }
        public void ShowCells() {
            var w = canvas.ActualWidth / 20;
            var h = canvas.ActualHeight / 20;
            foreach (var i in 20.Range()) {
                foreach (var j in 20.Range()) {
                    var c = _cells[i, j];
                    if (c.Dirty) {
                        if (c.Control == null) {
                            c.Control = new CircuitElementControl();
                            canvas.Children.Add(c.Control);
                        }
                        c.Dirty = false;
                        c.Control.State = c.State;
                    }
                    if (c.Control != null) {
                        c.Control.SetValue(Canvas.LeftProperty, i*w);
                        c.Control.SetValue(Canvas.TopProperty, j*h);
                        c.Control.Width = w;
                        c.Control.Height = h;
                    }

                    var fr = _waveControls[Tuple.Create("<", i, j).ToString()];
                    var fl = _waveControls[Tuple.Create(">", i, j).ToString()];
                    var fd = _waveControls[Tuple.Create("^", i, j).ToString()];
                    var fu = _waveControls[Tuple.Create("v", i, j).ToString()];
                    fr.Width = fl.Width = fd.Width = fu.Width = w;
                    fr.Height = fl.Height = fd.Height = fu.Height = h;
                    fr.SetValue(Canvas.LeftProperty, i * w);
                    fl.SetValue(Canvas.LeftProperty, i * w);
                    fd.SetValue(Canvas.LeftProperty, i * w);
                    fu.SetValue(Canvas.LeftProperty, i * w);
                    fr.SetValue(Canvas.TopProperty, j * h);
                    fl.SetValue(Canvas.TopProperty, j * h);
                    fd.SetValue(Canvas.TopProperty, j * h);
                    fu.SetValue(Canvas.TopProperty, j * h);
                }
            }
        }
        private IEnumerable<Cell> AllCells {
            get {
                return from i in 20.Range()
                       from j in 20.Range()
                       select _cells[i, j];
            }
        }
        private struct CellDiry<T> : ICircuitElement<T> {
            public ICircuitElement<T> Element;
            public Cell Cell;
            public Superposition<T> Apply(T state) {
                Cell.Trace = 1;
                return Element.Apply(state);
            }
            public IReadOnlyList<Wire> Inputs { get { return Element.Inputs; } }
        }
        public void ComputeCircuit() {
            var wp = CircuitState.WireProp;
            var wires = 
                Enumerable.Range(-1, 22)
                .SelectMany(x => 
                    Enumerable.Range(-1, 22)
                    .SelectMany(y => new[] {
                        Tuple.Create("^", x, y),
                        Tuple.Create("<", x, y),
                        Tuple.Create(">", x, y),
                        Tuple.Create("v", x, y)}))
                .ToDictionary(e => e, e => new Wire(e.ToString()));
            var dw = wires.WithDefaultResult();
            var dcount = 0;
            var elements = 
                AllCells
                .Select<Cell, IEnumerable<ICircuitElement<CircuitState>>>(e => {
                    var inLeft = dw[Tuple.Create(">", e.X, e.Y)];
                    var inRight = dw[Tuple.Create("<", e.X, e.Y)];
                    var inUp = dw[Tuple.Create("v", e.X, e.Y)];
                    var inDown = dw[Tuple.Create("^", e.X, e.Y)];

                    var outLeft = dw[Tuple.Create("<", e.X - 1, e.Y)];
                    var outRight = dw[Tuple.Create(">", e.X + 1, e.Y)];
                    var outUp = dw[Tuple.Create("^", e.X, e.Y - 1)];
                    var outDown = dw[Tuple.Create("v", e.X, e.Y + 1)];

                    var ins = new[] { inRight, inUp, inLeft, inDown };
                    var outs = new[] { outRight, outUp, outLeft, outDown };

                    var r1 = new[] { 1, 0, 3, 2 };
                    var r2 = new[] { 3, 2, 1, 0 };
                    if (e.State == CellState.Empty)
                        return 4.Range().Select(i => wp.Propagate(ins[i], outs[(i + 2) % 4]));
                    if (e.State == CellState.BackSlashSplitter)
                        return 4.Range().Select(i => wp.Split(ins[i], outs[(i + 2) % 4], outs[r1[i]]));
                    if (e.State == CellState.ForeSlashSplitter)
                        return 4.Range().Select(i => wp.Split(ins[i], outs[(i + 2) % 4], outs[r2[i]]));
                    if (e.State == CellState.BackSlashMirror)
                        return 4.Range().Select(i => wp.Reflect(ins[i], outs[r1[i]]));
                    if (e.State == CellState.ForeSlashMirror)
                        return 4.Range().Select(i => wp.Reflect(ins[i], outs[r2[i]]));
                    if (e.State == CellState.DetectorTerminate) {
                        var d = CircuitState.DetectorProp(dcount++);
                        return 4.Range().Select(i => wp.Detect(ins[i], d));
                    }
                    if (e.State == CellState.DetectorPropagate) {
                        var d = CircuitState.DetectorProp(dcount++);
                        return 4.Range().Select(i => wp.Detect(ins[i], d, outs[(i + 2) % 4]));
                    }

                    throw new NotImplementedException();
                })
                .Zip(AllCells, (c, ce) => c.Select(e => new CellDiry<CircuitState> { Cell = ce, Element = e}))
                .SelectMany(e => e)
                .ToArray();

            var initialState = new CircuitState() {
                Wire = wires[Tuple.Create(">", 0, 10)],
                Detections = new EquatableList<bool>(ReadOnlyList.Repeat(false, dcount))
            };
            foreach (var e in _waveControls.Values)
                e.Amplitude = 0;
            foreach (var e in AllCells)
                e.Trace = 0;
            try {
                var state = initialState.Super();
                var n = 0;
                while (true) {
                    n += 1;
                    if (n > 10000) throw new InvalidOperationException("Overcompute");
                    var activeWires = new HashSet<Wire>(state.Amplitudes.Keys.Select(e => e.Wire).Where(e => e != null));
                    foreach (var e in activeWires.Where(f => _waveControls.ContainsKey(f.Name))) {
                        var r = state.Amplitudes.Where(f => Equals(f.Key.Wire, e)).Select(f => f.Value.SquaredMagnitude()).Sum();
                        _waveControls[e.Name].Amplitude = r;
                    }
                    var activeElements = elements.Where(e => e.Inputs.Any(activeWires.Contains)).ToArray();

                    var newState = activeElements.Aggregate(state, (a, e) => a.Transform(e.Apply));
                    if (Equals(state, newState)) break;
                    state = newState;
                }
                this.Title = state.ToString();
            } catch (Exception ex) {
                this.Title = ex.ToString();
            }
        }
    }
    public static class Util {
        public static int FloorInt(this double d) {
            return (int)Math.Floor(d);
        }
    }
}
