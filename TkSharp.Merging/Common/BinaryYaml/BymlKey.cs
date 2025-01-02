using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using BymlLibrary;

namespace TkSharp.Merging.Common.BinaryYaml;

[DebuggerDisplay("KeyType = {Primary?.Value}, {Secondary?.Value}")]
public readonly struct BymlKey(Byml? primary)
{
    public bool IsEmpty => Primary is null;
    
    public readonly Byml? Primary = primary;
    
    public readonly Byml? Secondary;

    public BymlKey(Byml? primary, Byml? secondary) : this(primary)
    {
        Secondary = secondary;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not BymlKey key) {
            return false;
        }
        
        return Byml.ValueEqualityComparer.Default.Equals(Primary, key.Primary) && Byml.ValueEqualityComparer.Default.Equals(Secondary, key.Secondary);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Primary is null ? 0 : Byml.ValueEqualityComparer.Default.GetHashCode(Primary),
            Secondary is null ? 0 : Byml.ValueEqualityComparer.Default.GetHashCode(Secondary)
        );
    }

    public override string ToString()
    {
        return $"{Primary?.Value} ({Secondary?.Value})";
    }
}