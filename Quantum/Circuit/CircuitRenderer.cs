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
        }

        public bool EnableClear { get; set; }

        public bool Show { get; set; }

        public virtual void Initialize(DeviceContext contextD2D) {
            this._sceneColorBrush = new SolidColorBrush(contextD2D, Color.Red);

            this._clock = Stopwatch.StartNew();
        }

        private void InitPathGeometry(RenderParams renderParams, float sizeX) {
            var sizeShape = sizeX / 4;

            // Creates a random geometry inside a circle
            _pathGeometry1 = new PathGeometry1(renderParams.DirectXResources.FactoryDirect2D);

            var pathSink = _pathGeometry1.Open();
            var startingPoint = new DrawingPointF(sizeShape / 2, 0);
            pathSink.BeginFigure(startingPoint, FigureBegin.Hollow);
            foreach (var i in 128.Range()) {
                var angle = i * Tau / 128;
                var b = (i & 1) != 0;
                var r = sizeShape * (float)(b ? Math.Sin(angle * 6) * 0.1 + 0.9 : Math.Cos(angle) * 0.1 + 0.4);
                var theta = angle + (b ? Tau / 24 : 0);
                pathSink.AddLine(new DrawingPointF(
                    r * (float)Math.Cos(theta),
                    r * (float)Math.Sin(theta)));
            }
            pathSink.EndFigure(FigureEnd.Open);
            pathSink.Close();
        }
        public virtual void Render(RenderParams renderParams) {
            var t = (float)_clock.Elapsed.TotalSeconds;
            if (!Show) return;

            var context2D = renderParams.DevicesAndContexts.ContextDirect2D;
            context2D.BeginDraw();

            if (EnableClear) context2D.Clear(Color.Black);

            var r = renderParams.SizedDeviceResources.RenderTargetBounds;
            var sizeX = (float)r.Width;
            var sizeY = (float)r.Height;
            var centerX = (float)(r.X + sizeX / 2);
            var centerY = (float)(r.Y + sizeY / 2);

            _textFormat = _textFormat ?? new TextFormat(renderParams.DirectXResources.FactoryDirectWrite, "Calibri", 96 * sizeX / 1920) {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            if (_pathGeometry1 == null) InitPathGeometry(renderParams, sizeX);

            context2D.TextAntialiasMode = TextAntialiasMode.Grayscale;
            context2D.Transform = Matrix.RotationZ((float)(Math.Cos(t * Tau / 2))) * Matrix.Translation(centerX, centerY, 0);
            context2D.DrawText(Message, _textFormat, new RectangleF(-sizeX / 2, -sizeY / 2, +sizeX / 2, sizeY / 2), _sceneColorBrush);

            context2D.Transform =
                  Matrix.Scaling((float)(Math.Cos(t * Tau / 4 * 0.25) / 4 + 0.75))
                * Matrix.RotationZ(t / 2)
                * Matrix.Translation(centerX, centerY, 0);
            context2D.DrawGeometry(_pathGeometry1, this._sceneColorBrush, 2);

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

            var initialState = new CircuitState(TimeSpan.Zero, new Photon(new Position(0, 0), Velocity.PlusX, default(Polarization)));
            foreach (var e in AllCells)
                e.Trace = 0;
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
