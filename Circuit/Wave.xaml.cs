using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace Circuit {
    public partial class Wave {
        private const double Tau = Math.PI*2;
        private Complex _amplitude;

        public Complex Amplitude {
            get { return _amplitude; }
            set {
                _amplitude = value;
                RefreshView();
            }
        }
        public Wave() {
            InitializeComponent();
            Loaded += (sender, arg) => RefreshView();
            SizeChanged += (sender, arg) => RefreshView();
        }
        private void RefreshView() {
            if (_amplitude == 0) {
                WaveImage1.Visibility = WaveImage2.Visibility = Visibility.Hidden;
                return;
            }
            var w = ActualWidth;
            var h = ActualHeight;
            var s = _amplitude.Magnitude;
            var p = -_amplitude.Phase/Tau;

            if (p < 0) p += 1;
            var mh = (1 - s)*h/2;
            WaveImage1.Margin = new Thickness(-p * w, mh, p * w, mh);
            //WaveImage2.Margin = new Thickness(w-p * w, mh, -w+p * w, mh);

            //WaveImage1.Visibility = WaveImage2.Visibility = Visibility.Visible;
            WaveImage1.Visibility = Visibility.Visible;
        }
    }
}
