global using static TkSharp.Core.Common.TkLocalizationInterface;

namespace TkSharp.Core.Common;

public class TkLocalizationInterface
{
    public static Func<string, bool, string> GetLocale { get; set; } = (key, _) => key;
    
    public static Func<string, string> GetCultureName { get; set; } = culture => culture;
        
    public static readonly TkLocalizationInterface Locale = new();

    public string this[string key] => this[key, failSoftly: false];
    
    public string this[string key, params object[] arguments] => string.Format(this[key], arguments);

    public string this[string key, bool failSoftly] => GetLocale(key, failSoftly);
    
    public static string GetLocaleOrDefault(string localeName, string @default)
    {
        if (Locale[localeName, failSoftly: true] is not { } translated || translated == localeName) {
            return @default;
        }

        return translated;
    }
    
    public static string GetCultureNameFromLocale(string culture)
    {
        return GetCultureName(culture);
    }
}