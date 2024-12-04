using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace TkSharp.Merging.Mergers.BinaryYaml;

public class BymlRowComparer(string keyName) : IComparer<Byml>
{
    public int Compare(Byml? x, Byml? y)
    {
        return (x?.Value, y?.Value) switch {
            (BymlMap xMap, BymlMap yMap) => (xMap.GetValueOrDefault(keyName)?.Value, yMap.GetValueOrDefault(keyName)?.Value) switch {
                (string xStringValue, string yStringValue) => StringComparer.Ordinal.Compare(xStringValue, yStringValue),
                (long xInt64Value, long yInt64Value) => xInt64Value.CompareTo(yInt64Value),
                (ulong xUInt64Value, ulong yUInt64Value) => xUInt64Value.CompareTo(yUInt64Value),
                (uint xUIntValue, uint yUIntValue) => xUIntValue.CompareTo(yUIntValue),
                (int xIntValue, int yIntValue) => xIntValue.CompareTo(yIntValue),
                _ => 0
            },
            _ => 0
        };
    }
}
