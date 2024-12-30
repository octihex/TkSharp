using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace TkSharp.Merging.Common.BinaryYaml;

public readonly struct BymlKeyName
{
    public bool IsEmpty => Primary is null && GetKeyNode is null;
    
    public bool IsFunc => GetKeyNode is not null;
    
    public readonly string? Primary;

    public readonly Func<BymlMap, Byml?>? GetKeyNode;

    public static implicit operator BymlKeyName(string? primary) => new(primary);
    
    public static implicit operator BymlKeyName(Func<BymlMap, Byml?> getKeyNode) => new(getKeyNode);

    public BymlKeyName(string? primary)
    {
        Primary = primary;
    }
    
    public BymlKeyName(Func<BymlMap, Byml?>? getKeyNode)
    {
        GetKeyNode = getKeyNode;
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

    public BymlKey GetKeyFromMap(BymlMap map)
    {
        if (IsEmpty) {
            return default;
        }
        
        return GetKeyNode is not null
            ? new BymlKey(GetKeyNode.Invoke(map))
            : new BymlKey(map!.GetValueOrDefault(Primary));
    }

    public override string ToString()
    {
        return $"{(IsFunc ? "(delegate)" : Primary)}";
    }
}