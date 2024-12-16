using System;
using System.Globalization;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    public static class ScSystemLanguageExtender
    {

        public static string ToTwoLettersCountry(this SystemLanguage lang)
        {
            return lang switch
            {
            SystemLanguage.Afrikaans => "ZA",
            SystemLanguage.Arabic => "SA",
            SystemLanguage.Basque => "ES",
            SystemLanguage.Belarusian => "BY",
            SystemLanguage.Bulgarian => "BG",
            SystemLanguage.Catalan => "ES",
            SystemLanguage.Chinese => "CN",
            SystemLanguage.Czech => "CZ",
            SystemLanguage.Danish => "DK",
            SystemLanguage.Dutch => "NL",
            SystemLanguage.English => "US",
            SystemLanguage.Estonian => "EE",
            SystemLanguage.Faroese => "FO",
            SystemLanguage.Finnish => "FI",
            SystemLanguage.French => "FR",
            SystemLanguage.German => "DE",
            SystemLanguage.Greek => "GR",
            SystemLanguage.Hebrew => "IL",
            SystemLanguage.Hungarian => "HU",
            SystemLanguage.Icelandic => "IS",
            SystemLanguage.Indonesian => "ID",
            SystemLanguage.Italian => "IT",
            SystemLanguage.Japanese => "JP",
            SystemLanguage.Korean => "KR",
            SystemLanguage.Latvian => "LV",
            SystemLanguage.Lithuanian => "LT",
            SystemLanguage.Norwegian => "NO",
            SystemLanguage.Polish => "PL",
            SystemLanguage.Portuguese => "PT",
            SystemLanguage.Romanian => "RO",
            SystemLanguage.Russian => "RU",
            SystemLanguage.SerboCroatian => "RS",
            SystemLanguage.Slovak => "SK",
            SystemLanguage.Slovenian => "SI",
            SystemLanguage.Spanish => "ES",
            SystemLanguage.Swedish => "SE",
            SystemLanguage.Thai => "TH",
            SystemLanguage.Turkish => "TR",
            SystemLanguage.Ukrainian => "UA",
            SystemLanguage.Vietnamese => "VN",
            SystemLanguage.ChineseSimplified => "CN",
            SystemLanguage.ChineseTraditional => "TW",
            SystemLanguage.Hindi => "IN",
            SystemLanguage.Unknown => "UN",
            _ => throw new ArgumentOutOfRangeException(nameof(lang), lang, null)
            };
        }
        public static CultureInfo ToCultureInfo(this SystemLanguage lang)
        {
            return lang switch
            {
                SystemLanguage.Afrikaans => new CultureInfo("af"),
                SystemLanguage.Arabic => new CultureInfo("ar"),
                SystemLanguage.Basque => new CultureInfo("eu"),
                SystemLanguage.Belarusian => new CultureInfo("be"),
                SystemLanguage.Bulgarian => new CultureInfo("bg"),
                SystemLanguage.Catalan => new CultureInfo("ca"),
                SystemLanguage.Chinese => new CultureInfo("zh"),
                SystemLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
                SystemLanguage.ChineseTraditional => new CultureInfo("zh-TW"),
                SystemLanguage.Czech => new CultureInfo("cs"),
                SystemLanguage.Danish => new CultureInfo("da"),
                SystemLanguage.Dutch => new CultureInfo("nl"),
                SystemLanguage.English => new CultureInfo("en"),
                SystemLanguage.Estonian => new CultureInfo("et"),
                SystemLanguage.Finnish => new CultureInfo("fi"),
                SystemLanguage.French => new CultureInfo("fr"),
                SystemLanguage.German => new CultureInfo("de"),
                SystemLanguage.Greek => new CultureInfo("el"),
                SystemLanguage.Hebrew => new CultureInfo("he"),
                SystemLanguage.Hungarian => new CultureInfo("hu"),
                SystemLanguage.Icelandic => new CultureInfo("is"),
                SystemLanguage.Indonesian => new CultureInfo("id"),
                SystemLanguage.Italian => new CultureInfo("it"),
                SystemLanguage.Japanese => new CultureInfo("ja"),
                SystemLanguage.Korean => new CultureInfo("ko"),
                SystemLanguage.Latvian => new CultureInfo("lv"),
                SystemLanguage.Lithuanian => new CultureInfo("lt"),
                SystemLanguage.Norwegian => new CultureInfo("no"),
                SystemLanguage.Polish => new CultureInfo("pl"),
                SystemLanguage.Portuguese => new CultureInfo("pt"),
                SystemLanguage.Romanian => new CultureInfo("ro"),
                SystemLanguage.Russian => new CultureInfo("ru"),
                SystemLanguage.SerboCroatian => new CultureInfo("sr"),
                SystemLanguage.Slovak => new CultureInfo("sk"),
                SystemLanguage.Slovenian => new CultureInfo("sl"),
                SystemLanguage.Spanish => new CultureInfo("es"),
                SystemLanguage.Swedish => new CultureInfo("sv"),
                SystemLanguage.Thai => new CultureInfo("th"),
                SystemLanguage.Turkish => new CultureInfo("tr"),
                SystemLanguage.Ukrainian => new CultureInfo("uk"),
                SystemLanguage.Vietnamese => new CultureInfo("vi"),
                SystemLanguage.Hindi => new CultureInfo("hi"),
                SystemLanguage.Faroese => new CultureInfo("fo"),
                SystemLanguage.Unknown => null,
                _ => null
            };
        }
    }
}