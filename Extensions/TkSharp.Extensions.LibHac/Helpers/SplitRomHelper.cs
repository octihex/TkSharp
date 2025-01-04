using LibHac.Common.Keys;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using LibHac.Tools.Fs;
using TkSharp.Extensions.LibHac.Extensions;

namespace TkSharp.Extensions.LibHac.Helpers
{
    public class SplitRomHelper : IDisposable
    {
        private ConcatenationStorage _storage;

        public SwitchFs InitializeFromSplitFiles(string splitDirectory, KeySet keys)
        {
            var splitFiles = Directory.GetFiles(splitDirectory)
                .OrderBy(f => f)
                .Select(f => new LocalStorage(f, FileAccess.Read))
                .ToArray();

            _storage = new ConcatenationStorage(splitFiles, true);
            return _storage.GetSwitchFs("rom", keys);
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
} 