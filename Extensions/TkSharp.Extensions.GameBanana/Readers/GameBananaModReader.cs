using System.Runtime.CompilerServices;
using ReverseMarkdown;
using TkSharp.Core;
using TkSharp.Core.Models;
using TkSharp.Extensions.GameBanana.Helpers;

namespace TkSharp.Extensions.GameBanana.Readers;

public sealed class GameBananaModReader(ITkModReaderProvider readerProvider) : ITkModReader
{
    private readonly ITkModReaderProvider _readerProvider = readerProvider;

    public async ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext context = default, CancellationToken ct = default)
    {
        if (input is GameBananaFile file) {
            return await ParseFromFileUrl(file.DownloadUrl, file.Id, file, ct);
        }
        
        if (input is not string arg) {
            return null;
        }

        if (!GbUrlHelper.TryGetId(arg, out long id)) {
            return null;
        }

        if (arg.Contains("/mods/")) {
            return await ParseFromModId(id, ct);
        }

        return await ParseFromFileUrl(arg, id, ct: ct);
    }

    public bool IsKnownInput(object? input)
    {
        return input is GameBananaFile
               || (input is string arg && (
                   arg.Contains("gamebanana.com/mods/") || arg.Contains("gamebanana.com/dl/")
               ) && GbUrlHelper.TryGetId(arg, out _));
    }

    public async ValueTask<TkMod?> ParseFromModId(long modId, CancellationToken ct)
    {
        GameBananaMod? gbMod = await GameBanana.GetMod(modId, ct);
        GameBananaFile? target = gbMod?.Files
            .FirstOrDefault(file => file.IsTkcl);

        target ??= gbMod?.Files
            .FirstOrDefault(file => _readerProvider.CanRead(file.Name));

        if (target is null || gbMod is null) {
            return null;
        }

        TkMod? mod = await ParseFromFileUrl(target.DownloadUrl, target.Id, target, ct);

        if (mod is null) {
            return null;
        }

        mod.Name = gbMod.Name;
        mod.Author = gbMod.Submitter.Name;
        mod.Description = new Converter(new Config {
            GithubFlavored = true,
            ListBulletChar = '*',
            UnknownTags = Config.UnknownTagsOption.Bypass
        }).Convert(gbMod.Text);
        mod.Thumbnail = new TkThumbnail();
        mod.Version = gbMod.Version;

        foreach (GameBananaAuthor author in gbMod.Credits.SelectMany(group => group.Authors)) {
            mod.Contributors.Add(new TkModContributor(author.Name, author.Role));
        }

        return mod;
    }

    public async ValueTask<TkMod?> ParseFromFileUrl(string fileUrl, long fileId, GameBananaFile? target = default, CancellationToken ct = default)
    {
        target ??= await GameBanana.Get<GameBananaFile>($"File/{fileId}", GameBananaModJsonContext.Default.GameBananaFile, ct);
        
        if (target is null) {
            return null;
        }

        ITkModReader? reader = _readerProvider.GetReader(target.Name);
        Ulid id = Unsafe.As<long, Ulid>(ref fileId);
        
        byte[] data = await DownloadHelper.DownloadAndVerify(
            fileUrl, Convert.FromHexString(target.Checksum), ct: ct);

        await using MemoryStream ms = new(data);
        return reader?.ReadMod(target.Name, ms, new TkModContext(id), ct) switch {
            { } result => await result,
            _ => null
        };
    }
}