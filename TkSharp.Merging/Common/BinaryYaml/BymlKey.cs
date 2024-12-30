using System.Diagnostics;
using BymlLibrary;

namespace TkSharp.Merging.Common.BinaryYaml;

[DebuggerDisplay("KeyType = {Primary?.Value}")]
public readonly struct BymlKey(Byml? primary)
{
    public bool IsEmpty => Primary is null;
    
    public readonly Byml? Primary = primary;

    public class Comparer : IEqualityComparer<BymlKey>
    {
        public static readonly Comparer Default = new();
        
        public bool Equals(BymlKey x, BymlKey y)
        {
            return Byml.ValueEqualityComparer.Default.Equals(x.Primary, y.Primary);
        }

        public int GetHashCode(BymlKey obj)
        {
            return obj.Primary is not null ? Byml.ValueEqualityComparer.Default.GetHashCode(obj.Primary) : 0;
        }
    }
}