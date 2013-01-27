using System;
using System.Diagnostics;
using System.Numerics;
using Strilanc.Value;

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
            return new Photon(this.Pos, new Velocity(this.Vel.Y, this.Vel.X), this.Pol).Super() * Complex.ImaginaryOne;
        }
        public Superposition<Photon> SwapNegateVelocity() {
            return new Photon(this.Pos, new Velocity(-this.Vel.Y, -this.Vel.X), this.Pol).Super() * Complex.ImaginaryOne;
        }
        public Superposition<Photon> HalfSwapVelocity() {
            return this.Super()
                   | SwapVelocity();
        }
        public Superposition<Photon> HalfNegateSwapVelocity() {
            return this.Super()
                   | SwapNegateVelocity();
        }
        public Superposition<May<Photon>> Polarize(Polarization polarizer) {
            var turn = polarizer.Dir - Pol.Dir;
            return new Photon(Pos, Vel, polarizer).Maybe().Super()*Math.Cos(turn.NaturalAngle)
                + May<Photon>.NoValue.Super()*Math.Sin(turn.NaturalAngle);
        }
        public override string ToString() {
            return string.Format("{0}, {1}, {2}", this.Pos, this.Vel, this.Pol);
        }
    }
}