using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Strilanc.LinqToCollections;
using Matrix = SharpDX.Matrix;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace Quantum {
    public class CircuitRenderer {
        private const float Tau = (float)Math.PI * 2;
        private TextFormat _textFormat;
        private Brush _sceneColorBrush;
        private PathGeometry1 _pathGeometry1;
        private Stopwatch _clock;

        public CircuitRenderer() {
            EnableClear = true;
            Show = true;
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
            context2D.DrawText("SharpDX\nDirect2D1\nDirectWrite", _textFormat, new RectangleF(-sizeX / 2, -sizeY / 2, +sizeX / 2, sizeY / 2), _sceneColorBrush);

            context2D.Transform =
                  Matrix.Scaling((float)(Math.Cos(t * Tau / 4 * 0.25) / 4 + 0.75))
                * Matrix.RotationZ(t / 2)
                * Matrix.Translation(centerX, centerY, 0);
            context2D.DrawGeometry(_pathGeometry1, this._sceneColorBrush, 2);

            context2D.EndDraw();
        }

    }
}
