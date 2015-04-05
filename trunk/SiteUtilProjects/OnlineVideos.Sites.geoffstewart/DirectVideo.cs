/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 4/1/2013
 * Time: 9:57 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// This is a category for Condensed Games or Highlights where the URL is already known without doing a getVideoList
	/// </summary>
	public class DirectVideo : Category
	{
		public string homeUrl { get; set; }
		public string awayUrl { get; set; }
		
		public DirectVideo()
		{
		}
	}
}
