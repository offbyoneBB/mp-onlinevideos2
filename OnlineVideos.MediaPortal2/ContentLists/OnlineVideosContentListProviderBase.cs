#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com
    This file is part of MediaPortal 2
    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ContentLists;
using MediaPortal.UI.Presentation.Workflow;
using OnlineVideos.Sites;

namespace OnlineVideos.MediaPortal2.ContentLists
{
    public abstract class OnlineVideosContentListProviderBase : IContentListProvider
    {
        protected readonly ItemsList _allItems;

        protected OnlineVideosContentListProviderBase()
        {
            _allItems = new ItemsList();
        }

        public ItemsList AllItems
        {
            get { return _allItems; }
        }

        public abstract Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects);

        protected async Task<UsageStatistics> GetSiteStats()
        {
            IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
            if (userManagement.UserProfileDataManagement == null) return null;
            UsageStatistics stats = await userManagement.UserProfileDataManagement.GetFeatureUsageStatisticsAsync(userManagement.CurrentUser.ProfileId, "onlinevideos");
            return stats;
        }
        protected async Task<UsageStatistics> GetSiteLanguageStats()
        {
            IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
            if (userManagement.UserProfileDataManagement == null) return null;
            UsageStatistics stats = await userManagement.UserProfileDataManagement.GetFeatureUsageStatisticsAsync(userManagement.CurrentUser.ProfileId, "onlinevideos-lang");
            return stats;
        }

        protected void GetSites(IList<string> sites, ItemsList sitesList)
        {
            if (!OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
            {
                while (!OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
                    System.Threading.Thread.Sleep(50);
            }
            sitesList.Clear();
            var converter = new SiteUtilIconConverter();
            foreach (string siteName in sites)
            {
                foreach (var site in OnlineVideoSettings.Instance.SiteUtilsList)
                {
                    SiteUtilBase siteUtil = site.Value;
                    if (siteUtil.Settings.Name == siteName && siteUtil.Settings.IsEnabled &&
                        (!siteUtil.Settings.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                    {
                        var item = new SiteViewModel(siteUtil);
                        item.Command = new AsyncMethodDelegateCommand(() => GotoSite(item));
                        if (converter.Convert(siteUtil, null, null, null, out object imageUrl))
                            item.Thumb = imageUrl as string;
                        sitesList.Add(item);
                        break;
                    }
                }
            }
            sitesList.FireChange();
        }

        protected void GetSiteGroups(IList<string> languages, ItemsList sitesList)
        {
            if (!OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
            {
                while (!OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
                    System.Threading.Thread.Sleep(50);
            }
            sitesList.Clear();
            foreach (string languageCode in languages)
            {
                var item = new SiteGroupViewModel(
                    LanguageCodeLocalizedConverter.GetLanguageInUserLocale(languageCode),
                    string.Format(@"LanguageFlagsBig\{0}.png", languageCode),
                    GetSitesByLanguage(languageCode));
                item.Command = new AsyncMethodDelegateCommand(() => GotoSiteGroup(item));

                sitesList.Add(item);
            }
            sitesList.FireChange();
        }

        protected List<string> GetSitesByLanguage(string languageCode)
        {
            List<string> siteNames = new List<string>();
            foreach (var site in OnlineVideoSettings.Instance.SiteUtilsList)
            {
                SiteUtilBase siteUtil = site.Value;
                if (siteUtil.Settings.Language == languageCode && siteUtil.Settings.IsEnabled &&
                    (!siteUtil.Settings.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                {
                    siteNames.Add(siteUtil.Settings.Name);
                }
            }
            return siteNames;
        }

        protected Task GotoSite(SiteViewModel item)
        {
            OnlineVideosWorkflowModel model = ServiceRegistration.Get<IWorkflowManager>().GetModel(OnlineVideosWorkflowModel.WF_MODEL_ID) as OnlineVideosWorkflowModel;
            if (model != null)
            {
                model.SelectSite(item);
            }
            return Task.CompletedTask;
        }

        protected Task GotoSiteGroup(SiteGroupViewModel item)
        {
            OnlineVideosWorkflowModel model = ServiceRegistration.Get<IWorkflowManager>().GetModel(OnlineVideosWorkflowModel.WF_MODEL_ID) as OnlineVideosWorkflowModel;
            if (model != null)
            {
                model.SelectSiteGroup(item);
            }
            return Task.CompletedTask;
        }
    }

    public class FavoriteOnlineVideoSitesListProvider : OnlineVideosContentListProviderBase
    {
        public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects)
        {
            var stats = await GetSiteStats();
            if (stats == null)
                return false;
            GetSites(stats.TopUsed.Select(t => t.Name).ToList(), AllItems);
            return true;
        }
    }

    public class LatestOnlineVideoSitesListProvider : OnlineVideosContentListProviderBase
    {
        public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects)
        {
            var stats = await GetSiteStats();
            if (stats == null)
                return false;
            GetSites(stats.LastUsed.Select(t => t.Name).ToList(), AllItems);
            return true;
        }
    }

    public class TopLanguagesOnlineVideoSitesListProvider : OnlineVideosContentListProviderBase
    {
        public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects)
        {
            var stats = await GetSiteLanguageStats();
            if (stats == null)
                return false;
            GetSiteGroups(stats.TopUsed.Select(t => t.Name).ToList(), AllItems);
            return true;
        }
    }
}
