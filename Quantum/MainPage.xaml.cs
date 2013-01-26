namespace Quantum {
    public sealed partial class MainPage {
        public MainPage(CircuitRenderer circuit) {
            InitializeComponent();
            
            this.Tapped += (sender, arg) => {
                var p = arg.GetPosition(this);
                var i = (p.X / this.ActualWidth * circuit.CellColumnCount).FloorInt();
                var j = (p.Y / this.ActualHeight * circuit.CellRowCount).FloorInt();
                var c = circuit._cells[i, j];
                c.State = (CircuitRenderer.CellState)(((int)c.State + 1) % 11);
                circuit.ComputeCircuit();
            };
        }
    }
}
