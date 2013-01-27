using Windows.UI.Xaml.Input;

namespace Quantum {
    public sealed partial class MainPage {
        public MainPage(CircuitRenderer circuit) {
            InitializeComponent();

            TappedEventHandler h = (sender, arg) => {
                var p = arg.GetPosition(this);
                var i = (p.X/this.ActualWidth*circuit.CellColumnCount).FloorInt();
                var j = (p.Y/this.ActualHeight*circuit.CellRowCount).FloorInt();
                var c = circuit._cells[i, j];
                c.State = (CircuitRenderer.CellState)(((int)c.State + 1)%11);
                circuit.ComputeCircuit();
            };
            this.Tapped += h;
            this.IsDoubleTapEnabled = false;
        }
    }
}
