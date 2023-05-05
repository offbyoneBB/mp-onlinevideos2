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

using System;
using MediaPortal.UI.Presentation.Models;

namespace OnlineVideos.MediaPortal2.Models
{
    public class OnlineVideosListModel : BaseContentListModel
    {
        #region Consts

        // Global ID definitions and references
        public const string OV_LIST_MODEL_ID_STR = "AFD048F1-9EBB-4EBC-84C8-B27B561B77D0";

        // ID variables
        public static readonly Guid OV_LIST_MODEL_ID = new Guid(OV_LIST_MODEL_ID_STR);

        #endregion


        public OnlineVideosListModel() : base("/Content/OnlineVideosListProviders") { }
    }
}
