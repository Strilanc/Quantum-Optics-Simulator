using System;
using System.Diagnostics;
using System.Numerics;

[DebuggerDisplay("{ToString()}")]
public struct Polarization {
    public static readonly Polarization Horizontal = new Polarization(false);
    public static readonly Polarization Vertical = new Polarization(true);
    
    private readonly bool _vertical;
    public bool IsVertical { get { return _vertical; } }
    public bool IsHorizontal { get { return !_vertical; } }
    public Polarization(bool vertical) {
        this._vertical = vertical;
    }
    public override string ToString() {
        return _vertical ? "|" : "--";
    }
}
[DebuggerDisplay("{ToString()}")]
public struct Position {
    public readonly int X;
    public readonly int Y;
    public Position(int x, int y) {
        this.X = x;
        this.Y = y;
    }
    public override string ToString() {
        return string.Format("({0}, {1})", X, Y);
    }
    public static Position operator +(Position p, Velocity v) {
        return new Position(p.X + v.X, p.Y + v.Y);
    }
}
[DebuggerDisplay("{ToString()}")]
public struct Velocity {
    public static readonly Velocity PlusX = new Velocity(1, 0);
    public static readonly Velocity MinusX = new Velocity(-1, 0);
    public static readonly Velocity PlusY = new Velocity(0, 1);
    public static readonly Velocity MinusY = new Velocity(0, -1);

    private readonly int _xMinus1;
    private readonly int _y;
    public int X { get { return _xMinus1 + 1; }}
    public int Y { get { return _y; }}
    public Velocity(int x, int y) {
        if (x * y != 0) throw new ArgumentException();
        if (Math.Abs(x + y) != 1) throw new ArgumentException();
        this._xMinus1 = x - 1;
        this._y = y;
    }
    public override string ToString() {
        return string.Format("<{0:+#;-#;0}, {1:+#;-#;0}>", X, Y);
    }
}
[DebuggerDisplay("{ToString()}")]
public struct Photon {
    public readonly Position Pos;
    public readonly Velocity Vel;
    public readonly Polarization Pol;
    public Photon(Position pos, Velocity vel, Polarization pol) {
        Pos = pos;
        Vel = vel;
        Pol = pol;
    }
    public Superposition<Photon> SwapVelocity() {
        return new Photon(Pos, new Velocity(Vel.Y, Vel.X), Pol).Super();
    }
    public Superposition<Photon> SwapNegateVelocity() {
        return new Photon(Pos, new Velocity(-Vel.Y, -Vel.X), Pol).Super();
    }
    public Superposition<Photon> HalfSwapVelocity() {
        return this.Super()
            + SwapVelocity() * Complex.ImaginaryOne;
    }
    public Superposition<Photon> HalfNegateSwapVelocity() {
        return this.Super()
            + SwapNegateVelocity() * Complex.ImaginaryOne;
    }
    public override string ToString() {
        return string.Format("{0}, {1}, {2}", Pos, Vel, Pol);
    }
}