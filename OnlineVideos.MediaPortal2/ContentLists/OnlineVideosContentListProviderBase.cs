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

using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using System.Threading.Tasks;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.ContentLists;
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

        public abstract Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason);

        protected ListItem CreateAppItem(SiteUtilBase siteUtil)
        {
            ListItem item = new ListItem();
            item.AdditionalProperties[Consts.KEY_ID] = siteUtil.ToString(); // TODO
            item.Command = new AsyncMethodDelegateCommand(() => GotoSite(item));
            return item;
        }

        protected Task GotoSite(ListItem item)
        {
            //AppLauncherHomeModel model = ServiceRegistration.Get<IWorkflowManager>().GetModel(Guid.Parse(AppLauncherHomeModel.MODEL_ID_STR)) as AppLauncherHomeModel;
            //if (model != null)
            //{
            //    model.StartApp(item);
            //}
            return Task.CompletedTask;
        }
    }

    public class FavoriteOnlineVideoSitesListProvider : OnlineVideosContentListProviderBase
    {
        public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason)
        {
            return true;
        }
    }
    public class LatestOnlineVideoSitesListProvider : OnlineVideosContentListProviderBase
    {
        public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason)
        {
            return true;
        }
    }
}
