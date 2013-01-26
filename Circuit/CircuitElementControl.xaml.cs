using System.Numerics;
using System.Windows;

namespace Circuit {
    public partial class CircuitElementControl {
        public CircuitElementControl() {
            InitializeComponent();
            empty.Visibility=Visibility.Visible;
        }
        private MainWindow.CellState _state;
        private Visibility V(bool b) {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        public MainWindow.CellState State {
            get { return _state; }
            set { 
                _state = value;
                empty.Visibility = V(value == MainWindow.CellState.Empty);
                mirrorBack.Visibility = V(value == MainWindow.CellState.BackSlashMirror);
                mirrorFore.Visibility = V(value == MainWindow.CellState.ForeSlashMirror);
                splitterBack.Visibility = V(value == MainWindow.CellState.BackSlashSplitter);
                splitterFore.Visibility = V(value == MainWindow.CellState.ForeSlashSplitter);
                detectorCatch.Visibility = V(value == MainWindow.CellState.DetectorTerminate);
                detectorPass.Visibility = V(value == MainWindow.CellState.DetectorPropagate);
                polarizerVertical.Visibility = V(value == MainWindow.CellState.VerticalPolarizer);
                polarizerHorizontal.Visibility = V(value == MainWindow.CellState.HorizontalPolarizer);
                polarizerBackSlash.Visibility = V(value == MainWindow.CellState.BackSlashPolarizer);
                polarizerForeSlash.Visibility = V(value == MainWindow.CellState.ForeSlashPolarizer);
            }
        }
    }
}
