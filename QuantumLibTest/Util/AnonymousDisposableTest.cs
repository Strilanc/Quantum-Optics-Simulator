using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AnonymousDisposableTest {
    [TestMethod]
    public void EmptyDisposable() {
        var r = new AnonymousDisposable();
        r.Dispose();
        r.Dispose();
    }
    [TestMethod]
    public void DisposesExactlyOnce() {
        var n = 0;
        var r = new AnonymousDisposable(() => n += 1);
        n.AssertEquals(0);
        r.Dispose();
        n.AssertEquals(1);
        r.Dispose();
        n.AssertEquals(1);
    }
}
