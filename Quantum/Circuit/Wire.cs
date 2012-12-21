public sealed class Wire {
    public readonly string Name;
    public Wire(string name = null) {
        this.Name = name;
    }
    public override string ToString() {
        return this.Name ?? base.ToString();
    }
}