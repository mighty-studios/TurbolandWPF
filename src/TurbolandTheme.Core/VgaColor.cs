namespace TurbolandTheme.Core;

/// <summary>
/// A 24-bit color kept framework-agnostic so the core token library stays portable.
/// </summary>
public readonly struct VgaColor : System.IEquatable<VgaColor>
{
    public VgaColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    /// <summary>ARGB with opaque alpha, layout compatible with WPF <c>Color.FromArgb</c> results.</summary>
    public int ToArgb() => unchecked((int)0xFF000000) | (R << 16) | (G << 8) | B;

    public string ToHex() => string.Format("#{0:X2}{1:X2}{2:X2}", R, G, B);

    public bool Equals(VgaColor other) => R == other.R && G == other.G && B == other.B;

    public override bool Equals(object? obj) => obj is VgaColor c && Equals(c);

    public override int GetHashCode() => ToArgb();

    public override string ToString() => ToHex();

    public static bool operator ==(VgaColor left, VgaColor right) => left.Equals(right);

    public static bool operator !=(VgaColor left, VgaColor right) => !left.Equals(right);
}
