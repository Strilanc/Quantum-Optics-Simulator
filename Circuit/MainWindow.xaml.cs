using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Strilanc.LinqToCollections;

namespace Circuit {
    public partial class MainWindow {
        private enum CellState {
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
            public FrameworkElement Control;
            public bool Dirty;
            public double Trace;
        }

        private Cell[,] _cells;
        public MainWindow() {
            InitializeComponent();
            
            _cells = new Cell[20, 20];
            foreach (var i in 20.Range()) {
                foreach (var j in 20.Range()) {
                    _cells[i, j] = new Cell {X = i, Y = j, State = CellState.Empty, Control = null, Dirty=true};
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
            foreach (var i in 20.Range()) {
                foreach (var j in 20.Range()) {
                    var c = _cells[i, j];
                    if (c.Dirty) {
                        if (c.Control != null) {
                            canvas.Children.Remove(c.Control);
                            c.Control = null;
                        }
                        var container = new UserControl();
                        container.Width = canvas.Width/20;
                        container.Height = canvas.Height/20;
                        switch (c.State) {
                        case CellState.Empty:
                            container.Content = new Rectangle {
                                Fill = new SolidColorBrush(Colors.Black),
                                Height = 2,
                                Width = 2
                            };
                            break;
                        case CellState.BackSlashSplitter:
                            container.Content = new Rectangle {
                                Fill = new SolidColorBrush(Colors.Red),
                                Height = 2,
                                RenderTransformOrigin = new Point(0.5, 0.5),
                                RenderTransform = new RotateTransform(45)
                            };
                            break;
                        case CellState.ForeSlashSplitter:
                            container.Content = new Rectangle {
                                Fill = new SolidColorBrush(Colors.Red),
                                Height = 2,
                                RenderTransformOrigin = new Point(0.5, 0.5),
                                RenderTransform = new RotateTransform(-45)
                            };
                            break;
                        case CellState.BackSlashMirror:
                            container.Content = new Rectangle {
                                Fill = new SolidColorBrush(Colors.Blue),
                                Height = 2,
                                RenderTransformOrigin = new Point(0.5, 0.5),
                                RenderTransform = new RotateTransform(45)
                            };
                            break;
                        case CellState.ForeSlashMirror:
                            container.Content = new Rectangle {
                                Fill = new SolidColorBrush(Colors.Blue),
                                Height = 2,
                                RenderTransformOrigin = new Point(0.5, 0.5),
                                RenderTransform = new RotateTransform(-45)
                            };
                            break;
                        case CellState.DetectorTerminate:
                            container.Content = new Ellipse {
                                Fill = new SolidColorBrush(Colors.Red)
                            };
                            break;
                        case CellState.DetectorPropagate:
                            container.Content = new Ellipse {
                                Fill = new SolidColorBrush(Colors.Blue)
                            };
                            break;
                        }
                        c.Control = container;
                        canvas.Children.Add(container);
                    }
                    if (c.Control != null) {
                        var w = canvas.ActualWidth/20;
                        var h = canvas.ActualHeight/20;
                        c.Control.SetValue(Canvas.LeftProperty, i*w);
                        c.Control.SetValue(Canvas.TopProperty, j*h);
                        c.Control.Width = w;
                        c.Control.Height = h;
                    }
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
        public void ComputeCircuit() {
            if (_cells[0, 10].State != CellState.Empty) {
                this.Title = "?";
                return;
            }
            var wp = CircuitState.WireProp;
            var wires = AllCells
                .SelectMany(e => new[] {
                    Tuple.Create("^", e.X, e.Y),
                    Tuple.Create("<", e.X, e.Y),
                    Tuple.Create(">", e.X, e.Y),
                    Tuple.Create("v", e.X, e.Y)
                })
                .ToDictionary(e => e, e => new Wire(e.ToString()));
            var dw = wires.WithDefaultResult();
            var propagateElements =
                AllCells
                .SelectMany(e => {
                    var r = new List<ICircuitElement<CircuitState>>();
                    if (e.X <= 0 || e.X >= 19) return r;
                    if (e.Y <= 0 || e.Y >= 19) return r;
                    var inLeft = dw[Tuple.Create(">", e.X - 1, e.Y)];
                    var inRight = dw[Tuple.Create("<", e.X + 1, e.Y)];
                    var inUp = dw[Tuple.Create("v", e.X, e.Y - 1)];
                    var inDown = dw[Tuple.Create("^", e.X, e.Y + 1)];

                    var outLeft = dw[Tuple.Create("<", e.X, e.Y)];
                    var outRight = dw[Tuple.Create(">", e.X, e.Y)];
                    var outUp = dw[Tuple.Create("^", e.X, e.Y)];
                    var outDown = dw[Tuple.Create("v", e.X, e.Y)];

                    var ins = new[] { inRight, inUp, inLeft, inDown };
                    var outs = new[] { outRight, outUp, outLeft, outDown };

                    foreach (var i in 4.Range()) {
                        var inp = ins[i];
                        var oup = outs[(i + 2) % 4];
                        r.Add(wp.Propagate(inp, oup));
                    }
                    return r;
                })
                .ToArray();

            var dcount = 0;
            var interestingElements = 
                AllCells
                .SelectMany(e => {
                    var r = new List<ICircuitElement<CircuitState>>();
                    if (e.X <= 0 || e.X >= 19) return r;
                    if (e.Y <= 0 || e.Y >= 19) return r;
                    var inLeft = dw[Tuple.Create(">", e.X - 1, e.Y)];
                    var inRight = dw[Tuple.Create("<", e.X + 1, e.Y)];
                    var inUp = dw[Tuple.Create("v", e.X, e.Y - 1)];
                    var inDown = dw[Tuple.Create("^", e.X, e.Y + 1)];

                    var outLeft = dw[Tuple.Create("<", e.X, e.Y)];
                    var outRight = dw[Tuple.Create(">", e.X, e.Y)];
                    var outUp = dw[Tuple.Create("^", e.X, e.Y)];
                    var outDown = dw[Tuple.Create("v", e.X, e.Y)];

                    var ins = new[] { inRight, inUp, inLeft, inDown };
                    var outs = new[] { outRight, outUp, outLeft, outDown };

                    var r1 = new[] { 1, 0, 3, 2 };
                    var r2 = new[] { 3, 2, 1, 0 };
                    if (e.State == CellState.BackSlashSplitter) {
                        foreach (var i in 4.Range()) {
                            r.Add(wp.Split(ins[i], outs[(i + 2)%4], outs[r1[i]]));
                        }
                    } else if (e.State == CellState.ForeSlashSplitter) {
                        foreach (var i in 4.Range()) {
                            r.Add(wp.Split(ins[i], outs[(i + 2) % 4], outs[r2[i]]));
                        }
                    } else if (e.State == CellState.BackSlashMirror) {
                        foreach (var i in 4.Range()) {
                            r.Add(wp.Reflect(ins[i], outs[r1[i]]));
                        }
                    } else if (e.State == CellState.ForeSlashMirror) {
                        foreach (var i in 4.Range()) {
                            r.Add(wp.Reflect(ins[i], outs[r2[i]]));
                        }
                    } else if (e.State == CellState.DetectorTerminate) {
                        var d = CircuitState.DetectorProp(dcount++);
                        foreach (var i in 4.Range()) {
                            var inp = ins[i];
                            r.Add(wp.Detect(inp, d));
                        }
                    } else if (e.State == CellState.DetectorPropagate) {
                        var d = CircuitState.DetectorProp(dcount++);
                        foreach (var i in 4.Range()) {
                            var inp = ins[i];
                            var oup = outs[(i + 2) % 4];
                            r.Add(wp.Detect(inp, d, oup));
                        }
                    }
                    return r;
                })
                .ToArray();
            var elements = interestingElements.Concat(propagateElements);

            var initialState = new CircuitState() {
                Wire = wires[Tuple.Create(">", 0, 10)],
                Detections = new EquatableList<bool>(ReadOnlyList.Repeat(false, dcount))
            };
            var state = initialState.Super();
            while (true) {
                var activeWires = new HashSet<Wire>(state.Amplitudes.Keys.Select(e => e.Wire).Where(e => e != null));
                var activeElements = elements.Where(e => e.Inputs.Any(activeWires.Contains)).ToArray();
                var newState = activeElements.Aggregate(state, (a, e) => a.Transform(e.Apply));
                if (Equals(state, newState)) break;
                state = newState;
            }
            this.Title = state.ToString();
        }
    }
    public static class Util {
        public static int FloorInt(this double d) {
            return (int)Math.Floor(d);
        }
    }
}
