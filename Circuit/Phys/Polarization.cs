using System.Diagnostics;
using Strilanc.Angle;

namespace Circuit.Phys {
    [DebuggerDisplay("{ToString()}")]
    public struct Polarization {
        private readonly Dir _dir;
        public Dir Dir { get { return _dir; } }
        public Polarization(Dir dir) {
            if (dir.UnitY < 0) dir = dir + Turn.OneTurnClockwise/2;
            _dir = dir;
        }
        public override string ToString() {
            return _dir.ToString();
        }
    }
}