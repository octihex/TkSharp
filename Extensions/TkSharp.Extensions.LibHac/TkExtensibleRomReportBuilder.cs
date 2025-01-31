using LibHac.Common.Keys;

namespace TkSharp.Extensions.LibHac;

internal class TkExtensibleRomReportBuilder
{
    public KeySet? Keys { get; set; }

    public bool HasBaseGame { get; set; }
    public Dictionary<string, bool> BaseGameSources { get; } = [];
    
    public bool HasUpdate { get; set; }
    public Dictionary<string, bool> UpdateSources { get; } = [];
    
    private TkExtensibleRomReportBuilder()
    {
    }

    public static TkExtensibleRomReportBuilder Create()
    {
        return new TkExtensibleRomReportBuilder();
    }

    public TkExtensibleRomReport Build()
    {
        return new TkExtensibleRomReport(Keys, HasBaseGame, BaseGameSources, HasUpdate, UpdateSources);
    }

    /// <summary>
    /// Sets <see cref="Keys"/> if <paramref name="keys"/> is not null.
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public void SetKeys(KeySet? keys)
    {
        if (keys is not null) {
            Keys = keys;
        }
    }

    /// <summary>
    /// Sets <see cref="HasBaseGame"/> if it is not already true.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public void SetHasBaseGame(bool value, string source)
    {
        BaseGameSources[source] = value;
        
        if (!HasBaseGame) HasBaseGame = value;
    }

    /// <summary>
    /// Sets <see cref="HasUpdate"/> if it is not already true.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public void SetHasUpdate(bool value, string source)
    {
        UpdateSources[source] = value;
        
        if (!HasUpdate) HasUpdate = value;
    }
}