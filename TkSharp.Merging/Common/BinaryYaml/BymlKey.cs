using System.Diagnostics;
using BymlLibrary;

namespace TkSharp.Merging.Common.BinaryYaml;

[DebuggerDisplay("KeyType = {Primary?.Type}, {Secondary?.Type}")]
public readonly struct BymlKey(Byml? primary)
{
    public bool IsEmpty => Primary is null;
    
    public readonly Byml? Primary = primary;
    
    public readonly Byml? Secondary;

    public BymlKey(Byml? primary, Byml? secondary) : this(primary)
    {
        Secondary = secondary;
    }

    public class Comparer : IEqualityComparer<BymlKey>
    {
        public static readonly Comparer Default = new();
        
        public bool Equals(BymlKey x, BymlKey y)
        {
            return Byml.ValueEqualityComparer.Default.Equals(x.Primary, y.Primary) && Byml.ValueEqualityComparer.Default.Equals(x.Secondary, y.Secondary);
        }

        public int GetHashCode(BymlKey obj)
        {
            return HashCode.Combine(
                obj.Primary is null ? 0 : Byml.ValueEqualityComparer.Default.GetHashCode(obj.Primary),
                obj.Secondary is null ? 0 : Byml.ValueEqualityComparer.Default.GetHashCode(obj.Secondary)
            );
        }
    }
}