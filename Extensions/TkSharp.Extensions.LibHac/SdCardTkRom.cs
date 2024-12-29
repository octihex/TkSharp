using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.IO.Parsers;
using TkSharp.Extensions.LibHac.Extensions;
using IOPath = System.IO.Path;

namespace TkSharp.Extensions.LibHac;

public sealed class SdCardTkRom : ITkRom, IDisposable
{
    public const ulong EX_KING_APP_ID = 0x0100F2C0115B6000;
    
    private readonly UniqueRef<IAttributeFileSystem> _localFs;
    private readonly AesXtsFileSystem _encFs;
    private readonly TkChecksums _checksums;
    private readonly IFileSystem _fileSystem;
    
    public int GameVersion { get; }
    public string NsoBinaryId { get; }
    public TkZstd Zstd { get; }
    public IDictionary<string, string> AddressTable { get; }
    public Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> EventFlowVersions { get; }
    public Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> EffectVersions { get; }

    public SdCardTkRom(TkChecksums checksums, string keysFolderPath, string contentPath)
    {
        Console.WriteLine("Initializing SdCardTkRom...");

        _checksums = checksums;
        
        KeySet keys = new();
        Console.WriteLine("Reading keys...");
        ExternalKeyReader.ReadKeyFile(keys,
            prodKeysFilename: IOPath.Combine(keysFolderPath, "prod.keys"),
            titleKeysFilename: IOPath.Combine(keysFolderPath, "title.keys")
        );

        Console.WriteLine("Keys loaded successfully.");

        Console.WriteLine("Creating local file system...");
        LocalFileSystem.Create(out var localFs, contentPath).ThrowIfFailure();
        _localFs = new UniqueRef<IAttributeFileSystem>(localFs);
        var concatFs = new ConcatenationFileSystem(ref _localFs);

        Console.WriteLine("Local file system created.");

        using var contentDirPath = new global::LibHac.Fs.Path();
        Console.WriteLine("Setting up content directory path...");
        PathFunctions.SetUpFixedPath(ref contentDirPath.Ref(), "/Nintendo/Contents"u8).ThrowIfFailure();

        var contentDirFs = new SubdirectoryFileSystem(concatFs);
        Console.WriteLine("Initializing content directory file system...");
        contentDirFs.Initialize(in contentDirPath).ThrowIfFailure();

        Console.WriteLine("Content directory file system initialized.");

        Console.WriteLine("Setting up encrypted file system...");
        _encFs = new AesXtsFileSystem(contentDirFs, keys.SdCardEncryptionKeys[1].DataRo.ToArray(), 0x4000);

        Console.WriteLine("Encrypted file system initialized.");

        var appFs = new SubdirectoryFileSystem(_encFs);
        using var appPath = new global::LibHac.Fs.Path();
        var appIdPath = new U8String($"/{EX_KING_APP_ID:X16}");
        Console.WriteLine("Setting up application path...");
        PathFunctions.SetUpFixedPath(ref appPath.Ref(), appIdPath).ThrowIfFailure();

        Console.WriteLine("Verifying application ID on the SD card...");
        try
        {
            appFs.Initialize(in appPath).ThrowIfFailure();
            string testFilePath = "/System/RegionLangMask.txt";
            UniqueRef<IFile> testFile = new();
            appFs.OpenFile(ref testFile, testFilePath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

            Console.WriteLine("Application ID found and verified.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to verify application ID: {ex.Message}");
            throw;
        }

        Console.WriteLine("Application file system initialized.");

        Console.WriteLine("Listing all files in the file system...");
        ListAllFiles(_fileSystem, "/");

        Console.WriteLine("Setting up region language mask...");
        try
        {
            string regionLangMaskPath = "/System/RegionLangMask.txt";
            Console.WriteLine($"Opening file: {regionLangMaskPath}");
            using Stream regionLangMaskFs = _fileSystem.OpenFileStream(regionLangMaskPath);
            using RentedBuffer<byte> regionLangMask = RentedBuffer<byte>.Allocate(regionLangMaskFs);
            GameVersion = RegionLangMaskParser.ParseVersion(regionLangMask.Span, out string nsoBinaryId);
            NsoBinaryId = nsoBinaryId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open region language mask: {ex.Message}");
            throw;
        }

        Console.WriteLine("Setting up Zstd dictionary...");
        {
            using Stream zsDicFs = _fileSystem.OpenFileStream("/Pack/ZsDic.pack.zs");
            Zstd = new TkZstd(zsDicFs);
        }

        Console.WriteLine("Setting up address table...");
        {
            using Stream addressTableFs = _fileSystem.OpenFileStream($"/System/AddressTable/Product.{GameVersion}.Nin_NX_NVN.atbl.byml.zs");
            using RentedBuffer<byte> addressTableBuffer = RentedBuffer<byte>.Allocate(addressTableFs);
            AddressTable = AddressTableParser.ParseAddressTable(addressTableBuffer.Span, Zstd);
        }

        Console.WriteLine("Setting up event flow file entry...");
        {
            using Stream eventFlowFileEntryFs = _fileSystem.OpenFileStream($"/{AddressTable["Event/EventFlow/EventFlowFileEntry.Product.byml"]}.zs");
            using RentedBuffer<byte> eventFlowFileEntryBuffer = RentedBuffer<byte>.Allocate(eventFlowFileEntryFs);
            EventFlowVersions = EventFlowFileEntryParser.ParseFileEntry(eventFlowFileEntryBuffer.Span, Zstd);
        }

        Console.WriteLine("Setting up effect info...");
        {
            using Stream effectInfoFs = _fileSystem.OpenFileStream($"/{AddressTable["Effect/EffectFileInfo.Product.Nin_NX_NVN.byml"]}.zs");
            using RentedBuffer<byte> effectInfoBuffer = RentedBuffer<byte>.Allocate(effectInfoFs);
            EffectVersions = EffectInfoParser.ParseFileEntry(effectInfoBuffer.Span, Zstd);
        }
    }

    private void ListAllFiles(IFileSystem fileSystem, string path)
    {
        Console.WriteLine($"Listing files in directory: {path}");
        UniqueRef<IDirectory> directory = new();
        using var dirPath = new global::LibHac.Fs.Path();
        byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(path);
        PathFunctions.SetUpFixedPath(ref dirPath.Ref(), pathBytes).ThrowIfFailure();

        Console.WriteLine("Attempting to open directory...");
        try
        {
            fileSystem.OpenDirectory(ref directory, in dirPath, OpenDirectoryMode.All).ThrowIfFailure();
            Console.WriteLine("Directory opened successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open directory: {ex.Message}");
            return;
        }

        var entries = new DirectoryEntry[10];
        while (true)
        {
            Console.WriteLine("Reading directory entries...");
            directory.Get.Read(out long entriesRead, entries).ThrowIfFailure();
            Console.WriteLine($"Entries read: {entriesRead}");
            if (entriesRead == 0) break;

            for (int i = 0; i < entriesRead; i++)
            {
                Console.WriteLine($"{entries[i].Type}: {entries[i].Name.ToString()}");
            }
        }

        Console.WriteLine("Finished listing files.");
        directory.Destroy();
    }

    public RentedBuffer<byte> GetVanilla(string relativeFilePath)
    {
        relativeFilePath = $"/{relativeFilePath}";
        
        Console.WriteLine($"Getting vanilla file: {relativeFilePath}");
        
        UniqueRef<IFile> file = new();
        _fileSystem.OpenFile(ref file, relativeFilePath.ToU8Span(), OpenMode.Read);

        if (!file.HasValue) {
            Console.WriteLine("File not found.");
            return default;
        }
        
        file.Get.GetSize(out long size);
        Console.WriteLine($"File size: {size}");
        RentedBuffer<byte> rawBuffer = RentedBuffer<byte>.Allocate((int)size);
        Span<byte> raw = rawBuffer.Span;
        file.Get.Read(out _, offset: 0, raw);
        file.Destroy();
        
        if (!TkZstd.IsCompressed(raw)) {
            Console.WriteLine("File is not compressed.");
            return rawBuffer;
        }

        try {
            RentedBuffer<byte> decompressed = RentedBuffer<byte>.Allocate(TkZstd.GetDecompressedSize(raw)); 
            Console.WriteLine("Decompressing file...");
            Zstd.Decompress(raw, decompressed.Span);
            Console.WriteLine("File decompressed successfully.");
            return decompressed;
        }
        finally {
            rawBuffer.Dispose();
        }
    }

    public bool IsVanilla(ReadOnlySpan<char> canonical, Span<byte> src, int fileVersion)
    {
        return _checksums.IsFileVanilla(canonical, src, fileVersion);
    }

    public void Dispose()
    {
        var localFsCopy = _localFs;
        localFsCopy.Destroy();
    }
}