namespace TkSharp.Core;

[Flags]
public enum TkFileAttributes
{
    None = 0,
    HasZsExtension = 1,
    HasMcExtension = 2,
    IsProductFile = 4,
}