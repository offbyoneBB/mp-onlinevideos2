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

namespace Amazon.Client
{
  public class AmazonConsts
  {
    public const string MEDIA_NAVIGATION_MODE = "Amazon";

    public static Guid WF_AMAZON_MOVIES_NAVIGATION_ROOT_STATE = new Guid("A7FA34AC-72E2-489E-AFAA-5A9EC9CC84E7");
    public static Guid WF_AMAZON_SERIES_NAVIGATION_ROOT_STATE = new Guid("6D58D0DD-DEE9-4F01-B395-3ADE27F04A79");
  }
}
