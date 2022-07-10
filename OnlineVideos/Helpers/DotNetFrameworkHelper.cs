using System;
using System.Reflection;

namespace OnlineVideos.Helpers
{
    public static class DotNetFrameworkHelper
    {
#if !NET6_0_OR_GREATER
        /// <summary>
        /// Method to change the AllowUnsafeHeaderParsing property of HttpWebRequest.
        /// </summary>
        /// <param name="setState"></param>
        /// <returns></returns>
        public static bool SetAllowUnsafeHeaderParsing(bool setState)
        {
            try
            {
                //Get the assembly that contains the internal class
                Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
                if (aNetAssembly == null)
                    return false;

                //Use the assembly in order to get the internal type for the internal class
                Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (aSettingsType == null)
                    return false;

                //Use the internal static property to get an instance of the internal settings class.
                //If the static instance isn't created allready the property will create it for us.
                object anInstance = aSettingsType.InvokeMember("Section",
                                                                BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic,
                                                                null, null, new object[] { });
                if (anInstance == null)
                    return false;

                //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                if (aUseUnsafeHeaderParsing == null)
                    return false;

                // and finally set our setting
                aUseUnsafeHeaderParsing.SetValue(anInstance, setState);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Unsafe header parsing setting change failed: " + ex.ToString());
                return false;
            }
        }
#endif

        /// <summary>
        /// Workaround for .net issue documented here: https://connect.microsoft.com/VisualStudio/feedback/details/386695/system-uri-incorrectly-strips-trailing-dots
        /// </summary>
        /// <remarks>
        /// Workaround sets static flag in the UriParser and will affect all System.Uri classes created afterwards - APPLICATION wise (not just OnlineVideos)
        /// </remarks>
        public static void FixUriTrailingDots()
        {
            MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            FieldInfo flagsField = typeof(UriParser).GetField("m_Flags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (getSyntax != null && flagsField != null)
            {
                foreach (string scheme in new[] { "http", "https" })
                {
                    UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
                    if (parser != null)
                    {
                        int flagsValue = (int)flagsField.GetValue(parser);
                        // Clear the CanonicalizeAsFilePath attribute
                        if ((flagsValue & 0x1000000) != 0)
                        {
                            flagsField.SetValue(parser, flagsValue & ~0x1000000);
                        }
                    }
                }
            }
        }

        public static class UriWithoutUrlDecoding
        {
            //Default .NET Uri automatically decodes url's so a request to http://.../a%2Fb is impossible. in this case a request to http://.../a/b is made.
            //It's possible in .NET 4.0 to change a configuration parameter to disable this, but that changes the behaviour of all Uri's
            //This is a workaround copied from http://blogs.msdn.com/b/xiangfan/archive/2012/01/16/10256915.aspx

            private const GenericUriParserOptions c_Options =
                GenericUriParserOptions.Default |
                GenericUriParserOptions.DontUnescapePathDotsAndSlashes |
                GenericUriParserOptions.Idn |
                GenericUriParserOptions.IriParsing;
            private static readonly GenericUriParser s_SyntaxHttp = new GenericUriParser(c_Options);
            private static readonly GenericUriParser s_SyntaxHttps = new GenericUriParser(c_Options);

            static UriWithoutUrlDecoding()
            {
                // Initialize the scheme
                FieldInfo fieldInfoSchemeName = typeof(UriParser).GetField("m_Scheme", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfoSchemeName == null)
                {
                    throw new MissingFieldException("'m_Scheme' field not found");
                }
                fieldInfoSchemeName.SetValue(s_SyntaxHttp, "http");
                fieldInfoSchemeName.SetValue(s_SyntaxHttps, "https");

                FieldInfo fieldInfoPort = typeof(UriParser).GetField("m_Port", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfoPort == null)
                {
                    throw new MissingFieldException("'m_Port' field not found");
                }
                fieldInfoPort.SetValue(s_SyntaxHttp, 80);
                fieldInfoPort.SetValue(s_SyntaxHttps, 443);
            }

            public static Uri Create(string url)
            {
                Uri result = new Uri(url);
                if (url.IndexOf("%2F", StringComparison.OrdinalIgnoreCase) != -1)
                    FixUri(result);
                return result;
            }

            public static Uri Create(string baseUrl, string relativeUrl)
            {
                Uri result = new Uri(new Uri(baseUrl), relativeUrl);
                if (baseUrl.IndexOf("%2F", StringComparison.OrdinalIgnoreCase) != -1 || relativeUrl.IndexOf("%2F", StringComparison.OrdinalIgnoreCase) != -1)
                    FixUri(result);
                return result;
            }

            private static void FixUri(Uri uri)
            {
                UriParser parser = null;
                switch (uri.Scheme.ToLowerInvariant())
                {
                    case "http":
                        parser = s_SyntaxHttp;
                        break;
                    case "https":
                        parser = s_SyntaxHttps;
                        break;
                }

                if (parser != null)
                {
                    // Associate the parser
                    FieldInfo fieldInfo = typeof(Uri).GetField("m_Syntax", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fieldInfo == null)
                    {
                        throw new MissingFieldException("'m_Syntax' field not found");
                    }
                    fieldInfo.SetValue(uri, parser);
                }
            }
        }

    }
}
