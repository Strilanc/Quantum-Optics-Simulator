static class CircuitUtils {
    public static BeamSplitter<T> Split<T>(this MockProperty<T, Wire> wireProperty, Wire input, Wire throughput, Wire reflectput) {
        return new BeamSplitter<T>(input, throughput, reflectput, wireProperty);
    }
    public static CrossBeamSplitter<T> CrossSplit<T>(this MockProperty<T, Wire> wireProperty, Wire inputH, Wire inputV, Wire outputH, Wire outputV) {
        return new CrossBeamSplitter<T>(inputH, inputV, outputH, outputV, wireProperty);
    }
    public static Reflector<T> Reflect<T>(this MockProperty<T, Wire> wireProperty, Wire input, Wire output) {
        return new Reflector<T>(input, output, wireProperty);
    }
    public static Detector<T> Detect<T>(this MockProperty<T, Wire> wireProperty, Wire input, MockProperty<T, bool> detectedProperty, Wire output = null) {
        return new Detector<T>(input, output, detectedProperty, wireProperty);
    }
}