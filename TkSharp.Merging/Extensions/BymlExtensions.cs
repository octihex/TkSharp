using System.Collections.Frozen;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using TkSharp.Merging.Common.BinaryYaml;

namespace TkSharp.Merging.Extensions;

public static class BymlExtensions
{
    public static FrozenDictionary<BymlKey, int> CreateIndexCache(this BymlArray array, in BymlKeyName keyName)
    {
        switch (array.Count) {
            case 0:
            case 1 when array[0].Type != BymlNodeType.Map:
                return FrozenDictionary<BymlKey, int>.Empty;
        }

        Dictionary<BymlKey, int> indexCache = new(BymlKey.Comparer.Default);

        for (int i = 0; i < array.Count; i++) {
            Byml entry = array[i];
            if (!keyName.TryGetKey(entry, out BymlKey key)) {
                throw new InvalidOperationException($"Invalid BYML key name, vanilla entry at '{i}' does not match the key type {keyName}");
            }
            
            indexCache[key] = i;
        }
        
        return indexCache.ToFrozenDictionary(BymlKey.Comparer.Default);
    }
}