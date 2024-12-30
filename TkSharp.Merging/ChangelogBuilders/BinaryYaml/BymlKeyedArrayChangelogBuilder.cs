using System.Collections.Frozen;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Merging.Common.BinaryYaml;
using TkSharp.Merging.Extensions;

namespace TkSharp.Merging.ChangelogBuilders.BinaryYaml;

public class BymlKeyedArrayChangelogBuilder<T>(BymlKeyName keyName) : IBymlArrayChangelogBuilder where T : IEquatable<T>
{
    private readonly BymlKeyName _key = keyName;

    public bool LogChanges(ref BymlTrackingInfo info, ref Byml root, BymlArray src, BymlArray vanilla)
    {
        BymlArrayChangelog changelog = [];
        int detectedAdditions = 0;

        FrozenDictionary<BymlKey, int> vanillaIndexMap = vanilla.CreateIndexCache(_key);
        using var vanillaRecordsFound = RentedBitArray.Create(vanilla.Count);

        for (int i = 0; i < src.Count; i++) {
            Byml element = src[i];
            if (!_key.TryGetKey(element, out BymlKey key)) {
                TkLog.Instance.LogWarning(
                    "Entry '{Index}' in '{Type}' was missing the key field '{Key}'.",
                    i, info.Type.ToString(), _key);
                changelog.Add((i - detectedAdditions, BymlChangeType.Add, element));
                detectedAdditions++;
                continue;
            }
            
            if (!vanillaIndexMap.TryGetValue(key, out int vanillaIndex)) {
                int relativeIndex = i - detectedAdditions;
                changelog.Add(((vanilla.Count > relativeIndex) switch {
                    true => relativeIndex, false => i
                }, BymlChangeType.Add, element));
                
                detectedAdditions++;
                continue;
            }

            if (BymlChangelogBuilder.LogChangesInline(ref info, ref element, vanilla[vanillaIndex])) {
                src[i] = BymlChangeType.Remove;
                goto UpdateVanilla;
            }

            changelog.Add((vanillaIndex, BymlChangeType.Edit, element));

        UpdateVanilla:
            vanillaRecordsFound[vanillaIndex] = true;
        }

        for (int i = 0; i < vanilla.Count; i++) {
            if (vanillaRecordsFound[i]) {
                continue;
            }

            changelog.Add((i, BymlChangeType.Remove, new Byml()));
        }

        root = changelog;
        return changelog.Count == 0;
    }

    private static bool TryGetIndex(IList<Byml> list, T element, string key, out int index)
    {
        int len = list.Count;
        for (int i = 0; i < len; i++) {
            if (list[i].Value is not BymlMap map || !map[key].Get<T>().Equals(element)) {
                continue;
            }

            index = i;
            return true;
        }

        index = -1;
        return false;
    }
}