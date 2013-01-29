using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class OnetimeLockTest {
    [TestMethod]
    public void TryAcquire() {
        // TryAcquired acquires the lock once
        var r = new OnetimeLock();
        r.TryAcquire().AssertIsTrue();
        r.TryAcquire().AssertIsFalse();
        r.TryAcquire().AssertIsFalse();
    }
    [TestMethod]
    public void IsAcquired() {
        // IsAcquired determines if the lock is acquired
        var r = new OnetimeLock();
        r.IsAcquired().AssertIsFalse();
        r.TryAcquire().AssertIsTrue();
        r.IsAcquired().AssertIsTrue();
    }
    [TestMethod]
    public void TryAcquire_Race() {
        // racing TryAcquires: only one wins
        var r = new OnetimeLock();
        var x = Task.WhenAll(
            Enumerable.Range(0, 10).Select(
                e => Task.Factory.StartNew(
                    () => r.TryAcquire(),
                    TaskCreationOptions.LongRunning))).AssertRanToCompletion();
        x.Where(e => e).Count().AssertEquals(1);
        r.IsAcquired().AssertIsTrue();
    }
}
