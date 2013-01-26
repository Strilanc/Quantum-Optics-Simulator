using System;
using System.Diagnostics;

namespace Circuit.Phys {
    [DebuggerDisplay("{ToString()}")]
    public struct Velocity {
        public static readonly Velocity PlusX = new Velocity(1, 0);
        public static readonly Velocity MinusX = new Velocity(-1, 0);
        public static readonly Velocity PlusY = new Velocity(0, 1);
        public static readonly Velocity MinusY = new Velocity(0, -1);

        private readonly int _xMinus1;
        private readonly int _y;
        public int X { get { return this._xMinus1 + 1; }}
        public int Y { get { return this._y; }}
        public Velocity(int x, int y) {
            if (x * y != 0) throw new ArgumentException();
            if (Math.Abs(x + y) != 1) throw new ArgumentException();
            this._xMinus1 = x - 1;
            this._y = y;
        }
        public override string ToString() {
            return string.Format("<{0:+#;-#;0}, {1:+#;-#;0}>", this.X, this.Y);
        }
    }
}