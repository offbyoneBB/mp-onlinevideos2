/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 2/23/2013
 * Time: 1:54 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OnlineVideos.Sites.geoffstewart;
using OnlineVideos;

namespace OnlineVideos.Sites.geoffstewart
{
	[TestFixture]
	public class TestHockeyStreams
	{
		[Test]
		public void TestMethod()
		{
			HockeyStreams hs = new HockeyStreams();
			hs.setUsername("geoffstewart");
			hs.setPassword("homer");
			
//			System.Collections.Generic.List<VideoInfo> videoList = hs.getVideoList(null);
//			
//			foreach (VideoInfo vi in videoList) {
//				List<string> urls = hs.getMultipleVideoUrls(vi, false);
//				Console.Out.Write(urls);
//			}
			SiteSettings s = new SiteSettings();
			hs.Initialize(s);
			hs.Settings.Categories = new System.ComponentModel.BindingList<Category>();
			
			hs.DiscoverDynamicCategories();
			
			// live games
			hs.DiscoverSubCategories(hs.Settings.Categories[0]);
			
			Category game = hs.Settings.Categories[0].SubCategories[0];
			List<VideoInfo> vidList = hs.GetVideos(game);
			
			
			// archived games
			hs.DiscoverSubCategories(hs.Settings.Categories[1]);
			
			List<VideoInfo> vidList2 = hs.GetVideos(hs.Settings.Categories[1].SubCategories[0]);
			
			List<string> vidList3 = hs.GetMultipleVideoUrls(vidList2[0], false);
		}
	}
}
