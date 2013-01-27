using System.Reactive.Linq;
using SharpDX.Direct2D1;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using System;

namespace Quantum {
    sealed partial class App {
        private SwapChainBackgroundPanelTarget _target;

        public App() {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args) {
            var shapeRenderer = new CircuitRenderer();
            var content = new MainPage(shapeRenderer);
            Window.Current.Content = content;
            Window.Current.Activate();

            // Initialize
#if DEBUG
            var debugLevel = DebugLevel.Information;
#else
            var debugLevel = DebugLevel.None;
#endif
            this._target = new SwapChainBackgroundPanelTarget(content, debugLevel);
            _target.DevicesAndContexts.WhereNotNull().Select(e => e.ContextDirect2D).Subscribe(shapeRenderer.Initialize);

            // Setup Rendering
            _target.OnRender.Subscribe(shapeRenderer.Render);
            CompositionTarget.Rendering += (sender, e) => {
                _target.RenderAll();
                _target.Present();
            };
        }
    }
}
