namespace TkSharp.Abstractions;

public interface ITkAddressTable
{
    string GetVersionedFileName(string canonicalFileName);
}