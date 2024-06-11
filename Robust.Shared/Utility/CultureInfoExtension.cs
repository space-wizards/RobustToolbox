using System.Globalization;

namespace Robust.Shared.Utility;

public static class CultureInfoExtension
{
    public static bool Equals(this CultureInfo cultureInfo, CultureInfo otherCultureInfo)
    {
        return cultureInfo.Name == otherCultureInfo.Name;
    }
}
