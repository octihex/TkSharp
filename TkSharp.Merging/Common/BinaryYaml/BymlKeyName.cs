using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace TkSharp.Merging.Common.BinaryYaml;

public readonly struct BymlKeyName(string? primary)
{
    public bool IsEmpty => Primary is null;
    
    public bool IsPair => Secondary is not null;
    
    public readonly string? Primary = primary;

    public readonly string? Secondary;

    public static implicit operator BymlKeyName(string? primary) => new(primary);
    
    public static implicit operator BymlKeyName((string Primary, string Secondary) pair) => new(pair.Primary, pair.Secondary);

    public static implicit operator string?(BymlKeyName bymlKey) => bymlKey.Primary;

    public static implicit operator (string, string)?(BymlKeyName bymlKey) => bymlKey.IsEmpty ? null : (bymlKey.Primary!, bymlKey.Secondary!);

    public BymlKeyName(string primary, string? secondary) : this(primary)
    {
        Secondary = secondary;
    }

    public bool TryGetKey(Byml node, out BymlKey key)
    {
        if (node.Value is BymlMap map) {
            key = GetKeyFromMap(map);
            return !key.IsEmpty;
        }

        key = default;
        return false;
    }

    public BymlKey GetKey(Byml node)
    {
        if (node.Value is BymlMap map) {
            return GetKeyFromMap(map);
        }

        return default;
    }

    public BymlKey GetKeyFromMap(IReadOnlyDictionary<string, Byml> map)
    {
        if (IsEmpty) {
            return default;
        }
        
        return IsPair 
            ? new BymlKey(map!.GetValueOrDefault(Primary), map!.GetValueOrDefault(Secondary))
            : new BymlKey(map!.GetValueOrDefault(Primary));
    }
}