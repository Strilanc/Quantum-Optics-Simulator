using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Circuit.Phys;
using Strilanc.Angle;
using Strilanc.LinqToCollections;
using Strilanc.Value;

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
            HorizontalPolarizer,
            VerticalPolarizer,
            BackSlashPolarizer,
            ForeSlashPolarizer,
        }
        private sealed class Cell {
            public int X;
            public int Y;
            public CellState State;
            public CircuitElementControl Control;
            public bool Dirty;
        }

        private readonly Dictionary<string, Wave> _waveControls = new Dictionary<string, Wave>();
        private readonly Cell[,] _cells;
        
        public MainWindow() {
            InitializeComponent();
            _cells = new Cell[20, 20];
            foreach (var i in 20.Range()) {
                foreach (var j in 20.Range()) {
                    _cells[i, j] = new Cell {X = i, Y = j, State = CellState.Empty, Control = null, Dirty=true};

                    var w = new Wave { RenderTransformOrigin=new Point(0.5, 0.5), RenderTransform=new RotateTransform(180)};
                    _waveControls[Tuple.Create(Velocity.MinusX, i, j).ToString()] = w;
                    canvas.Children.Add(w);
                    
                    w = new Wave { RenderTransformOrigin=new Point(0.5, 0.5)};
                    _waveControls[Tuple.Create(Velocity.PlusX, i, j).ToString()] = w;
                    canvas.Children.Add(w);
                    
                    w = new Wave { RenderTransformOrigin=new Point(0.5, 0.5), RenderTransform=new RotateTransform(270)};
                    _waveControls[Tuple.Create(Velocity.MinusY, i, j).ToString()] = w;
                    canvas.Children.Add(w);
                    
                    w = new Wave { RenderTransformOrigin=new Point(0.5, 0.5), RenderTransform=new RotateTransform(90)};
                    _waveControls[Tuple.Create(Velocity.PlusY, i, j).ToString()] = w;
                    canvas.Children.Add(w);
                }
            }

            
            this.MouseDown += (sender, arg) => {
                var p = arg.GetPosition(canvas);
                var i = (p.X / canvas.ActualWidth * 20).FloorInt();
                var j = (p.Y / canvas.ActualHeight * 20).FloorInt();
                var c = _cells[i, j];
                c.State = (CellState)(((int)c.State + (arg.ChangedButton == MouseButton.Left ? 1 : -1) + 11) % 11);
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

                    var fr = _waveControls[Tuple.Create(Velocity.MinusX, i, j).ToString()];
                    var fl = _waveControls[Tuple.Create(Velocity.PlusX, i, j).ToString()];
                    var fd = _waveControls[Tuple.Create(Velocity.MinusY, i, j).ToString()];
                    var fu = _waveControls[Tuple.Create(Velocity.PlusY, i, j).ToString()];
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
        public void ComputeCircuit() {
            var elements = 
                AllCells
                .Where(e => e.State != CellState.Empty)
                .ToDictionary(e => new Position(e.X, e.Y), e => {
                    Func<Photon, Superposition<Photon>> x;
                    if (e.State == CellState.BackSlashSplitter) {
                        x = p => p.HalfSwapVelocity();
                    } else if (e.State == CellState.ForeSlashSplitter) {
                        x = p => p.HalfNegateSwapVelocity();
                    } else if (e.State == CellState.BackSlashMirror) {
                        x = p => p.SwapVelocity();
                    } else if (e.State == CellState.ForeSlashMirror) {
                        x = p => p.SwapNegateVelocity();
                    } else {
                        x = null;
                    }

                    Func<Photon, Superposition<May<Photon>>> x3;
                    if (x != null) {
                        x3 = p => x(p).Transform<May<Photon>>(v => v.Maybe());
                    } else if (e.State == CellState.ForeSlashPolarizer) {
                        x3 = p => p.Polarize(new Polarization(Dir.FromVector(1, 1)));
                    } else if (e.State == CellState.BackSlashPolarizer) {
                        x3 = p => p.Polarize(new Polarization(Dir.FromVector(-1, 1)));
                    } else if (e.State == CellState.HorizontalPolarizer) {
                        x3 = p => p.Polarize(new Polarization(Dir.FromVector(1, 0)));
                    } else if (e.State == CellState.VerticalPolarizer) {
                        x3 = p => p.Polarize(new Polarization(Dir.FromVector(0, 1)));
                    } else {
                        x3 = null;
                    }

                    Func<CircuitState, Superposition<CircuitState>> x2;
                    if (x3 != null) {
                        x2 = c => c.Photon.Match(p => x3(p).Transform<CircuitState>(p2 => c.WithPhoton(p2)), () => c.Super());
                    } else if (e.State == CellState.DetectorTerminate) {
                        x2 = c => c.WithPhoton(May.NoValue);
                    } else if (e.State == CellState.DetectorPropagate) {
                        x2 = c => c.WithDetection(new Position(e.X, e.Y));
                    } else {
                        throw new NotImplementedException();
                    }

                    return x2;
                });

            var initialState = new CircuitState(TimeSpan.Zero, new Photon(new Position(0, 0), Velocity.PlusX, default(Polarization)));
            foreach (var e in _waveControls.Values)
                e.Amplitude = 0;
            try {
                var state = initialState.Super();
                var n = 0;
                while (true) {
                    n += 1;
                    if (n > 10000) throw new InvalidOperationException("Overcompute");
                    foreach (var e in state.Amplitudes) {
                        if (!e.Key.Photon.HasValue) continue;
                        var p = e.Key.Photon.ForceGetValue();
                        var s = Tuple.Create(p.Vel, p.Pos.X, p.Pos.Y).ToString();
                        if (_waveControls.ContainsKey(s))
                            _waveControls[s].Amplitude = e.Value;
                    }

                    var newState = state.Transform(e =>
                        e.Photon
                        .Where(p => elements.ContainsKey(p.Pos))
                        .Select(p => elements[p.Pos](e))
                        .Else(e.Super()));
                    var newState2 = newState.Transform(e =>
                        e.Photon
                        .Where(p => p.Pos.X >= 0 && p.Pos.X < 20 && p.Pos.Y >= 0 && p.Pos.Y < 20)
                        .Select(p => e.WithTick().Super())
                        .Else(e.Super()));
                    if (Equals(state, newState2)) break;
                    state = newState2;
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
