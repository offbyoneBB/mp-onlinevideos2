using NUnit.Framework;
using OnlineVideos.CrossDomain;
using OnlineVideos.Sites;
using System.Reflection;

namespace OnlineVideos.Tests
{
    [TestFixture]
    public class AssemblyLoadTests
    {
        const string TEST_SITE_UTIL_DIRECTORY_NAME = "TestSiteUtils";
        const string TEST_SITE_UTIL_ASSEMBLY_NAME = "OnlineVideos.Sites.Test.dll";

        [OneTimeSetUp]
        protected void Setup()
        {
            OnlineVideosAssemblyContext.UseSeperateDomain = true;
        }

        [OneTimeTearDown]
        protected void TearDown()
        {
            string siteUtilDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), TEST_SITE_UTIL_DIRECTORY_NAME);
            if (!Directory.Exists(siteUtilDirectory))
                return;
            string siteUtilDllPath = Path.Combine(siteUtilDirectory, TEST_SITE_UTIL_ASSEMBLY_NAME);
            if (File.Exists(siteUtilDllPath))
                try
                {
                    File.Delete(siteUtilDllPath);
                }
                catch { }
            try
            {
                Directory.Delete(siteUtilDirectory);
            }
            catch { }
        }

        [Test]
        public void ShouldRecreateSettingsInstance()
        {
            OnlineVideoSettings instance = OnlineVideoSettings.Instance;

            OnlineVideoSettings.Reload();

            OnlineVideoSettings recreated = OnlineVideoSettings.Instance;
            Assert.AreNotSame(instance, recreated);
        }

        [Test]
        public void ShouldUpdateSite()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string siteUtilDirectory = Path.Combine(currentDirectory, TEST_SITE_UTIL_DIRECTORY_NAME);
            string siteUtilDllPath = Path.Combine(siteUtilDirectory, TEST_SITE_UTIL_ASSEMBLY_NAME);

            // Create or clean up a site util directory for loading the site util dlls from
            if (!Directory.Exists(siteUtilDirectory))
                Directory.CreateDirectory(siteUtilDirectory);
            else if (File.Exists(siteUtilDllPath))
                File.Delete(siteUtilDllPath);

            OnlineVideoSettings.Instance.DllsDir = siteUtilDirectory;

            // Copy the first version of the site util dll to the directory and create the site util
            File.Copy(Path.Combine(currentDirectory, "OnlineVideos.Sites.Testv1.dll"), siteUtilDllPath);
            SiteUtilBase siteUtil = SiteUtilFactory.Instance.CreateFromShortName("TestSite", new SiteSettings());

            Assert.NotNull(siteUtil);
            Assert.AreEqual("v1", siteUtil.GetVideos(null).First().Title);

            // Attempt to overwrite with the second version of the dll and reload the dlls and site util
            File.Copy(Path.Combine(currentDirectory, "OnlineVideos.Sites.Testv2.dll"), siteUtilDllPath, true);
            OnlineVideoSettings.Reload();
            siteUtil = SiteUtilFactory.Instance.CreateFromShortName("TestSite", new SiteSettings());

            Assert.NotNull(siteUtil);
            Assert.AreEqual("v2", siteUtil.GetVideos(null).First().Title);
        }
    }
}
