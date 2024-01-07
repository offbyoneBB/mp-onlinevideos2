using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Xml;
using WebServiceCore.Models;
using WebServiceCore.Models.Dto;
using WebServiceCore.Models.Entities;
using WebServiceCore.Services;

namespace WebServiceCore.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OnlineVideosController : ControllerBase
    {
        private readonly OnlineVideosDataContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IImageService _imageService;
        private readonly IConfiguration _config;
        private readonly ILogger<OnlineVideosController> _logger;

        public OnlineVideosController(OnlineVideosDataContext context, IWebHostEnvironment environment, IImageService imageService, IConfiguration config, ILogger<OnlineVideosController> logger)
        {
            _context = context;
            _environment = environment;
            _imageService = imageService;
            _config = config;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<ActionResult<string>> RegisterEmail(string email)
        {
            User user = await _context.Users.FindAsync(email);
            bool isNewUser = false;
            if (user == null)
            {
                user = new User() { Email = email, IsAdmin = false, Password = RandomString(6) };
                isNewUser = true;
            }
            if (await SendPasswordEmail(user))
            {
                if (isNewUser)
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                return Ok("Password was sent to your Email address.");
            }
            else
                return Problem("Password could not be sent to your Email address.");
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 401)]
        public async Task<ActionResult<string>> SubmitSite(SubmitSiteDto dto)
        {
            DateTime updateTime = DateTime.Now.ToUniversalTime(); // use universal Time when setting the current TimeStamp to the Database

            // is the given site xml at least valid xml?
            XmlDocument xml = new XmlDocument();
            string siteName = "";
            string lang = "";
            string desc = "";
            bool isAdult = false;
            try
            {
                xml.LoadXml(dto.SiteXml);
                if (xml.SelectNodes("//Site").Count != 1)
                    return BadRequest("Exactly one <Site> node expected!");

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
                return BadRequest("Invalid xml for site!");
            }

            User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            // Check user and password
            if (user == null)
                return BadRequest("Email not registered!");
            if (user.Password != dto.Password)
                return Unauthorized("Wrong password!");

            // is the requiredDll available on the server?
            if (!string.IsNullOrEmpty(dto.RequiredDll) && !await _context.Dlls.AnyAsync(d => d.Name == dto.RequiredDll))
                return BadRequest("Required Dll not found on Server!");

            // does the site already exist?
            Site site = await _context.Sites.Include(s => s.Owner).FirstOrDefaultAsync(s => s.Name == siteName);
            if (site != null)
            {
                // need to update
                if (site.Owner.Email == user.Email || user.IsAdmin)
                {
                    site.Language = lang;
                    site.Description = desc;
                    site.IsAdult = isAdult;
                    site.LastUpdated = updateTime;
                    if (site.XML != dto.SiteXml) site.XML = dto.SiteXml;
                    if (site.DllId != dto.RequiredDll) site.DllId = dto.RequiredDll;
                    site.State = SiteState.Working;
                    await _context.SaveChangesAsync();
                    return Ok("Site successfully updated!" + await SaveImages(siteName, dto.Icon, dto.Banner));
                }
                else
                    return Unauthorized("Only the owner or an admin can update existing sites!");
            }
            else
            {
                // insert new site
                site = new Site()
                {
                    Name = siteName,
                    Language = lang,
                    Description = desc,
                    IsAdult = isAdult,
                    LastUpdated = updateTime,
                    XML = dto.SiteXml,
                    DllId = dto.RequiredDll,
                    Owner = user,
                    State = SiteState.Working
                };
                _context.Sites.Add(site);
                await _context.SaveChangesAsync();
                return Ok("Site successfully added!" + await SaveImages(siteName, dto.Icon, dto.Banner));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 401)]
        public async Task<ActionResult<string>> SubmitDll(SubmitDllDto dto)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            // Check user and password
            if (user == null)
                return BadRequest("Email not registered!");
            if (user.Password != dto.Password)
                return Unauthorized("Wrong password!");

            // calc dll's MD5 hash
            string md5String = GetMD5Hash(dto.Data);
            // does the dll already exist?
            Dll dll = await _context.Dlls.Include(d => d.Owner).FirstOrDefaultAsync(d => d.Name == dto.Name);
            if (dll != null)
            {
                // need to update                        
                if (dll.Owner.Email == user.Email || user.IsAdmin)
                {
                    dll.MD5 = md5String;
                    dll.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync();
                    // write the file
                    await System.IO.File.WriteAllBytesAsync(GetDllPath(dto.Name), dto.Data);
                    return Ok("Dll successfully updated!");
                }
                else
                    return Unauthorized("Only the owner or an admin can update existing dlls!");
            }
            else
            {
                // insert new dll
                Dll newDll = new Dll()
                {
                    MD5 = md5String,
                    LastUpdated = DateTime.Now,
                    Name = dto.Name,
                    Owner = user
                };
                _context.Dlls.Add(newDll);
                await _context.SaveChangesAsync();
                // write the file
                await System.IO.File.WriteAllBytesAsync(GetDllPath(dto.Name), dto.Data);
                return Ok("Dll successfully added!");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<ActionResult<string>> SubmitReport(SubmitReportDto dto)
        {
            Site site = await _context.Sites.FirstOrDefaultAsync(s => s.Name == dto.SiteName);
            if (site == null)
                return BadRequest("Site not found!");

            Report report = new Report
            {
                Date = DateTime.Now,
                Message = dto.Message,
                Type = dto.Type,
                Site = site,
            };
            if (dto.Type == ReportType.Broken && site.State == SiteState.Working) site.State = SiteState.Reported;
            else if (dto.Type == ReportType.ConfirmedBroken) site.State = SiteState.Broken;
            else if (dto.Type == ReportType.RejectedBroken || dto.Type == ReportType.Fixed) site.State = SiteState.Working;

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            SendNewReportEmail(report);
            return Ok("Report successfully submitted!");
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GetReportDto>), 200)]
        public async Task<ActionResult<IEnumerable<GetReportDto>>> GetReports(string siteName)
        {
            return await _context.Reports
                .AsNoTrackingWithIdentityResolution()
                .Where(r => r.Site.Name == siteName)
                .Select(r => r.ToGetReportDto())
                .ToListAsync();
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GetSiteDto>), 200)]
        public async Task<ActionResult<IEnumerable<GetSiteDto>>> GetSitesOverview()
        {
            return await _context.Sites
                .AsNoTrackingWithIdentityResolution()
                .Select(s => s.ToGetSiteDto())
                .ToListAsync();
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GetDllDto>), 200)]
        public async Task<ActionResult<IEnumerable<GetDllDto>>> GetDllsOverview()
        {
            return await _context.Dlls
                .AsNoTrackingWithIdentityResolution()
                .Select(d => d.ToGetDllDto())
                .ToListAsync();
        }

        [HttpGet]
        [ProducesResponseType(typeof(GetDllOwnerDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GetDllOwnerDto>> GetDllOwner(string dllName)
        {
            Dll dll = await _context.Dlls.FindAsync(dllName);
            if (dll == null)
                return NotFound();

            return dll.ToGetDllOwnerDto();
        }

        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<string>> GetSiteXml(string siteName)
        {
            Site site = await _context.Sites.FindAsync(siteName);
            if (site == null)
                return NotFound();
            return site.XML;
        }

        [HttpGet]
        [ProducesResponseType(typeof(byte[]), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<byte[]>> GetSiteIcon(string siteName)
        {
            string path = GetIconPath(siteName);
            return await GetFile(path);
        }

        [HttpGet]
        [ProducesResponseType(typeof(byte[]), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<byte[]>> GetSiteIconIfChanged(string siteName, string md5)
        {
            string path = GetIconPath(siteName);
            return await GetFile(path, md5);
        }

        [HttpGet]
        [ProducesResponseType(typeof(byte[]), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<byte[]>> GetSiteBanner(string siteName)
        {
            string path = GetBannerPath(siteName);
            return await GetFile(path);
        }

        [HttpGet]
        [ProducesResponseType(typeof(byte[]), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<byte[]>> GetSiteBannerIfChanged(string siteName, string md5)
        {
            string path = GetBannerPath(siteName);
            return await GetFile(path, md5);
        }

        [HttpGet]
        [ProducesResponseType(typeof(byte[]), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<byte[]>> GetDll(string name)
        {
            string path = GetDllPath(name);
            return await GetFile(path);
        }

        async Task<ActionResult<byte[]>> GetFile(string path, string md5 = null)
        {
            if (!System.IO.File.Exists(path))
                return NotFound();
            byte[] bytes = await System.IO.File.ReadAllBytesAsync(path);
            if (!string.IsNullOrEmpty(md5) && GetMD5Hash(bytes) == md5)
                return NoContent();
            return bytes;
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

        private SmtpClient getSmtpClient(string toAddress, out MailMessage m)
        {
            var smtpSettings = _config.GetSection("Smtp");

            m = new MailMessage(smtpSettings.GetValue<String>("FromAddress"), toAddress);

            var smtpClient = new SmtpClient();
            smtpClient.Port = smtpSettings.GetValue<int>("Port");
            smtpClient.EnableSsl = true;
            smtpClient.Host = smtpSettings.GetValue<String>("Server");
            smtpClient.Credentials = new NetworkCredential(smtpSettings.GetValue<String>("User"), smtpSettings.GetValue<String>("Password"));
            return smtpClient;
        }

        async Task<bool> SendPasswordEmail(User user)
        {
            try
            {
                MailMessage m;
                var smtpClient = getSmtpClient(user.Email, out m);
                m.Subject = "Thank you for registering with MediaPortal OnlineVideos Plugin.";
                m.Body = "Your registered password is: " + user.Password;
                await smtpClient.SendMailAsync(m);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password email");
                return false;
            }
        }

        bool SendNewReportEmail(Report report)
        {
            try
            {
                MailMessage m;
                var smtpClient = getSmtpClient(report.Site.Owner.Email, out m);

                m.Subject = string.Format("OnlineVideos: New Report ({0}) for {1}", report.Type.ToString(), report.Site.Name);
                m.Body = report.Message;
                smtpClient.Send(m);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new report email");
                return false;
            }
        }

        string GetIconPath(string siteName)
        {
            return GetSafeWebRootPath("Icons", siteName, ".png");
        }

        string GetBannerPath(string siteName)
        {
            return GetSafeWebRootPath("Banners", siteName, ".png");
        }

        string GetDllPath(string name)
        {
            return GetSafeWebRootPath("Dlls", name, ".dll");
        }

        string GetSafeWebRootPath(string directory, string fileName, string extension)
        {
            // Check that the filename is valid, no sanitization is performed to retain the
            // behaviour of the previous API
            if (Path.GetInvalidFileNameChars().Any(c => fileName.Contains(c)))
                throw new ArgumentException($"Invalid filename '{fileName}'", nameof(fileName));

            // The above check should prevent directory traversal attacks, e.g. by passing a filename starting with ..\,
            // as path separators are not valid in filenames, however as an additional layer resolve the absolute path
            // and confirm that it is a subdirectory of WebRootPath.
            var subPath = Path.Combine(directory, fileName + extension);
            string absolutePath = Path.GetFullPath(subPath, _environment.WebRootPath);
            if (!absolutePath.StartsWith(_environment.WebRootPath))
                throw new ArgumentException($"Invalid filename '{fileName}'", nameof(fileName));
            return absolutePath;
        }

        async Task<string> SaveImages(string siteName, byte[] icon, byte[] banner)
        {
            string message = "";
            if (icon != null && icon.Length > 0)
            {
                message += await _imageService.TrySaveIcon(icon, GetIconPath(siteName));
                message += " ";
            }
            if (banner != null && banner.Length > 0)
            {
                message += await _imageService.TrySaveBanner(banner, GetBannerPath(siteName));
            }
            return message;
        }

        string GetMD5Hash(byte[] data)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] md5Hash = md5.ComputeHash(data);
            string md5String = BitConverter.ToString(md5Hash).Replace("-", "").ToLower();
            return md5String;
        }

        #endregion
    }
}
