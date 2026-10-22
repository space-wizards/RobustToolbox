using System.Globalization;

namespace Robust.Shared.Utility;

public static class CultureInfoExtension
{
    public static bool NameEquals(this CultureInfo cultureInfo, CultureInfo otherCultureInfo)
    {
        return cultureInfo.Name == otherCultureInfo.Name;
    }
}
