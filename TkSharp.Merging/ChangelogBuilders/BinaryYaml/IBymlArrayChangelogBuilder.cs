using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace TkSharp.Merging.ChangelogBuilders.BinaryYaml;

public interface IBymlArrayChangelogBuilder
{
    bool LogChanges(ref BymlTrackingInfo info, ref Byml root, BymlArray src, BymlArray vanilla);
}