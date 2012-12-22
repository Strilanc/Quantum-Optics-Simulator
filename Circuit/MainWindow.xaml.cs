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
            HWire,
            VWire,
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
            _cells[0, 10].State = CellState.HWire;

            this.MouseDown += (sender, arg) => {
                var p = arg.GetPosition(canvas);
                var i = (p.X / canvas.ActualWidth * 20).FloorInt();
                var j = (p.Y / canvas.ActualHeight * 20).FloorInt();
                var c = _cells[i, j];
                c.State = (CellState)(((int)c.State + 1) % 9);
                c.Dirty = true;
                ShowCells();
                ComputeCircuit();
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
                        container.BorderBrush = new SolidColorBrush(Colors.Yellow);
                        container.BorderThickness = new Thickness(1);
                        container.Width = canvas.Width/20;
                        container.Height = canvas.Height/20;
                        switch (c.State) {
                        case CellState.Empty:
                            break;
                        case CellState.HWire:
                            container.Content = new Rectangle {
                                Fill = new SolidColorBrush(Colors.Black),
                                Height = 2
                            };
                            break;
                        case CellState.VWire:
                            container.Content = new Rectangle {
                                Fill = new SolidColorBrush(Colors.Black),
                                Height = 2,
                                RenderTransformOrigin = new Point(0.5, 0.5),
                                RenderTransform = new RotateTransform(90)
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
            var i = 0;
            var j = 10;
            if (_cells[i, j].State != CellState.HWire) {
                this.Title = "?";
                return;
            }
            var wp = CircuitState.WireProp;
            var wires = AllCells
                .Where(cell => cell.State == CellState.HWire || cell.State == CellState.VWire)
                .ToDictionary(e => e, e => new Wire(string.Format("{0},{1}", e.X, e.Y)));
            var dw = wires.WithDefaultResult();
            var dcount = 0;
            var elements = AllCells
                .SelectMany(e => {
                    var r = new List<ICircuitElement<CircuitState>>();
                    if (e.X <= 0 || e.X >= 19) return r;
                    if (e.Y <= 0 || e.Y >= 19) return r;
                    var left = dw[_cells[e.X - 1, e.Y]];
                    var up = dw[_cells[e.X, e.Y - 1]];
                    var down = dw[_cells[e.X, e.Y+1]];
                    var right = dw[_cells[e.X + 1, e.Y]];

                    if (e.State == CellState.BackSlashSplitter) {
                        if (left != null && right != null && down != null)
                            r.Add(wp.Split(left, right, down));
                        if (up != null && right != null && down != null)
                            r.Add(wp.Split(up, down, right));
                    } else if (e.State == CellState.BackSlashMirror) {
                        if (left != null && down != null)
                            r.Add(wp.Reflect(left, down));
                        if (up != null && right != null)
                            r.Add(wp.Reflect(up, right));
                    } else if (e.State == CellState.DetectorTerminate) {
                        r.Add(wp.Detect(left ?? up, CircuitState.DetectorProp(dcount++)));
                    } else if (e.State == CellState.DetectorPropagate) {
                        r.Add(wp.Detect(left ?? up, CircuitState.DetectorProp(dcount++), right ?? down));
                    } else if (e.State == CellState.HWire && _cells[e.X - 1, e.Y].State == CellState.HWire) {
                        r.Add(wp.Propagate(left, dw[e]));
                    } else if (e.State == CellState.VWire && _cells[e.X, e.Y - 1].State == CellState.VWire) {
                        r.Add(wp.Propagate(up, dw[e]));
                    }
                    return r;
                })
                .ToArray();

            var initialState = new CircuitState() {
                Wire = wires[_cells[0, 10]],
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
