using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace TkSharp.Merging.Common.BinaryYaml;

public static class BymlKeyNames
{
    public static readonly BymlKeyName LevelSensorInfo = new(map => {
        if (!map.TryGetValue("Elements", out Byml? elementNode)
            || elementNode.Value is not BymlArray elements
            || elements is not [{ Value: BymlMap firstElement }, ..]) {
            return null;
        }

        return firstElement.GetValueOrDefault("ActorNameHash");
    });
}