using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;

namespace TkSharp.Extensions.LibHac.Common;

public class NxRefFileStream(UniqueRef<IFile> file) : NxFileStream(file.Get, OpenMode.Read, leaveOpen: false)
{
    private UniqueRef<IFile> _file = file;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (disposing) {
            _file.Destroy();
        }
    }
}