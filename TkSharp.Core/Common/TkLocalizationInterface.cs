global using static TkSharp.Core.Common.TkLocalizationInterface;

namespace TkSharp.Core.Common;

public class TkLocalizationInterface
{
    public static Func<string, bool, string> GetLocale { get; set; } = (key, _) => key;
        
    public static readonly TkLocalizationInterface Locale = new();

    public string this[string key] => this[key, failSoftly: false];
    
    public string this[string key, params object[] arguments] => string.Format(this[key], arguments);

    public string this[string key, bool failSoftly] => GetLocale(key, failSoftly);
    
}