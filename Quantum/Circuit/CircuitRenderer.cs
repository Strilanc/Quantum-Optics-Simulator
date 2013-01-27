using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Circuit.Phys;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Strilanc.Angle;
using Strilanc.LinqToCollections;
using Strilanc.Value;
using Matrix = SharpDX.Matrix;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace Quantum {
    public class CircuitRenderer {
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
        public sealed class Cell {
            public int X;
            public int Y;
            public CellState State;
            public Complex Trace;
        }

        public readonly List<Tuple<Photon, Complex>> Waves = new List<Tuple<Photon, Complex>>(); 
        public readonly Cell[,] _cells;
        public int CellColumnCount = 20;
        public int CellRowCount = 20;

        private const float Tau = (float)Math.PI * 2;
        private TextFormat _textFormat;
        private Brush _sceneColorBrush;
        private PathGeometry1 _pathGeometry1;
        private Stopwatch _clock;
        private string Message = "";

        public CircuitRenderer() {
            EnableClear = true;
            Show = true;

            _cells = new Cell[CellColumnCount, CellRowCount];
            foreach (var i in CellColumnCount.Range()) {
                foreach (var j in CellRowCount.Range()) {
                    _cells[i, j] = new Cell { X = i, Y = j, State = CellState.Empty };
                }
            }
            ComputeCircuit();
        }

        public bool EnableClear { get; set; }

        public bool Show { get; set; }

        public virtual void Initialize(DeviceContext contextD2D) {
            this._sceneColorBrush = new SolidColorBrush(contextD2D, Color.Red);

            this._clock = Stopwatch.StartNew();
        }

        public virtual void Render(RenderParams renderParams) {
            var t = (float)_clock.Elapsed.TotalSeconds;
            if (!Show) return;

            var context2D = renderParams.DevicesAndContexts.ContextDirect2D;
            context2D.BeginDraw();

            if (EnableClear) context2D.Clear(Color.Black);

            var r = renderParams.SizedDeviceResources.RenderTargetBounds;
            _textFormat = _textFormat ?? new TextFormat(renderParams.DirectXResources.FactoryDirectWrite, "Calibri", 16*(float)r.Width/1920) {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            context2D.TextAntialiasMode = TextAntialiasMode.Grayscale;
            context2D.DrawText(Message, _textFormat, new RectangleF((float)r.Left, (float)r.Top, (float)r.Right, (float)r.Bottom), _sceneColorBrush);

            using (PathGeometry1 sineWave = new PathGeometry1(renderParams.DirectXResources.FactoryDirect2D),
                                 cosineWave = new PathGeometry1(renderParams.DirectXResources.FactoryDirect2D)) {
                const int Precision = 100;

                // sine wave shape
                var pathSink = sineWave.Open();
                pathSink.BeginFigure(new DrawingPointF(0, Precision / 2.0f), FigureBegin.Filled);
                foreach (var i in Precision.Range()) {
                    var x = i + 1.0f;
                    var y = Precision * (float)(Math.Sin(x / Precision * Tau) + 1) / 2;
                    pathSink.AddLine(new DrawingPointF(x, y));
                }
                pathSink.EndFigure(FigureEnd.Open);
                pathSink.Close();

                // cosine wave shape
                pathSink = cosineWave.Open();
                pathSink.BeginFigure(new DrawingPointF(0, Precision / 2.0f), FigureBegin.Filled);
                foreach (var i in Precision.Range()) {
                    var x = i + 1.0f;
                    var y = Precision * (float)(Math.Cos(x / Precision * Tau) + 1) / 2;
                    pathSink.AddLine(new DrawingPointF(x, y));
                }
                pathSink.AddLine(new DrawingPointF(Precision, Precision / 2.0f));
                pathSink.EndFigure(FigureEnd.Open);
                pathSink.Close();

                var w = (float)renderParams.SizedDeviceResources.RenderTargetBounds.Width / CellColumnCount;
                var h = (float)renderParams.SizedDeviceResources.RenderTargetBounds.Height / CellRowCount;
                using (SolidColorBrush brush = new SolidColorBrush(renderParams.DevicesAndContexts.ContextDirect2D, Color.White),
                                       brush2 = new SolidColorBrush(renderParams.DevicesAndContexts.ContextDirect2D, new Color(0, 255, 0, 64)),
                                       brush3 = new SolidColorBrush(renderParams.DevicesAndContexts.ContextDirect2D, new Color(255, 0, 0, 64))) {

                    foreach (var p in Waves) {
                        var amps = (float)Math.Min(1, Math.Max(0, p.Item2.Magnitude));
                        var rot = (float)Dir.FromVector(-p.Item1.Vel.X, -p.Item1.Vel.Y).SignedNaturalAngle;
                        var x = w*(p.Item1.Pos.X + 0.5f);
                        var y = h*(p.Item1.Pos.Y + 0.5f);

                        context2D.Transform =
                              Matrix.Scaling(1.0f / Precision)
                            * Matrix.Translation(0, -0.5f, 0)
                            * Matrix.Scaling(1, amps * amps * (float)p.Item1.Pol.Dir.UnitX, 1)
                            * Matrix.RotationZ(rot)
                            * Matrix.Scaling(w, h, 1)
                            * Matrix.Translation(x, y, 0);
                        context2D.FillGeometry(sineWave, brush2);
                        context2D.DrawGeometry(sineWave, brush);

                        context2D.Transform =
                              Matrix.Scaling(1.0f / Precision)
                            * Matrix.Translation(0, -0.5f, 0)
                            * Matrix.Scaling(1, amps * amps * (float)p.Item1.Pol.Dir.UnitY, 1)
                            * Matrix.RotationZ(rot)
                            * Matrix.Scaling(w, h, 1)
                            * Matrix.Translation(x, y, 0);
                        context2D.FillGeometry(cosineWave, brush3);
                        context2D.DrawGeometry(cosineWave, brush);
                    }
                    
                    context2D.Transform = Matrix.Identity;
                    foreach (var c in AllCells) {
                        var cr = new RectangleF(w * c.X, h * c.Y, w * (c.X + 1), h * (c.Y + 1));
                        context2D.DrawText(c.State == CellState.Empty ? "." : c.State.ToString(), _textFormat, cr, brush);
                    }
                }
            }

            context2D.EndDraw();
        }
        private IEnumerable<Cell> AllCells {
            get {
                return from i in CellColumnCount.Range()
                       from j in CellRowCount.Range()
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

            var initialState = new CircuitState(TimeSpan.Zero, new Photon(new Position(0, 10), Velocity.PlusX, default(Polarization)));
            foreach (var e in AllCells)
                e.Trace = 0;
            Waves.Clear();
            try {
                var state = initialState.Super();
                var n = 0;
                while (true) {
                    n += 1;
                    if (n > 10000) throw new InvalidOperationException("Overcompute");
                    foreach (var e in state.Amplitudes) {
                        if (!e.Key.Photon.HasValue) continue;
                        var p = e.Key.Photon.ForceGetValue();
                        if (p.Pos.X >= 0 && p.Pos.X < CellColumnCount && p.Pos.Y >= 0 && p.Pos.Y < CellRowCount)
                            _cells[p.Pos.X, p.Pos.Y].Trace = e.Value;
                        Waves.Add(Tuple.Create(p, e.Value));
                    }

                    var newState = state.Transform(e =>
                        e.Photon
                        .Where(p => elements.ContainsKey(p.Pos))
                        .Select(p => elements[p.Pos](e))
                        .Else(e.Super()));
                    var newState2 = newState.Transform(e =>
                        e.Photon
                        .Where(p => p.Pos.X >= 0 && p.Pos.X < CellColumnCount && p.Pos.Y >= 0 && p.Pos.Y < CellRowCount)
                        .Select(p => e.WithTick().Super())
                        .Else(e.Super()));
                    if (Equals(state, newState2)) break;
                    state = newState2;
                }
                Message = state.ToString();
            } catch (Exception ex) {
                Message = ex.ToString();
            }
        }
    }
}
