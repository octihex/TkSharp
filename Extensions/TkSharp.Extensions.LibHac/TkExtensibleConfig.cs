using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using LibHac.Common.Keys;
using TkSharp.Extensions.LibHac.Models;

namespace TkSharp.Extensions.LibHac;

internal enum TkExtensibleConfigType
{
    None,
    File,
    Folder,
    Path,
}

internal delegate bool TkConfigValidator<in T>(T input, KeySet keys, SwitchFsContainer? switchFsContainer);

internal struct TkExtensibleConfig<T>(TkExtensibleConfigType type, TkConfigValidator<T>? validator = null)
{
    private Func<T?>? _value;
    private readonly TkExtensibleConfigType _type = type;
    private readonly TkConfigValidator<T>? _validator = validator;

    public void Set(Func<T?>? value)
    {
        _value = value;
    }

    [Pure]
    public bool Get([MaybeNullWhen(false)] out T result) => Get(out result, keys: null, switchFsContainer: null);

    [Pure]
    public bool Get([MaybeNullWhen(false)] out T result, KeySet? keys, SwitchFsContainer? switchFsContainer)
    {
        if (_value is null) {
            result = default;
            return false;
        }
        
        result = _value();

        bool isValid = result is not null && _type switch {
            TkExtensibleConfigType.File when result is string filePath
                => File.Exists(filePath),
            TkExtensibleConfigType.Folder when result is string folderPath
                => Directory.Exists(folderPath),
            TkExtensibleConfigType.Path when result is string path
                => Path.Exists(path),

            _ => true
        };

        if (keys is null) {
            return isValid;
        }

        if (result is not null && _validator?.Invoke(result, keys, switchFsContainer) is false) {
            isValid = false;
        }

        return isValid;
    }
}