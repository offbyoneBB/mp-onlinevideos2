namespace InputStreamSourceFilter.Extensions
{
  public static class TimeStampExtension
  {
    /// <summary>
    /// Converts DirectShow hundreds nanoseconds to milliseconds.
    /// </summary>
    /// <param name="dsTime">DirectShow time</param>
    /// <returns>Milliseconds</returns>
    public static int ToMS(this long dsTime)
    {
      return (int)(dsTime / 10000);
    }

    /// <summary>
    /// Converts milliseconds to DirectShow hundreds nanoseconds.
    /// </summary>
    /// <param name="msTime">Milliseconds</param>
    /// <returns>DirectShow time</returns>
    public static long ToDS(this int msTime)
    {
      return (long)msTime * 10000;
    }
    /// <summary>
    /// Converts from DVD_TIME (10^7) to DirectShow hundreds nanoseconds (10^8).
    /// </summary>
    /// <param name="dvdTime">DVD_TIME</param>
    /// <returns>DirectShow time</returns>
    public static long ToDS(this double dvdTime)
    {
      return (long)dvdTime * 10;
    }

  }
}
