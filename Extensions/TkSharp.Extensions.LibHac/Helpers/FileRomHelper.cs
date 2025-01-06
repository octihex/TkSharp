using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using TkSharp.Extensions.LibHac.Extensions;

namespace TkSharp.Extensions.LibHac.Helpers
{
    public class FileRomHelper : ILibHacRomHelper
    {
        private IStorage _storage;

        public SwitchFs Initialize(string filePath, KeySet keys)
        {
            _storage = new LocalStorage(filePath, FileAccess.Read);
            return _storage.GetSwitchFs(filePath, keys);
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
} 