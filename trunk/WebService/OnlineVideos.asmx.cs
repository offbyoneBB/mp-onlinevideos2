using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mail;
using System.Web;
using System.Web.Services;
using System.Xml;
using OnlineVideos.WebService.Database;

namespace OnlineVideos.WebService
{
    public enum SiteState : byte { Working, Reported, Broken };
    public enum ReportType : byte { Suggestion, Broken, ConfirmedBroken, RejectedBroken, Fixed };

    /// <summary>
    /// Zusammenfassungsbeschreibung für OnlineVideos WebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class OnlineVideosService : System.Web.Services.WebService
    {        
        [WebMethod]
        public bool RegisterEmail(string email, out string infoMessage)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    User user = null;
                    if (dc.User.Any(u => u.Email == email))
                    {
                        user = dc.User.First(u => u.Email == email);
                    }
                    else
                    {
                        user = new User() { Email = email, IsAdmin = false, Password = RandomString(6) };
                        dc.User.InsertOnSubmit(user);
                    }
                    if (SendPasswordEmail(user))
                    {
                        infoMessage = "Password was sent to your Email address.";
                        dc.SubmitChanges();
                        return true;
                    }
                    else
                    {
                        infoMessage = "Password could not be sent to your Email address.";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                infoMessage = ex.Message;
                return false;
            }
        }

        [WebMethod]
        public bool SubmitSite(string email, string password, string siteXml, byte[] icon, byte[] banner, string requiredDll, out string infoMessage)
        {
			DateTime updateTime = DateTime.Now.ToUniversalTime(); // use universal Time when setting the current TimeStamp to the Database
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);

            // is the given site xml at least valid xml?
            XmlDocument xml = new XmlDocument();
            string siteName = "";
            string lang = "";
            string desc = "";
            bool isAdult = false;
            try
            {
                xml.LoadXml(siteXml);
                if (xml.SelectNodes("//Site").Count != 1)
                {
                    infoMessage = "Exactly one <Site> node expected!";
                    return false;
                }
                siteName = xml.SelectSingleNode("//Site/@name").Value;
                lang = xml.SelectSingleNode("//Site/@lang").Value;
                desc = xml.SelectSingleNode("//Site/Description").InnerText;
                XmlNode n = xml.SelectSingleNode("//Site/@agecheck");
                if (n != null) isAdult = bool.Parse(n.Value);
				// write the current server time into the xml as lastUpdated, so comparing it later with the DB field will always work on any client 
				// (the submitting client might have wrong time settings)
				XmlNode lastUpdatedNode = xml.SelectSingleNode("//Site/@lastUpdated");
				if (lastUpdatedNode == null)
				{
					lastUpdatedNode = (xml.SelectSingleNode("//Site") as XmlElement).Attributes.Append(xml.CreateAttribute("lastUpdated"));
				}
				(lastUpdatedNode as XmlAttribute).Value = updateTime.ToString("o"); 
            }
            catch
            {
                infoMessage = "Invalid xml for site!";
                return false;
            }
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    // does the user exist?
                    if (!dc.User.Any(u => u.Email == email))
                    {
                        infoMessage = "Email not registered!";
                        return false;
                    }
                    User user = dc.User.First(u => u.Email == email);
                    // is the correct password given?
                    if (user.Password != password)
                    {
                        infoMessage = "Wrong password!";
                        return false;
                    }
                    // is the requiredDll available on the server?
                    if (!string.IsNullOrEmpty(requiredDll))
                    {
                        if (!dc.Dll.Any(d => d.Name == requiredDll))
                        {
                            infoMessage = "Required Dll not found on Server!";
                            return false;
                        }
                    }
                    // does the site already exist?
                    if (dc.Site.Any(s => s.Name == siteName))
                    {
                        // need to update                        
                        Site site = dc.Site.First(s => s.Name == siteName);
                        if (site.Owner.Email == user.Email || user.IsAdmin)
                        {
                            site.Language = lang;
                            site.Description = desc;
                            site.IsAdult = isAdult;
							site.LastUpdated = updateTime;
                            if (site.XML != siteXml) site.XML = siteXml;
                            if (site.RequiredDll != requiredDll) site.RequiredDll = requiredDll;
                            site.State = SiteState.Working;
                            dc.SubmitChanges();
                            infoMessage = "Site successfully updated!";
                            infoMessage += SaveImages(siteName, icon, banner);
                            return true;
                        }
                        else
                        {
                            infoMessage = "Only the owner or an admin can update existing sites!";
                            return false;
                        }
                    }
                    else
                    {
                        // insert new site
                        Site site = new Site()
                        {
                            Name = siteName,
                            Language = lang,
                            Description = desc,
                            IsAdult = isAdult,
							LastUpdated = updateTime,
                            XML = siteXml,
                            RequiredDll = requiredDll,
                            Owner = user,
                            State = SiteState.Working
                        };
                        dc.Site.InsertOnSubmit(site);
                        dc.SubmitChanges();
                        infoMessage = "Site successfully added!";
                        infoMessage += SaveImages(siteName, icon, banner);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                infoMessage = ex.Message;
                return false;
            }
        }

        [WebMethod]
        public bool SubmitDll(string email, string password, string name, byte[] data, out string infoMessage)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    // does the user exist?
                    if (!dc.User.Any(u => u.Email == email))
                    {
                        infoMessage = "Email not registered!";
                        return false;
                    }
                    User user = dc.User.First(u => u.Email == email);
                    // is the correct password given?
                    if (user.Password != password)
                    {
                        infoMessage = "Wrong password!";
                        return false;
                    }
                    // write the file and calc its MD5 hash
                    System.IO.File.WriteAllBytes(Server.MapPath("~/Dlls/") + name + ".dll", data);
					string md5String = GetMD5Hash(data);
                    // does the dll already exist?
                    if (dc.Dll.Any(d => d.Name == name))
                    {
                        // need to update                        
                        Dll dll = dc.Dll.First(d => d.Name == name);
                        if (dll.Owner.Email == user.Email || user.IsAdmin)
                        {
                            dll.MD5 = md5String;
                            dll.LastUpdated = DateTime.Now;
                            dc.SubmitChanges();
                            infoMessage = "Dll successfully updated!";
                            return true;
                        }
                        else
                        {
                            infoMessage = "Only the owner or an admin can update existing dlls!";
                            return false;
                        }
                    }
                    else
                    {
                        // insert new dll
                        Dll newDll = new Dll()
                        {
                            MD5 = md5String,
                            LastUpdated = DateTime.Now,
                            Name = name,
                            Owner = user
                        };                        
                        dc.Dll.InsertOnSubmit(newDll);
                        dc.SubmitChanges();
                        infoMessage = "Dll successfully added!";
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                infoMessage = ex.Message;
                return false;
            }
        }

        [WebMethod]
        public bool SubmitReport(string siteName, string message, ReportType type, out string infoMessage)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {                    
                    // does the site exsist?
                    if (!dc.Site.Any(s => s.Name == siteName))
                    {
                        infoMessage = "Site not found!";
                        return false;
                    }
                    Site site = dc.Site.First(s => s.Name == siteName);
                    Report report = new Report()
                    {
                        Date = DateTime.Now,
                        Message = message,
                        Type = type,
                        Site = site
                    };
                    if (type == ReportType.Broken && site.State == SiteState.Working) site.State = SiteState.Reported;
                    else if (type == ReportType.ConfirmedBroken) site.State = SiteState.Broken;
                    else if (type == ReportType.RejectedBroken || type == ReportType.Fixed) site.State = SiteState.Working;
                    dc.Report.InsertOnSubmit(report);
                    dc.SubmitChanges();
                    SendNewReportEmail(report);
                    infoMessage = "Report successfully submitted!";
                    return true;
                }
            }
            catch (Exception ex)
            {
                infoMessage = ex.Message;
                return false;
            }
        }

        [WebMethod]
        public List<Report> GetReports(string siteName)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {                    
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    var r = from a in dc.Report where a.Site.Name == siteName select new { Date = a.Date, Type = a.Type, Message = a.Message };
                    return (List<Report>)r.ToList().ToNonAnonymousList(typeof(Report));
                }
            }
            catch
            {
                return new List<Report>();
            }
        }

        [WebMethod]
        public List<Site> GetSitesOverview()
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    var s = from a in dc.Site select new { Description = a.Description, Language = a.Language, IsAdult = a.IsAdult, LastUpdated = a.LastUpdated, Name = a.Name, State = a.State, Owner_FK = a.Owner_FK, RequiredDll = a.RequiredDll };
                    return (List<Site>)s.ToList().ToNonAnonymousList(typeof(Site));
                }
            }
            catch 
            {
                return new List<Site>();
            }
        }

        [WebMethod]
        public List<Dll> GetDllsOverview()
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    var s = from a in dc.Dll select new { Name = a.Name, LastUpdated = a.LastUpdated, Owner_FK = a.Owner_FK, MD5 = a.MD5 };
                    return (List<Dll>)s.ToList().ToNonAnonymousList(typeof(Dll));
                }
            }
            catch
            {
                return new List<Dll>();
            }
        }

        [WebMethod]
        public string GetDllOwner(string dllName, out string MD5)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            MD5 = null;
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {                    
                    var result = dc.Dll.Where(d => d.Name == dllName);
                    if (result.Any())
                    {
                        MD5 = result.First().MD5;
                        return result.First().Owner_FK;
                    }
                    else return null;
                }
            }
            catch
            {
                return null;
            }
        }

        [WebMethod]
        public string GetSiteXml(string siteName)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                using (OnlineVideosDataContext dc = new OnlineVideosDataContext())
                {
                    if (!dc.DatabaseExists()) { dc.CreateDatabase(); dc.SubmitChanges(); }
                    if (dc.Site.Any(s => s.Name == siteName))
                    {
                        Site site = dc.Site.First(s => s.Name == siteName);
                        return site.XML;
                    }
                }
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return "";
        }

        [WebMethod]
        public byte[] GetSiteIcon(string siteName)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                return System.IO.File.ReadAllBytes(Server.MapPath("~/Icons/") + siteName + ".png");
            }
            catch
            {
                return null;
            }
        }

		[WebMethod(Description="Returns the Icon for a Site as byte array, when MD5 Hash of file on server is different to given parameter.")]
		public byte[] GetSiteIconIfChanged(string siteName, string md5)
		{
			NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
			try
			{
				byte[] bytes = System.IO.File.ReadAllBytes(Server.MapPath("~/Icons/") + siteName + ".png");
				if (!string.IsNullOrEmpty(md5) && GetMD5Hash(bytes) == md5) return null;
				return bytes;
			}
			catch
			{
				return null;
			}
		}

        [WebMethod]
        public byte[] GetSiteBanner(string siteName)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                return System.IO.File.ReadAllBytes(Server.MapPath("~/Banners/") + siteName + ".png");
            }
            catch
            {
                return null;
            }
        }

		[WebMethod(Description="Returns the Banner for a Site as byte array, when MD5 Hash of file on server is different to given parameter.")]
        public byte[] GetSiteBannerIfChanged(string siteName, string md5)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(Server.MapPath("~/Banners/") + siteName + ".png");
				if (!string.IsNullOrEmpty(md5) && GetMD5Hash(bytes) == md5) return null;
				return bytes;
            }
            catch
            {
                return null;
            }
        }

        [WebMethod]
        public byte[] GetDll(string name)
        {
            NLog.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().Name).Info(Context.Request.UserHostName);
            try
            {
                return System.IO.File.ReadAllBytes(Server.MapPath("~/Dlls/") + name + ".dll");
            }
            catch
            {
                return null;
            }
        }

        #region Helper

        static readonly char[] RandomChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        /// <summary>
        /// Generates a random string with the given length
        /// </summary>
        /// <param name="size">Size of the string</param>
        /// <returns>Random string</returns>
        static string RandomString(int size)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            Random random = new Random();
            for (int i = 0; i < size; i++)
            {
                builder.Append(RandomChars[random.Next(0, RandomChars.Length)]);
            }
            return builder.ToString();
        }

        static bool SendPasswordEmail(User user)
        {
            try
            {
                MailMessage m = new MailMessage();
                m.To = user.Email;
                m.From = "offbyone@offbyone.de";
                m.Subject = "Thank you for registering with MediaPortal OnlineVideos Plugin.";
                m.Body = "Your registered password is: " + user.Password;
                SmtpMail.Send(m);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool SendNewReportEmail(Report report)
        {
            try
            {
                MailMessage m = new MailMessage();
                m.To = report.Site.Owner.Email;
                m.From = "offbyone@offbyone.de";
                m.Subject = string.Format("OnlineVideos: New Report ({0}) for {1}", report.Type.ToString(), report.Site.Name);
                m.Body = report.Message;
                SmtpMail.Send(m);
                return true;
            }
            catch
            {
                return false;
            }
        }

        string SaveImages(string siteName, byte[] icon, byte[] banner)
        {
            string message = "";
            if (icon != null && icon.Length > 0)
            {
                try
                {
                    System.Drawing.Image iconImage = System.Drawing.Image.FromStream(new System.IO.MemoryStream(icon));
                    if (iconImage.Width == iconImage.Height)
                    {
                        if (iconImage.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Png.Guid)
                        {
                            System.IO.File.WriteAllBytes(Server.MapPath("~/Icons/") + siteName + ".png", icon);
                            message += "Icon saved!";
                        }
                        else
                        {
                            message += "Icon not saved! Must be PNG!";
                        }
                    }
                    else
                    {
                        message += "Icon not saved! Height must be equal to width!";
                    }
                }
                catch
                {
                    message += "Icon invalid!";
                }
                message += " ";
            }
            if (banner != null && banner.Length > 0)
            {
                try
                {
                    System.Drawing.Image bannerImage = System.Drawing.Image.FromStream(new System.IO.MemoryStream(banner));
                    if (bannerImage.Width == 3 * bannerImage.Height)
                    {
                        if (bannerImage.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Png.Guid)
                        {
                            System.IO.File.WriteAllBytes(Server.MapPath("~/Banners/") + siteName + ".png", banner);                            
                            message += "Banner saved!";
                        }
                        else
                        {
                            message += "Banner not saved! Must be PNG!";
                        }
                    }
                    else
                    {
                        message += "Banner not saved! Width must be 3 times height!";
                    }
                }
                catch
                {
                    message += "Banner invalid!";
                }
            }            
            return message;
        }

		string GetMD5Hash(byte[] data)
		{
			System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] md5Hash = md5.ComputeHash(data);
			string md5String = BitConverter.ToString(md5Hash).Replace("-", "").ToLower();
			return md5String;
		}

        #endregion
    }

    public static class Extensions
    {
        public static object ToType<T>(this object obj, T type)
        {
            //create instance of T type object:
            var tmp = Activator.CreateInstance(Type.GetType(type.ToString()));
            //loop through the properties of the object you want to covert:
            foreach (System.Reflection.PropertyInfo pi in obj.GetType().GetProperties())
            {
                try
                {
                    //get the value of property and try 
                    //to assign it to the property of T type object:
                    tmp.GetType().GetProperty(pi.Name).SetValue(tmp, pi.GetValue(obj, null), null);
                }
                catch { }
            }

            //return the T type object:         
            return tmp;
        }

        public static object ToNonAnonymousList<T>(this List<T> list, Type t)
        {
            //define system Type representing List of objects of T type:
            var genericType = typeof(List<>).MakeGenericType(t);

            //create an object instance of defined type:
            var l = Activator.CreateInstance(genericType);

            //get method Add from from the list:
            System.Reflection.MethodInfo addMethod = l.GetType().GetMethod("Add");

            //loop through the calling list:
            foreach (T item in list)
            {
                //convert each object of the list into T object 
                //by calling extension ToType<T>()
                //Add this object to newly created list:
                addMethod.Invoke(l, new object[] { item.ToType(t) });
            }

            //return List of T objects:
            return l;
        }
    }
}
