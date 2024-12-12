using System.Collections;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Merging.ChangelogBuilders.BinaryYaml;

public class BymlKeyedArrayChangelogBuilder<T>(string key) : IBymlArrayChangelogBuilder where T : IEquatable<T>
{
    private readonly string _key = key;

    public bool LogChanges(ref BymlTrackingInfo info, ref Byml root, BymlArray src, BymlArray vanilla)
    {
        BymlArrayChangelog changelog = [];
        int detectedAdditions = 0;

        using var vanillaRecordsFound = RentedBitArray.Create(vanilla.Count);

        for (int i = 0; i < src.Count; i++) {
            Byml element = src[i];
            if (!element.GetMap().TryGetValue(_key, out Byml? keyEntry)) {
                TkLog.Instance.LogWarning(
                    "Entry '{Index}' in '{Type}' was missing a {Key} field.",
                    i, info.Type.ToString(), _key);
                changelog.Add((i - detectedAdditions, BymlChangeType.Add, element));
                detectedAdditions++;
                continue;
            }
            
            if (!TryGetIndex(vanilla, keyEntry.Get<T>(), _key, out int vanillaIndex)) {
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