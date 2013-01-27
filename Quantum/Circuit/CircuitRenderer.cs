using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Circuit.Phys;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Strilanc.Angle;
using Strilanc.LinqToCollections;
using Strilanc.Value;
using TwistedOak.Util;
using Matrix = SharpDX.Matrix;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace Quantum {
    public sealed class CircuitRenderer {
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
        private string _message = "";

        public CircuitRenderer() {
            _cells = new Cell[CellColumnCount, CellRowCount];
            foreach (var i in CellColumnCount.Range()) {
                foreach (var j in CellRowCount.Range()) {
                    _cells[i, j] = new Cell { X = i, Y = j, State = CellState.Empty };
                }
            }
            ComputeCircuit();
        }

        public void Initialize(DeviceContext contextD2D) {
        }

        public void Render(RenderParams renderParams) {
            var g = renderParams.DevicesAndContexts.ContextDirect2D;
            g.BeginDraw();

            g.Clear(Color.Black);

            var fac = renderParams.DirectXResources.FactoryDirect2D;
            var sineWavesTrace = new PathGeometry1[4];
            var sineWavesFill = new PathGeometry1[4];
            const int Precision = 100;
            foreach (var o in 4.Range()) {
                sineWavesTrace[o] = new PathGeometry1(fac);
                sineWavesFill[o] = new PathGeometry1(fac);
                var pathSinkTrace = sineWavesTrace[o].Open();
                var pathSinkFill = sineWavesFill[o].Open();
                pathSinkTrace.BeginFigure(new DrawingPointF(0, Precision*(float)(Math.Sin(0.25*o*Tau) + 1)/2), FigureBegin.Hollow);
                pathSinkFill.BeginFigure(new DrawingPointF(0, Precision / 2.0f), FigureBegin.Filled);
                foreach (var i in (Precision+1).Range()) {
                    var x = (float)i;
                    var y = Precision*(float)(Math.Sin((x/Precision + 0.25*o)*Tau) + 1)/2;
                    pathSinkTrace.AddLine(new DrawingPointF(x, y));
                    pathSinkFill.AddLine(new DrawingPointF(x, y));
                }
                pathSinkFill.AddLine(new DrawingPointF(Precision, Precision / 2.0f));
                pathSinkTrace.EndFigure(FigureEnd.Open);
                pathSinkFill.EndFigure(FigureEnd.Open);
                pathSinkTrace.Close();
                pathSinkFill.Close();
            }
            try {
                var r = renderParams.SizedDeviceResources.RenderTargetBounds;
                var w = (float)r.Width/CellColumnCount;
                var h = (float)r.Height/CellRowCount;
                using (SolidColorBrush white = new SolidColorBrush(g, Color.White),
                                       quasiGreen = new SolidColorBrush(g, new Color(0, 255, 0, 64)),
                                       quasiRed = new SolidColorBrush(g, new Color(255, 0, 0, 64)),
                                       quasiWhite = new SolidColorBrush(g, new Color(255, 255, 255, 64))) {

                    _textFormat = _textFormat ?? new TextFormat(renderParams.DirectXResources.FactoryDirectWrite, "Calibri", 16) {
                        TextAlignment = TextAlignment.Center,
                        ParagraphAlignment = ParagraphAlignment.Center,
                        WordWrapping = WordWrapping.Wrap
                    };
                    g.TextAntialiasMode = TextAntialiasMode.Grayscale;
                    g.DrawText(_message, _textFormat, new RectangleF((float)r.Left, (float)r.Top, (float)r.Right, (float)r.Top + 60), white);

                    foreach (var p in Waves) {
                        var amps = (float)Math.Min(1, Math.Max(0, p.Item2.Magnitude));
                        var rot = (float)Dir.FromVector(-p.Item1.Vel.X, -p.Item1.Vel.Y).SignedNaturalAngle;
                        var x = w*(p.Item1.Pos.X + 0.5f);
                        var y = h*(p.Item1.Pos.Y + 0.5f);

                        var phase = (int)Math.Round(p.Item2.Phase/Tau*4) & 3;
                        
                        g.Transform =
                            Matrix.Scaling(1.0f/Precision)
                            *Matrix.Translation(0, -0.5f, 0)
                            *Matrix.Scaling(1, amps*(float)p.Item1.Pol.Dir.UnitX, 1)
                            *Matrix.RotationZ(rot)
                            *Matrix.Scaling(w, h, 1)
                            *Matrix.Translation(x, y, 0);
                        g.FillGeometry(sineWavesFill[phase], quasiGreen);
                        g.DrawGeometry(sineWavesTrace[phase], white);

                        g.Transform =
                            Matrix.Scaling(1.0f/Precision)
                            *Matrix.Translation(0, -0.5f, 0)
                            *Matrix.Scaling(1, amps*(float)p.Item1.Pol.Dir.UnitY, 1)
                            *Matrix.RotationZ(rot)
                            *Matrix.Scaling(w, h, 1)
                            *Matrix.Translation(x, y, 0);
                        var p1 = (phase + 1) & 3;
                        g.FillGeometry(sineWavesFill[p1], quasiRed);
                        g.DrawGeometry(sineWavesTrace[p1], white);
                    }

                    g.Transform = Matrix.Identity;
                    foreach (var c in AllCells) {
                        var center = new DrawingPointF(w*(c.X + 0.5f), h*(c.Y + 0.5f));
                        var s = Math.Min(w, h);
                        var vl = center.X - s/3;
                        var vt = center.Y - s/3;
                        var vr = center.X + s/3;
                        var vb = center.Y + s/3;
                        var vc = new RectangleF(vl, vt, vr, vb);
                        switch (c.State) {
                        case CellState.Empty:
                            g.FillEllipse(new Ellipse(center, 1, 1), quasiWhite);
                            break;
                        case CellState.BackSlashMirror:
                            g.DrawLine(new DrawingPointF(vl - 1, vt + 1), new DrawingPointF(vr - 1, vb + 1), white);
                            g.DrawLine(new DrawingPointF(vl + 1, vt - 1), new DrawingPointF(vr + 1, vb - 1), white);
                            break;
                        case CellState.ForeSlashMirror:
                            g.DrawLine(new DrawingPointF(vr - 1, vt - 1), new DrawingPointF(vl - 1, vb - 1), white);
                            g.DrawLine(new DrawingPointF(vr + 1, vt + 1), new DrawingPointF(vl + 1, vb + 1), white);
                            break;
                        case CellState.BackSlashSplitter:
                            g.FillRectangle(vc, quasiWhite);
                            g.DrawRectangle(vc, white);
                            g.DrawLine(new DrawingPointF(vl, vt), new DrawingPointF(vr, vb), white);
                            break;
                        case CellState.ForeSlashSplitter:
                            g.FillRectangle(vc, quasiWhite);
                            g.DrawRectangle(vc, white);
                            g.DrawLine(new DrawingPointF(vr, vt), new DrawingPointF(vl, vb), white);
                            break;
                        case CellState.BackSlashPolarizer:
                            g.FillRectangle(vc, quasiRed);
                            g.FillRectangle(vc, quasiGreen);
                            g.DrawLine(new DrawingPointF(vl, vt), new DrawingPointF(vr, vb), white);
                            break;
                        case CellState.ForeSlashPolarizer:
                            g.FillRectangle(vc, quasiGreen);
                            g.FillRectangle(vc, quasiRed);
                            g.DrawLine(new DrawingPointF(vr, vt), new DrawingPointF(vl, vb), white);
                            break;
                        case CellState.HorizontalPolarizer:
                            g.FillRectangle(vc, quasiGreen);
                            g.DrawLine(new DrawingPointF(vl, center.Y), new DrawingPointF(vr, center.Y), white);
                            break;
                        case CellState.VerticalPolarizer:
                            g.FillRectangle(vc, quasiRed);
                            g.DrawLine(new DrawingPointF(center.X, vt), new DrawingPointF(center.X, vb), white);
                            break;
                        case CellState.DetectorTerminate:
                            g.FillEllipse(new Ellipse(center, s/3, s/3), white);
                            break;
                        case CellState.DetectorPropagate:
                            g.FillEllipse(new Ellipse(center, s/3, s/3), quasiWhite);
                            break;
                        }
                    }
                }
            } finally {
                foreach (var e in sineWavesTrace) e.Dispose();
                foreach (var e in sineWavesFill) e.Dispose();
            }

            g.EndDraw();
        }
        private IEnumerable<Cell> AllCells {
            get {
                return from i in CellColumnCount.Range()
                       from j in CellRowCount.Range()
                       select _cells[i, j];
            }
        }
        private readonly LifetimeExchanger _computeLifeExchanger = new LifetimeExchanger();
        public async void ComputeCircuit() {
            var life = _computeLifeExchanger.StartNextAndEndPreviousLifetime();
            var s = new Stopwatch();
            s.Start();

            var elements =
                AllCells
                .Where(e => e.State != CellState.Empty)
                .ToDictionary(e => new Position(e.X, e.Y), e => {
                    Func<Photon, Superposition<Photon>> fp;
                    if (e.State == CellState.BackSlashSplitter) {
                        fp = p => p.HalfSwapVelocity();
                    } else if (e.State == CellState.ForeSlashSplitter) {
                        fp = p => p.HalfNegateSwapVelocity();
                    } else if (e.State == CellState.BackSlashMirror) {
                        fp = p => p.SwapVelocity();
                    } else if (e.State == CellState.ForeSlashMirror) {
                        fp = p => p.SwapNegateVelocity();
                    } else {
                        fp = null;
                    }

                    Func<Photon, Superposition<May<Photon>>> fm;
                    if (fp != null) {
                        fm = p => fp(p).Transform<May<Photon>>(v => v.Maybe());
                    } else if (e.State == CellState.ForeSlashPolarizer) {
                        fm = p => p.Polarize(new Polarization(Dir.FromVector(1, 1)));
                    } else if (e.State == CellState.BackSlashPolarizer) {
                        fm = p => p.Polarize(new Polarization(Dir.FromVector(-1, 1)));
                    } else if (e.State == CellState.HorizontalPolarizer) {
                        fm = p => p.Polarize(new Polarization(Dir.FromVector(1, 0)));
                    } else if (e.State == CellState.VerticalPolarizer) {
                        fm = p => p.Polarize(new Polarization(Dir.FromVector(0, 1)));
                    } else {
                        fm = null;
                    }

                    Func<CircuitState, Superposition<CircuitState>> fc;
                    if (fm != null) {
                        fc = c => c.Photon.Match(p => fm(p).Transform<CircuitState>(p2 => c.WithPhoton(p2)), () => c.Super());
                    } else if (e.State == CellState.DetectorTerminate) {
                        fc = c => c.WithPhoton(May.NoValue);
                    } else if (e.State == CellState.DetectorPropagate) {
                        fc = c => c.WithDetection(new Position(e.X, e.Y));
                    } else {
                        throw new NotImplementedException();
                    }

                    return fc;
                });

            var initialState = new CircuitState(TimeSpan.Zero, new Photon(new Position(0, 10), Velocity.PlusX, default(Polarization)));
            foreach (var e in AllCells)
                e.Trace = 0;
            Waves.Clear();
            try {
                var state = initialState.Super();
                var n = 0;
                while (!life.IsDead) {
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
                        .Select(p => (p.Pos.X >= 0 && p.Pos.X < CellColumnCount && p.Pos.Y >= 0 && p.Pos.Y < CellRowCount ? e.WithTick() : e.WithPhoton(May.NoValue)).Super())
                        .Else(e.Super()));
                    if (Equals(state, newState2)) break;
                    state = newState2;
                    _message = state.ToString();

                    if (s.ElapsedMilliseconds > 50) {
                        await Task.Yield();
                        s.Restart();
                    }
                }
                if (!life.IsDead)
                    _message = state.ToString();
            } catch (Exception ex) {
                _message = ex.ToString();
            }
        }
    }
}
