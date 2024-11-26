namespace TkSharp.Core;

public interface ITkModReaderProvider
{
    ITkModReader? GetReader(object input);

    bool CanRead(object input);
}