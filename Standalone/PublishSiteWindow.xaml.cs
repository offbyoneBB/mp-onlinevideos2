using OnlineVideos;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace Standalone
{
    /// <summary>
    /// Interaction logic for PublishSiteWindow.xaml
    /// </summary>
    public partial class PublishSiteWindow : Window, INotifyPropertyChanged
    {
        string email;
        string password;
        Regex emailRegex = new Regex(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?");

        public ICommand PublishCommand { get; }

        public ICommand RegisterCommand { get; }

        public SiteSettings Site { get; private set; }

        public string Email
        {
            get { return email; }
            set
            {
                if (value != email)
                {
                    email = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Email"));
                }
            }
        }
        public string Password
        {
            get { return password; }
            set
            {
                if (value != password)
                {
                    password = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Password"));
                }
            }
        }

        public PublishSiteWindow()
        {
            InitializeComponent();
        }

        public PublishSiteWindow(SiteSettings site)
        {
            Site = site;
            PublishCommand = new RelayCommand(Publish, CanPublish);
            RegisterCommand = new RelayCommand(Register, CanRegister);
            DataContext = this;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        protected void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        bool CanPublish(object sender)
        {
            return !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(email) && emailRegex.IsMatch(email);
        }

        bool CanRegister(object sender)
        {
            return !string.IsNullOrWhiteSpace(email) && emailRegex.IsMatch(email);
        }

        void Publish(object sender)
        {
            // set current Time to last updated in the xml, so it can be compared later
            DateTime lastUdpBkp = Site.LastUpdated;
            Site.LastUpdated = DateTime.Now;
            SerializableSettings s = new SerializableSettings() { Sites = new BindingList<SiteSettings>() };
            s.Sites.Add(Site);
            var siteDoc = new XmlDocument();
            using (var ms = new MemoryStream())
            {
                s.Serialize(ms);
                ms.Position = 0;
                siteDoc.Load(ms);
            }
            XmlWriterSettings xmlSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = true
            };
            StringBuilder sb = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(sb, xmlSettings);
            siteDoc.SelectSingleNode("//Site").WriteTo(writer);
            writer.Flush();
            string siteXmlString = sb.ToString();
            byte[] icon = null;
            string image = Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Icons"), Site.Name + ".png");
            if (File.Exists(image)) icon = File.ReadAllBytes(image);
            byte[] banner = null;
            image = Path.Combine(Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, "Banners"), Site.Name + ".png");
            if (File.Exists(image)) banner = File.ReadAllBytes(image);
            bool success = false;
            try
            {
                string dll = OnlineVideos.Sites.SiteUtilFactory.RequiredDll(Site.UtilName);
                var ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                string msg = "";
                if (!string.IsNullOrEmpty(dll))
                {
                    string location = Path.Combine(OnlineVideoSettings.Instance.DllsDir, dll + ".dll");
                    if (File.Exists(location))
                    {
                        byte[] data = File.ReadAllBytes(location);
                        System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                        string md5LocalDll = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
                        // check webservice if we need to submit the dll
                        string md5RemoteDll = null;
                        string owner = ws.GetDllOwner(dll, out md5RemoteDll);
                        bool dllFound = md5RemoteDll != null;
                        bool dllsAreEqual = dllFound ? md5RemoteDll == md5LocalDll : false;
                        bool userIsOwner = dllFound ? owner == Email : true;
                        if (!dllsAreEqual)
                        {
                            bool isAdmin = false;
                            if (!userIsOwner)
                            {
                                if (MessageBox.Show("Only administrators can overwrite a DLL they don't own. I am an Admin. Proceed?", "New DLL - Admin required", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                                {
                                    return;
                                }
                            }
                            if (userIsOwner || isAdmin)
                            {
                                string info = dllFound ? "DLL found on server differs from your local file, do you want to update the existing one?" : "Do you want to upload the required dll?";
                                if (MessageBox.Show(info, "DLL required", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    if (data == null) data = File.ReadAllBytes(location);
                                    success = ws.SubmitDll(Email, Password, dll, data, out msg);
                                    MessageBox.Show(msg, success ? Translation.Instance.Success : Translation.Instance.Error, MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                success = ws.SubmitSite(Email, Password, siteXmlString, icon, banner, dll, out msg);
                MessageBox.Show(msg, success ? Translation.Instance.Success : Translation.Instance.Error, MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Translation.Instance.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // if the site was not submitted, restore old last updated date, so saving won't write the wrong value
            if (!success) Site.LastUpdated = lastUdpBkp;
        }

        void Register(object sender)
        {
            var ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
            string msg = null;
            var success = ws.RegisterEmail(Email, out msg);
            MessageBox.Show(msg, success ? Translation.Instance.Success : Translation.Instance.Error, MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
        }

    }
}
