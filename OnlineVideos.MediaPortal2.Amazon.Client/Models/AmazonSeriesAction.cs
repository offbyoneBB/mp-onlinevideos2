#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.UiComponents.Media.Actions;

namespace Amazon.Client.Models
{
  public class AmazonSeriesAction : VisibilityDependsOnServerConnectStateAction
  {
    #region Consts

    public const string AMAZON_CONTRIBUTOR_MODEL_ID_STR = "91748B7A-C80F-4B45-A013-AD67A34627D0";

    public static readonly Guid AMAZON_CONTRIBUTOR_MODEL_ID = new Guid(AMAZON_CONTRIBUTOR_MODEL_ID_STR);

    public const string RES_AMAZON_MENU_ITEM = "[AmazonClient.SeriesMenuItem]";

    #endregion

    public AmazonSeriesAction() :
      base(true, AmazonConsts.WF_AMAZON_SERIES_NAVIGATION_ROOT_STATE, RES_AMAZON_MENU_ITEM) { }
  }
}
