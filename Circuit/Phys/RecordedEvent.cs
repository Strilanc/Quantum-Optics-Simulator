using System;
using System.Diagnostics;

namespace Circuit.Phys {
    [DebuggerDisplay("{ToString()}")]
    public struct RecordedEvent<T> {
        public readonly T Value;
        public readonly TimeSpan Time;
        public RecordedEvent(T value, TimeSpan time) {
            Value = value;
            Time = time;
        }
        public override string ToString() {
            return string.Format("{0} at t={1}", Value, Time.TotalMilliseconds);
        }
    }
}