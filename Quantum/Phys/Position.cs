using System.Diagnostics;
using Circuit.Phys;

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