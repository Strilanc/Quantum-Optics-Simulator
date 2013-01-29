using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.LinqToCollections;

[TestClass]
public class ObservableValueTest {
    [TestMethod]
    public void UpdateCurrent() {
        var r = new ObservableValue<int>(1);
        r.Current.AssertEquals(1);
        r.Update(2);
        r.Current.AssertEquals(2);
    }
    [TestMethod]
    public void AdjustCurrent() {
        var r = new ObservableValue<int>(1);
        r.Current.AssertEquals(1);
        r.Adjust(e => e + 1);
        r.Current.AssertEquals(2);
        r.Adjust(e => e + 1);
        r.Current.AssertEquals(3);
    }
    [TestMethod]
    public void AdjustRace() {
        var r = new ObservableValue<int>();
        Task.WhenAll(10.Range().Select(e => Task.Factory.StartNew(() => {
            foreach (var i in 1000.Range()) {
                r.Adjust(x => x + 1);
            }
        }, TaskCreationOptions.LongRunning))).AssertRanToCompletion();
        r.Current.AssertEquals(10 * 1000);
    }
    [TestMethod]
    public void ObserveCurrent() {
        var r = new ObservableValue<int>();
        var li = new List<int>();
        r.Subscribe(li.Add);
        li.AssertSequenceEquals(new[] {0});

        r.Update(1, skipIfEqual: true);
        li.AssertSequenceEquals(new[] { 0, 1 });

        r.Update(1, skipIfEqual: true);
        li.AssertSequenceEquals(new[] { 0, 1 });

        r.Update(1, skipIfEqual: false);
        li.AssertSequenceEquals(new[] { 0, 1, 1 });
    }
}
