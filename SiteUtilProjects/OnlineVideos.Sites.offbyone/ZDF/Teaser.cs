namespace ZDFMediathek2009.Code
{
    using System;
    using System.Globalization;
    using System.Web;
    using ZDFMediathek2009.Code.DTO;

    public class Teaser
    {
        private string _banderoleTitle;
        private string _contextChannelID;
        private string _customChannelTitle;
        private bool _isInUserCollection;
        private bool _isSearchTeaser;        
        private string _nameOfTheChannel;
        private ZDFMediathek2009.Code.TeaserListChoiceType _teaserListChoiceType;
        private teaser _value;

        public Teaser()
        {
            this._banderoleTitle = "";            
            this._contextChannelID = "";
        }

        public Teaser(teaser value)
        {
            this._banderoleTitle = "";            
            this._contextChannelID = "";
            this._value = value;
        }

        public string GetImage(string keyDimensions)
        {
            if ((this.Value != null) && (this.Value.teaserimages != null))
            {
                foreach (teaserimagesTeaserimage teaserimage in this.Value.teaserimages)
                {
                    if (teaserimage.key == keyDimensions)
                    {
                        return teaserimage.Value;
                    }
                }
            }
            return null;
        }
        /*
        public void JumpInCuePointList()
        {
            if (ZDFMediathek2009.Code.Application.Current.CurrentPlaylist != null)
            {
                try
                {
                    if (this.MyNavigationState != null)
                    {
                        Playlist currentPlaylist = ZDFMediathek2009.Code.Application.Current.CurrentPlaylist;
                        this.MyNavigationState.MakeBroadcastState(this, currentPlaylist).Show();
                        ZDFMediathek2009.Code.Application.Current.PaneState = PaneState.AllClosed;
                    }
                    else
                    {
                        Logger.Log("MyNavigationState is null");
                    }
                }
                catch (ServiceException exception)
                {
                    ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-" + exception.ExceptionType.ToString())));
                }
                catch (StreamException)
                {
                    ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-stream-nicht-verfuegbar")));
                }
                catch (Exception exception2)
                {
                    ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-error")));
                    Logger.Log(exception2);
                }
            }
        }

        public NavigationState Navigate()
        {
            if (this.IsSearchTeaser)
            {
                ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_Searchpage(ZDFMediathek2009.Code.Application.Current.Navigation.ChangeTopNavigation("search"), this.CustomChannelTitle, true));
                return this.MyNavigationState;
            }
            if (((this.Value.type == type.sendung) || (this.Value.type == type.topthema)) || (((this.Value.type == type.thema) || (this.Value.type == type.@event)) || (this.Value.type == type.rubrik)))
            {
                return ZDFMediathek2009.Code.Application.Current.Navigation.DiveIntoChannel(this);
            }
            if (this.Value.type == type.einzelsendung)
            {
                Teaser[] teaserArray = ZDFMediathek2009.Code.Application.Current.Navigation.GetPageTeasers(this.ID, ZDFMediathek2009.Code.TeaserListChoiceType.CurrentBroadcasts, 0x19, 0, this.MyNavigationState, false);
                if (teaserArray.Length > 0)
                {
                    try
                    {
                        if (this.MyNavigationState != null)
                        {
                            Playlist playlist = new Playlist(this.MyNavigationState, PlaylistType.Einzelsendung, this);
                            foreach (Teaser teaser in teaserArray)
                            {
                                if (!teaser.IsChannel)
                                {
                                    playlist.Teasers.Add(teaser);
                                }
                            }
                            this.MyNavigationState.MakeBroadcastState(teaserArray[0], playlist).Show();
                            ZDFMediathek2009.Code.Application.Current.PaneState = PaneState.AllClosed;
                        }
                        else
                        {
                            Logger.Log("MyNavigationState is null");
                        }
                    }
                    catch (ServiceException exception)
                    {
                        ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-" + exception.ExceptionType.ToString())));
                    }
                    catch (StreamException)
                    {
                        ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-stream-nicht-verfuegbar")));
                    }
                    catch (Exception exception2)
                    {
                        ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-error")));
                        Logger.Log(exception2);
                    }
                }
            }
            if ((((this.Value.type == type.imageseries_emotional) || (this.Value.type == type.imageseries_emotionalaudio)) || ((this.Value.type == type.imageseries_informativ) || (this.Value.type == type.imageseries_informativaudio))) || ((this.Value.type == type.livevideo) || (this.Value.type == type.video)))
            {
                try
                {
                    if (this.MyNavigationState != null)
                    {
                        BroadcastState state2;
                        if (this.TeaserListChoiceType == ZDFMediathek2009.Code.TeaserListChoiceType.Search)
                        {
                            state2 = this.MyNavigationState.MakeBroadcastState(this, ZDFMediathek2009.Code.Application.Current.CurrentPlaylist);
                        }
                        else
                        {
                            state2 = this.MyNavigationState.MakeBroadcastState(this);
                        }
                        state2.Show();
                        ZDFMediathek2009.Code.Application.Current.PaneState = PaneState.AllClosed;
                    }
                    else
                    {
                        Logger.Log("MyNavigationState is null");
                    }
                }
                catch (ServiceException exception3)
                {
                    ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-" + exception3.ExceptionType.ToString())));
                }
                catch (StreamException)
                {
                    ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-stream-nicht-verfuegbar")));
                }
                catch (Exception exception4)
                {
                    ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-error")));
                    Logger.Log(exception4);
                }
            }
            return this.MyNavigationState;
        }

        public NavigationState NavigateAndPlay()
        {
            return this.Navigate();
        }

        public NavigationState NavigateFromSearchTeaserList(Teaser[] teasers)
        {
            try
            {
                if (((this.Value.type == type.sendung) || (this.Value.type == type.topthema)) || (((this.Value.type == type.thema) || (this.Value.type == type.@event)) || (this.Value.type == type.rubrik)))
                {
                    return ZDFMediathek2009.Code.Application.Current.Navigation.DiveIntoChannel(this);
                }
                if (this.Value.type == type.einzelsendung)
                {
                    Teaser[] teaserArray = ZDFMediathek2009.Code.Application.Current.Navigation.GetPageTeasers(this.ID, ZDFMediathek2009.Code.TeaserListChoiceType.CurrentBroadcasts, 0x19, 0, this.MyNavigationState, false);
                    if (teaserArray.Length > 0)
                    {
                        try
                        {
                            if (this.MyNavigationState != null)
                            {
                                Playlist playlist = new Playlist(this.MyNavigationState, PlaylistType.Einzelsendung, this);
                                foreach (Teaser teaser in teaserArray)
                                {
                                    if (!teaser.IsChannel)
                                    {
                                        playlist.Teasers.Add(teaser);
                                    }
                                }
                                this.MyNavigationState.MakeBroadcastState(teaserArray[0], playlist).Show();
                                ZDFMediathek2009.Code.Application.Current.PaneState = PaneState.AllClosed;
                            }
                            else
                            {
                                Logger.Log("MyNavigationState is null");
                            }
                        }
                        catch (ServiceException exception)
                        {
                            ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-" + exception.ExceptionType.ToString())));
                        }
                        catch (StreamException)
                        {
                            ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-stream-nicht-verfuegbar")));
                        }
                        catch (Exception exception2)
                        {
                            ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-error")));
                            Logger.Log(exception2);
                        }
                    }
                }
                else if (this.MyNavigationState != null)
                {
                    Playlist playlist2 = new Playlist(this.MyNavigationState, PlaylistType.Search, null);
                    foreach (Teaser teaser2 in teasers)
                    {
                        if (!teaser2.IsChannel)
                        {
                            playlist2.Teasers.Add(teaser2);
                        }
                    }
                    this.MyNavigationState.MakeBroadcastState(this, playlist2).Show();
                    ZDFMediathek2009.Code.Application.Current.PaneState = PaneState.AllClosed;
                }
                else
                {
                    Logger.Log("MyNavigationState is null");
                }
            }
            catch (ServiceException exception3)
            {
                ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-" + exception3.ExceptionType.ToString())));
            }
            catch (StreamException)
            {
                ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-stream-nicht-verfuegbar")));
            }
            catch (Exception exception4)
            {
                ZDFMediathek2009.Code.Application.Current.Session.GoTo(PageRequest.MAKE_ErrorPage(ConfigurationHelper.GetDictionaryKeyValue("titel_fehler-error")));
                Logger.Log(exception4);
            }
            return this.MyNavigationState;
        }
        */
        private string ParseLength()
        {
            string str = "";
            if ((this.Value != null) && (this.Value.details != null))
            {
                switch (this.Value.type)
                {
                    case type.einzelsendung:
                    {
                        string s = this.Value.details.length.Replace("min", "").Trim();
                        int result = 0;
                        int.TryParse(s, out result);
                        TimeSpan span = new TimeSpan(0, result, 0);
                        if (span.Hours <= 0)
                        {
                            return (ConfigurationHelper.GetDictionaryKeyValue("titel_label-" + this.Value.type) + ", " + span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00"));
                        }
                        return (ConfigurationHelper.GetDictionaryKeyValue("titel_label-" + this.Value.type) + ", " + span.Hours.ToString("00") + ":" + span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00"));
                    }
                    case type.thema:
                    case type.topthema:
                    case type.rubrik:
                        return this.NumberOfItems;

                    case type.sendung:
                        return this.NumberOfItems;

                    case type.@event:
                    case type.podcast:
                    case type.link:
                        return str;

                    case type.video:
                        DateTime time;
                        DateTime.TryParse(this.Value.details.length, out time);
                        if (time.Hour <= 0)
                        {
                            return (ConfigurationHelper.GetDictionaryKeyValue("titel_label-" + this.Value.type) + ", " + time.ToString("mm:ss", new CultureInfo("DE-de").DateTimeFormat));
                        }
                        return (ConfigurationHelper.GetDictionaryKeyValue("titel_label-" + this.Value.type) + ", " + time.ToString("HH:mm:ss", new CultureInfo("DE-de").DateTimeFormat));

                    case type.livevideo:
                        return ConfigurationHelper.GetDictionaryKeyValue("titel_label-" + this.Value.type);

                    case type.imageseries_informativ:
                    case type.imageseries_emotional:
                    case type.imageseries_informativaudio:
                    case type.imageseries_emotionalaudio:
                        return ConfigurationHelper.GetDictionaryKeyValue("titel_label-" + this.Value.type).Replace("%(ANZAHL)", this.Value.details.length);
                }
            }
            return str;
        }

        public string Airtime
        {
            get
            {
                if (((this.Value != null) && (this.Value.details != null)) && (this.Value.details.airtime != null))
                {
                    DateTime time;
                    DateTime.TryParse(this.Value.details.airtime, out time);
                    return time.ToString("dd.MM.yyyy", new CultureInfo("DE-de").DateTimeFormat);
                }
                return "";
            }
        }

        public DateTime AirtimeDateTime
        {
            get
            {
                if (((this.Value != null) && (this.Value.details != null)) && (this.Value.details.airtime != null))
                {
                    DateTime time;
                    DateTime.TryParse(this.Value.details.airtime, out time);
                    return time;
                }
                return new DateTime();
            }
        }

        public string AirtimeInHours
        {
            get
            {
                if ((this.Value != null) && (this.Value.details != null))
                {
                    DateTime time;
                    DateTime.TryParse(this.Value.details.airtime, out time);
                    return time.ToString("HH:mm", new CultureInfo("DE-de").DateTimeFormat);
                }
                return "";
            }
        }
        
        public string BanderoleTitle
        {
            get
            {
                if ((this._banderoleTitle == "") && (this.Value != null))
                {
                    if (this.Value.type == type.livevideo)
                    {
                        if (this.Value.member == teaserMember.onAir)
                        {
                            this._banderoleTitle = ConfigurationHelper.GetDictionaryKeyValue("titel_jetzt-live");
                        }
                        if (this.Value.member == teaserMember.today)
                        {
                            this._banderoleTitle = ConfigurationHelper.GetDictionaryKeyValue("titel_heute-live");
                        }
                    }
                    if (this.IsChannel)
                    {
                        this._banderoleTitle = this.ChannelTitle;
                    }
                }
                return this._banderoleTitle;
            }
            set
            {
                this._banderoleTitle = value;
            }
        }

        public bool CanBeAddedToMerkliste
        {
            get
            {
                return ((!this.IsChannel && !this.IsLiveVideo) && !this.IsEvent);
            }
        }

        public string Categories
        {
            get
            {
                if (((this.Value != null) && (this.Value.details != null)) && (this.Value.details.categories != null))
                {
                    return HttpUtility.HtmlDecode(this.Value.details.categories);
                }
                return "";
            }
        }

        public string ChannelID
        {
            get
            {
                if ((this.Value != null) && (this.Value.details != null))
                {
                    return this.Value.details.originChannelId;
                }
                return "";
            }
        }

        public string ChannelTitle
        {
            get
            {
                if (this.NameOfTheChannel != null)
                {
                    return this.NameOfTheChannel;
                }
                if (((this.Value != null) && (this.Value.details != null)) && (this.Value.details.originChannelTitle != null))
                {
                    return HttpUtility.HtmlDecode(this.Value.details.originChannelTitle.ToUpper());
                }
                return "";
            }
        }

        public string CommentType
        {
            get
            {
                if ((this.Value == null) || ((this.Value.type != type.livevideo) && (this.Value.member != teaserMember.today)))
                {
                    return "";
                }
                return "Orange";
            }
        }

        public string ContextChannelID
        {
            get
            {
                return this._contextChannelID;
            }
            set
            {
                this._contextChannelID = value;
            }
        }

        public string CustomChannelTitle
        {
            get
            {
                return this._customChannelTitle;
            }
            set
            {
                this._customChannelTitle = value;
            }
        }

        public string Details
        {
            get
            {
                if ((this.Value != null) && (this.Value.information != null))
                {
                    return HttpUtility.HtmlDecode(this.Value.information.detail);
                }
                return "";
            }
        }

        public string ID
        {
            get
            {
                if ((this.Value != null) && (this.Value.details != null))
                {
                    return this.Value.details.assetId;
                }
                return "";
            }
        }

        public string Image
        {
            get
            {
                if ((this.Value != null) && (this.Value.teaserimages != null))
                {
                    foreach (teaserimagesTeaserimage teaserimage in this.Value.teaserimages)
                    {
                        if (teaserimage.key == "173x120")
                        {
                            return teaserimage.Value;
                        }
                    }
                }
                return null;
            }
        }

        public string Image116x54
        {
            get
            {
                return this.GetImage("116x54");
            }
        }

        public string Image116x88
        {
            get
            {
                return this.GetImage("116x88");
            }
        }

        public string Image173x120
        {
            get
            {
                return this.GetImage("173x120");
            }
        }

        public string Image276x155
        {
            get
            {
                return this.GetImage("276x155");
            }
        }

        public string Image476x176
        {
            get
            {
                return this.GetImage("476x176");
            }
        }

        public string Image485x273
        {
            get
            {
                return this.GetImage("485x273");
            }
        }

        public string Image644x363
        {
            get
            {
                return this.GetImage("644x363");
            }
        }

        public string Image72x54
        {
            get
            {
                return this.GetImage("72x54");
            }
        }

        public string Image75x52
        {
            get
            {
                return this.GetImage("75x52");
            }
        }

        public string Image94x65
        {
            get
            {
                return this.GetImage("94x65");
            }
        }

        public string ImageUrl
        {
            get
            {
                if ((this.Value != null) && (this.Value.teaserimages != null))
                {
                    foreach (teaserimagesTeaserimage teaserimage in this.Value.teaserimages)
                    {
                        if (teaserimage.key == "116x54")
                        {
                            return teaserimage.Value;
                        }
                    }
                }
                return "";
            }
        }

        public bool IsChannel
        {
            get
            {
                if ((this.Value == null) || (((this.Value.type != type.sendung) && (this.Value.type != type.topthema)) && ((this.Value.type != type.thema) && (this.Value.type != type.rubrik))))
                {
                    return false;
                }
                return true;
            }
        }

        public bool IsEinzelsendung
        {
            get
            {
                return ((this.Value != null) && (this.Value.type == type.einzelsendung));
            }
        }

        public bool IsEvent
        {
            get
            {
                return ((this.Value != null) && (this.Value.type == type.@event));
            }
        }

        public bool IsImageGallery
        {
            get
            {
                if ((this.Value == null) || (((this.Value.type != type.imageseries_emotional) && (this.Value.type != type.imageseries_emotionalaudio)) && ((this.Value.type != type.imageseries_informativ) && (this.Value.type != type.imageseries_informativaudio))))
                {
                    return false;
                }
                return true;
            }
        }

        public bool IsInUserCollection
        {
            get
            {
                return this._isInUserCollection;
            }
            set
            {
                this._isInUserCollection = value;
                //base.FirePropertyChanged("IsInUserCollection");
            }
        }

        public bool IsLiveVideo
        {
            get
            {
                return ((this.Value != null) && (this.Value.type == type.livevideo));
            }
        }

        public bool IsOnAir
        {
            get
            {
                return ((this.Value != null) && (this.Value.member == teaserMember.onAir));
            }
        }

        public bool IsSearchTeaser
        {
            get
            {
                return this._isSearchTeaser;
            }
            set
            {
                this._isSearchTeaser = value;
            }
        }

        public bool IsTipp
        {
            get
            {
                return (((this.Value != null) && (this.Value.details != null)) && this.Value.details.tipp);
            }
        }

        public bool IsTopThema
        {
            get
            {
                return ((this.Value != null) && (this.Value.type == type.topthema));
            }
        }

        public bool IsVideo
        {
            get
            {
                if ((this.Value == null) || ((this.Value.type != type.livevideo) && (this.Value.type != type.video)))
                {
                    return false;
                }
                return true;
            }
        }

        public string Length
        {
            get
            {
                return this.ParseLength();
            }
        }

        public string Member
        {
            get
            {
                if (this.Value != null)
                {
                    return this.Value.member.ToString();
                }
                return "";
            }
        }
      
        public string NameOfTheChannel
        {
            get
            {
                return this._nameOfTheChannel;
            }
            set
            {
                this._nameOfTheChannel = value;
            }
        }

        public int NumberOfChannelTeasers
        {
            get
            {
                if ((this.Value != null) && (this.Value.details != null))
                {
                    int result = 0;
                    int.TryParse(this.Value.details.length, out result);
                    return result;
                }
                return 0;
            }
        }

        public string NumberOfItems
        {
            get
            {
                if ((this.Value != null) && (this.Value.details != null))
                {
                    if ((this.Value.type == type.thema) || (this.Value.type == type.topthema))
                    {
                        return (this.Value.details.length + ((this.Value.details.length.Trim() == "1") ? " BEITRAG" : " BEITR\x00c4GE") + " ZUM THEMA");
                    }
                    if (this.Value.type == type.rubrik)
                    {
                        return (this.Value.details.length + ((this.Value.details.length.Trim() == "1") ? " BEITRAG" : " BEITR\x00c4GE") + " ZUR RUBRIK");
                    }
                    if (this.Value.type == type.sendung)
                    {
                        return (this.Value.details.length + ((this.Value.details.length.Trim() == "1") ? " BEITRAG" : " BEITR\x00c4GE") + " ZUR SENDUNG");
                    }
                }
                return "";
            }
        }

        public int NumberOfTeasers
        {
            get
            {
                if ((this.Value != null) && (this.Value.details != null))
                {
                    int result = 0;
                    int.TryParse(this.Value.details.length, out result);
                    return result;
                }
                return 0;
            }
        }

        public string SearchLength
        {
            get
            {
                if (((this.Value != null) && !this.IsChannel) && !this.IsEvent)
                {
                    if (this.IsVideo)
                    {
                        DateTime time;
                        DateTime.TryParse(this.Value.details.length, out time);
                        if (time.Hour > 0)
                        {
                            return time.ToString("HH:mm:ss", new CultureInfo("DE-de").DateTimeFormat);
                        }
                        return time.ToString("mm:ss", new CultureInfo("DE-de").DateTimeFormat);
                    }
                    if (this.Value.type != type.einzelsendung)
                    {
                        return ConfigurationHelper.GetDictionaryKeyValue("titel_label-" + this.Value.type).Replace("%(ANZAHL)", this.Value.details.length);
                    }
                    if (this.Value.details != null)
                    {
                        string s = this.Value.details.length.Replace("min", "").Trim();
                        int result = 0;
                        int.TryParse(s, out result);
                        TimeSpan span = new TimeSpan(0, result, 0);
                        if (span.Hours > 0)
                        {
                            return (span.Hours.ToString("00") + ":" + span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00"));
                        }
                        return (span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00"));
                    }
                }
                return "";
            }
        }

        public string ShortTitle
        {
            get
            {
                if ((this.Value != null) && (this.Value.information != null))
                {
                    return HttpUtility.HtmlDecode(this.Value.information.shortTitle);
                }
                return "";
            }
        }

        public ZDFMediathek2009.Code.TeaserListChoiceType TeaserListChoiceType
        {
            get
            {
                return this._teaserListChoiceType;
            }
            set
            {
                this._teaserListChoiceType = value;
            }
        }

        public string Title
        {
            get
            {
                if ((this.Value != null) && (this.Value.information != null))
                {
                    return HttpUtility.HtmlDecode(this.Value.information.title);
                }
                return "";
            }
        }

        public string Type
        {
            get
            {
                if (this.Value != null)
                {
                    return this.Value.type.ToString().ToUpper();
                }
                return "";
            }
        }

        public teaser Value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
            }
        }

        public TimeSpan VideoLength
        {
            get
            {
                if (((this.Value != null) && (this.Value.type == type.video)) && (this.Value.details != null))
                {
                    TimeSpan span;
                    TimeSpan.TryParse(this.Value.details.length, out span);
                    return span;
                }
                return new TimeSpan();
            }
        }
    }
}

