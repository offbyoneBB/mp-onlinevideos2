using System;
using System.Globalization;
using System.Linq;

namespace InputStreamSourceFilter.Extensions
{
  public static class CultureExtension
  {
    public static CultureInfo FromISOName(this string name)
    {
      return CultureInfo
          .GetCultures(CultureTypes.NeutralCultures)
          .FirstOrDefault(c => string.Equals(c.ThreeLetterISOLanguageName, name, StringComparison.OrdinalIgnoreCase));
    }

    public static int TryGetLCID(this string name)
    {
      var culture = FromISOName(name);
      return culture != null ? culture.LCID : 0;
    }
  }
}
