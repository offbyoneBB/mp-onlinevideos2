using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace OnlineVideos.WebService
{
    /// <summary>
    /// Simple client for interacting with the OnlineVideos web service.
    /// </summary>
    public class OnlineVideosService
    {
        // ToDo: Update this to the actual url when published
        private const string DEFAULT_BASE_URL = "https://localhost:7259";
        private const string API_PATH = "/api/OnlineVideos";

        public OnlineVideosService(string url)
        {
            Url = url;
        }

        public OnlineVideosService()
        {
            Url = DEFAULT_BASE_URL + API_PATH;
        }

        public int Timeout { get; set; } = CompressionWebClient.DEFAULT_REQUEST_TIMEOUT;
        public bool EnableDecompression { get; set; }
        public ICredentials Credentials { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string Url { get; set; }

        public bool RegisterEmail(string email, out string message)
        {
            return TryGet("RegisterEmail", out message, "email", email);
        }

        public bool SubmitSite(string email, string password, string siteXml, byte[] icon, byte[] banner, string requiredDll, out string infoMessage)
        {
            SubmitSiteDto dto = new SubmitSiteDto
            {
                Email = email,
                Password = password,
                SiteXml = siteXml,
                Icon = icon,
                Banner = banner,
                RequiredDll = requiredDll
            };
            return TryPost("SubmitSite", dto, out infoMessage);
        }

        public bool SubmitDll(string email, string password, string name, byte[] data, out string infoMessage)
        {
            SubmitDllDto dto = new SubmitDllDto
            {
                Email = email,
                Password = password,
                Name = name,
                Data = data
            };
            return TryPost("SubmitDll", dto, out infoMessage);
        }

        public bool SubmitReport(string siteName, string message, ReportType type, out string infoMessage)
        {
            SubmitReportDto dto = new SubmitReportDto
            {
                SiteName = siteName,
                Message = message,
                Type = type
            };
            return TryPost("SubmitReport", dto, out infoMessage);
        }

        public Report[] GetReports(string siteName)
        {
            return Get<Report[]>("GetReports", "siteName", siteName);
        }

        public Site[] GetSitesOverview()
        {
            return Get<Site[]>("GetSitesOverview");
        }

        public Dll[] GetDllsOverview()
        {
            return Get<Dll[]>("GetDllsOverview");
        }

        public string GetDllOwner(string dllName, out string md5)
        {
            var dllOwner = Get<GetDllOwnerDto>("GetDllOwner", "dllName", dllName);
            md5 = dllOwner.MD5;
            return dllOwner.OwnerId;
        }

        public string GetSiteXml(string siteName)
        {
            return TryGet("GetSiteXml", out string xml, "siteName", siteName) ? xml : string.Empty;
        }

        public byte[] GetSiteIcon(string siteName)
        {
            return Get<byte[]>("GetSiteIcon", "siteName", siteName);
        }

        public byte[] GetSiteIconIfChanged(string siteName, string md5)
        {
            return Get<byte[]>("GetSiteIconIfChanged", "siteName", siteName, "md5", md5);
        }

        public byte[] GetSiteBanner(string siteName)
        {
            return Get<byte[]>("GetSiteBanner", "siteName", siteName);
        }

        public byte[] GetSiteBannerIfChanged(string siteName, string md5)
        {
            return Get<byte[]>("GetSiteBannerIfChanged", "siteName", siteName, "md5", md5);
        }

        public byte[] GetDll(string name)
        {
            return Get<byte[]>("GetDll", "name", name);
        }

        private bool TryGet(string action, out string response, params string[] query)
        {
            try
            {
                using (WebClient webClient = BuildWebClient(BuildQueryString(query)))
                    response = webClient.DownloadString(action);
                return true;
            }
            catch (WebException ex)
            {
                // Handle any expected error status codes and return the
                // relevant user readable info messages returned by the server
                if (HandleException(ex, out response))
                    // If an exception has been thrown then the operation was not
                    // successful regardless of whether the exception was handled
                    return false;
                // Unexpected exception, rethrow
                throw;
            }
        }

        private T Get<T>(string action, params string[] query)
        {
            if (!TryGet(action, out string response, query))
                return default;
            return JsonConvert.DeserializeObject<T>(response);
        }

        private bool TryPost(string action, object post, out string response)
        {
            try
            {
                using (WebClient webClient = BuildWebClient())
                {
                    webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                    response = webClient.UploadString(action, JsonConvert.SerializeObject(post));
                }
                return true;
            }
            catch (WebException ex)
            {
                // Handle any expected error status codes and return the
                // relevant user readable info messages returned by the server
                if (HandleException(ex, out response))
                    // If an exception has been thrown then the operation was not
                    // successful regardless of whether the exception was handled
                    return false;
                // Unexpected exception, rethrow
                throw;
            }
        }

        private bool HandleException(WebException ex, out string response)
        {
            response = null;
            HttpWebResponse webResponse = null;
            try
            {
                webResponse = (HttpWebResponse)ex.Response;
                // The API will return a user readable message with specific details for these status codes so try and read and return it
                if (webResponse.StatusCode == HttpStatusCode.Unauthorized || webResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    using (var responseStream = webResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream))
                        response = reader.ReadToEnd();
                    return true;
                }
                // The API may return this during normal operation, e.g. when requesting an image for a site that doesn't have one so
                // handle it here and don't rethrow
                else if (webResponse.StatusCode == HttpStatusCode.NotFound)
                    return true;

            }
            finally
            {
                webResponse?.Close();
            }
            return false;
        }

        protected NameValueCollection BuildQueryString(params string[] query)
        {
            NameValueCollection queryString = new NameValueCollection();
            if (query == null || query.Length == 0)
                return queryString;
            // The query string must have key value pairs so a multiple of 2 is always expected
            if (query.Length % 2 != 0)
                throw new ArgumentException("Query length is not a multiple of 2", nameof(query));

            for (int i = 0; i < query.Length; i += 2)
                queryString.Add(query[i], query[i + 1]);

            return queryString;
        }

        protected WebClient BuildWebClient(NameValueCollection queryString = null)
        {
            return new CompressionWebClient(EnableDecompression)
            {
                BaseAddress = Url.TrimEnd('/') + "/",
                QueryString = queryString,
                RequestTimeout = Timeout,
                Credentials = Credentials,
                UseDefaultCredentials = UseDefaultCredentials
            };
        }
    }

    // DTOs copied from the server, these will need to be manually
    // kept up to date with any changes to the web service code

    public class SubmitSiteDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string SiteXml { get; set; }
        public byte[] Icon { get; set; }
        public byte[] Banner { get; set; }
        public string RequiredDll { get; set; }
    }

    public class SubmitDllDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
    }

    public enum ReportType : byte { Suggestion, Broken, ConfirmedBroken, RejectedBroken, Fixed };

    public class SubmitReportDto
    {
        public string SiteName { get; set; }
        public string Message { get; set; }
        public ReportType Type { get; set; }
    }

    public class Report
    {
        public string SiteName { get; set; }
        public DateTime Date { get; set; }
        public ReportType Type { get; set; }
        public string Message { get; set; }
    }

    public enum SiteState : byte { Working, Reported, Broken };

    public class Site
    {
        public string Name { get; set; }
        public SiteState State { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public bool IsAdult { get; set; }
        public string RequiredDll { get; set; }
        public string OwnerId { get; set; }
    }

    public class Dll
    {
        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }
        public string OwnerId { get; set; }
        public string MD5 { get; set; }
    }

    public class GetDllOwnerDto
    {
        public string OwnerId { get; set; }
        public string MD5 { get; set; }
    }
}
