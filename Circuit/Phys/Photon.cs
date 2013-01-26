using System.Diagnostics;
using System.Numerics;

namespace Circuit.Phys {
    [DebuggerDisplay("{ToString()}")]
    public struct Photon {
        public readonly Position Pos;
        public readonly Velocity Vel;
        public readonly Polarization Pol;
        public Photon(Position pos, Velocity vel, Polarization pol) {
            this.Pos = pos;
            this.Vel = vel;
            this.Pol = pol;
        }
        public Superposition<Photon> SwapVelocity() {
            return new Photon(this.Pos, new Velocity(this.Vel.Y, this.Vel.X), this.Pol).Super();
        }
        public Superposition<Photon> SwapNegateVelocity() {
            return new Photon(this.Pos, new Velocity(-this.Vel.Y, -this.Vel.X), this.Pol).Super();
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
            return string.Format("{0}, {1}, {2}", this.Pos, this.Vel, this.Pol);
        }
    }
}