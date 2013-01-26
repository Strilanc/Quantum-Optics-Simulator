using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;

public static class UIUtil {
    public static IObservableLatest<Rect> ObservableCoreWindowBounds {
        get {
            return new AnonymousObservableLatest<Rect>(
                observer => {
                    TypedEventHandler<CoreWindow, WindowSizeChangedEventArgs> update = (sender, arg) => observer.OnNext(Window.Current.CoreWindow.Bounds);
                    Window.Current.CoreWindow.SizeChanged += update;
                    update(null, null);
                    return new AnonymousDisposable(() => Window.Current.CoreWindow.SizeChanged -= update);
                },
                () => Window.Current.CoreWindow.Bounds);
        }
    }
    public static IObservableLatest<double> ObservableDisplayPropertiesLogicalDpi {
        get {
            return new AnonymousObservableLatest<double>(
                observer => {
                    DisplayPropertiesEventHandler update = sender => observer.OnNext(DisplayProperties.LogicalDpi);
                    DisplayProperties.LogicalDpiChanged += update;
                    update(null);
                    return new AnonymousDisposable(() => DisplayProperties.LogicalDpiChanged -= update);
                },
                () => DisplayProperties.LogicalDpi);
        }
    }
}
