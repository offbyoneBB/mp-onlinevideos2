using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;

namespace OnlineVideos.MediaPortal2
{
    public class VideoViewModel : ListItem
    {
		protected PropertyChangedDelegator eventDelegator = null;

        protected AbstractProperty _titleProperty;
        public AbstractProperty TitleProperty { get { return _titleProperty; } }
        public string Title
        {
            get { return (string)_titleProperty.GetValue(); }
            set { _titleProperty.SetValue(value); }
        }

        protected AbstractProperty _title2Property;
        public AbstractProperty Title2Property { get { return _title2Property; } }
        public string Title2
        {
            get { return (string)_title2Property.GetValue(); }
            set { _title2Property.SetValue(value); }
        }

        protected AbstractProperty _descriptionProperty;
        public AbstractProperty DescriptionProperty { get { return _descriptionProperty; } }
        public string Description
        {
            get { return (string)_descriptionProperty.GetValue(); }
            set { _descriptionProperty.SetValue(value); }
        }

        protected AbstractProperty _lengthProperty;
        public AbstractProperty LengthProperty { get { return _lengthProperty; } }
        public string Length
        {
            get { return (string)_lengthProperty.GetValue(); }
            set { _lengthProperty.SetValue(value); }
        }

		protected AbstractProperty _airdateProperty;
		public AbstractProperty AirdateProperty { get { return _airdateProperty; } }
		public string Airdate
		{
			get { return (string)_airdateProperty.GetValue(); }
			set { _airdateProperty.SetValue(value); }
		}

        protected AbstractProperty _thumbnailImageProperty;
        public AbstractProperty ThumbnailImageProperty { get { return _thumbnailImageProperty; } }
        public string ThumbnailImage
        {
            get { return (string)_thumbnailImageProperty.GetValue(); }
            set { _thumbnailImageProperty.SetValue(value); }
        }

        public VideoInfo VideoInfo { get; protected set; }
		public Category Category { get; protected set; }
		public string SiteName { get; protected set; }
		public string SiteUtilName { get; protected set; }
		public bool IsDetailsVideo { get; protected set; }

        public VideoViewModel(string title, string thumbImage)
			: base(Consts.KEY_NAME, title)
        {
            _titleProperty = new WProperty(typeof(string), title);
            _thumbnailImageProperty = new WProperty(typeof(string), thumbImage);
        }

        public VideoViewModel(VideoInfo videoInfo, Category category, string siteName, string utilName, bool isDetailsVideo)
            : base(Consts.KEY_NAME, !string.IsNullOrEmpty(videoInfo.Title2) ? videoInfo.Title2 : videoInfo.Title)
        {
            VideoInfo = videoInfo;
			Category = category;
			SiteName = siteName;
			SiteUtilName = utilName;
			IsDetailsVideo = isDetailsVideo;

            _titleProperty = new WProperty(typeof(string), videoInfo.Title);
            _title2Property = new WProperty(typeof(string), videoInfo.Title2);
            _descriptionProperty = new WProperty(typeof(string), videoInfo.Description);
            _lengthProperty = new WProperty(typeof(string), videoInfo.Length);
			_airdateProperty = new WProperty(typeof(string), videoInfo.Airdate);
            _thumbnailImageProperty = new WProperty(typeof(string), videoInfo.ThumbnailImage);

			_contextMenuEntriesProperty = new WProperty(typeof(ItemsList), null);

			eventDelegator = OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(PropertyChangedDelegator).Assembly.FullName, typeof(PropertyChangedDelegator).FullName) as PropertyChangedDelegator;
			eventDelegator.InvokeTarget = new PropertyChangedExecutor()
			{
				InvokeHandler = (s, e) =>
				{
					if (e.PropertyName == "ThumbnailImage") ThumbnailImage = (s as VideoInfo).ThumbnailImage;
					else if (e.PropertyName == "Length") Length = (s as VideoInfo).Length;
				}
			};
			VideoInfo.PropertyChanged += eventDelegator.EventDelegate;
        }

		public void ChoosePlaybackOptions(string defaultUrl, Action<string> resultHandler, bool skipDialog = false)
		{
			// with no options set, return the VideoUrl field
			if (VideoInfo.PlaybackOptions == null || VideoInfo.PlaybackOptions.Count == 0)
				resultHandler(defaultUrl);
			// with just one option set, resolve it and call handler
			else if (VideoInfo.PlaybackOptions.Count == 1)
			{
				BackgroundTask.Instance.Start<string>(
					() =>
					{
						return VideoInfo.GetPlaybackOptionUrl(VideoInfo.PlaybackOptions.First().Key);
					},
					(success, url) =>
					{
						if (success)
							resultHandler(url);
					},
					"[OnlineVideos.GettingPlaybackUrlsForVideo]");
			}
			else
			{
				if (skipDialog)
				{
					var defaultOption = VideoInfo.PlaybackOptions.FirstOrDefault(p => p.Value == defaultUrl).Key;
					if (string.IsNullOrEmpty(defaultOption)) defaultOption = VideoInfo.PlaybackOptions.First().Key;
					BackgroundTask.Instance.Start<string>(
						() =>
						{
							return VideoInfo.GetPlaybackOptionUrl(defaultOption);
						},
						(success, url) =>
						{
							if (success)
								resultHandler(url);
						},
						"[OnlineVideos.GettingPlaybackUrlsForVideo]");
				}
				else
				{
					var playbackOptionsItems = new ItemsList();
					foreach (var item in VideoInfo.PlaybackOptions)
					{
						var listItem = new ListItem(Consts.KEY_NAME, item.Key);
						listItem.AdditionalProperties.Add(Constants.KEY_HANDLER, resultHandler);
						listItem.Selected = item.Value == defaultUrl;
						playbackOptionsItems.Add(listItem);
					}

					ServiceRegistration.Get<IWorkflowManager>().NavigatePushTransient(
						WorkflowState.CreateTransientState("PlaybackOptions", VideoInfo.Title, true, "ovsDialogGenericItems", false, WorkflowType.Dialog),
						new NavigationContextConfig()
						{
							AdditionalContextVariables = new Dictionary<string, object>
						{
							{ Constants.CONTEXT_VAR_ITEMS, playbackOptionsItems },
							{ Constants.CONTEXT_VAR_COMMAND, new CommandContainer<ListItem>(SelectPlaybackOption) }
						}
						});
				}
			}
		}

		void SelectPlaybackOption(ListItem option)
		{
			var resultHandler = (Action<string>)option.AdditionalProperties[Constants.KEY_HANDLER];
			BackgroundTask.Instance.Start<string>(
				() =>
				{
					return VideoInfo.GetPlaybackOptionUrl(option[Consts.KEY_NAME]);
				},
				(success, url) =>
				{
					if (success)
						resultHandler(url);
				},
				"[OnlineVideos.GettingPlaybackUrlsForVideo]");
		}

		public void Play(List<string> urls)
		{
			Utils.RemoveInvalidUrls(urls);
			if (urls != null && urls.Count > 0)
			{
				if (urls.Count == 1)
					MediaPortal.UiComponents.Media.Models.PlayItemsModel.PlayItem(new PlaylistItem(this, urls[0]));
				else
					MediaPortal.UiComponents.Media.Models.PlayItemsModel.PlayItems(
						new MediaPortal.UiComponents.Media.Models.GetMediaItemsDlgt(() =>
						{
							return new List<MediaItem>(urls.ConvertAll<MediaItem>(u => new PlaylistItem(this, u)));
						}),
						MediaPortal.UI.Presentation.Players.AVType.Video);
			}
			else
			{
				// todo : show dialog that no valid urls were found
			}
		}

		#region Context Menu

		protected AbstractProperty _contextMenuEntriesProperty;
		public AbstractProperty ContextMenuEntriesProperty
		{
			get
			{
				if (!_contextMenuEntriesProperty.HasValue()) // create entries upon first use
					_contextMenuEntriesProperty.SetValue(CreateContextMenuEntries());
				return _contextMenuEntriesProperty;
			}
		}
		public ItemsList ContextMenuEntries
		{
			get { return (ItemsList)_contextMenuEntriesProperty.GetValue(); }
			set { _contextMenuEntriesProperty.SetValue(value); }
		}

		ItemsList CreateContextMenuEntries()
		{
			var site = OnlineVideoSettings.Instance.SiteUtilsList[SiteName];
			var ctxEntries = new ItemsList();
			if (VideoInfo != null)
			{
				if (SiteUtilName != "DownloadedVideo" && (IsDetailsVideo || !(VideoInfo.HasDetails & site is IChoice)))
				{
					ctxEntries.Add(
						new ListItem(Consts.KEY_NAME, string.Format("{0} ({1})", Translation.Instance.Download, Translation.Instance.Concurrent))
						{
							Command = new MethodDelegateCommand(() => DownloadConcurrent())
						});
					ctxEntries.Add(
						new ListItem(Consts.KEY_NAME, string.Format("{0} ({1})", Translation.Instance.Download, Translation.Instance.Queued))
						{
							Command = new MethodDelegateCommand(() => DownloadQueued())
						});
				}
				foreach (var entry in site.GetContextMenuEntries(Category, VideoInfo))
				{
					var entry_for_closure = entry;
					var item = new ListItem(Consts.KEY_NAME, entry.DisplayText);
					item.Command = new MethodDelegateCommand(() => HandleCustomContextMenuEntry(entry_for_closure));
					ctxEntries.Add(item);
				}
			}
			return ctxEntries;
		}

		void DownloadConcurrent()
		{
			ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
			SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(VideoInfo, Category, OnlineVideoSettings.Instance.SiteUtilsList[SiteName])));
		}

		void DownloadQueued()
		{
			ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
			SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(VideoInfo, Category, OnlineVideoSettings.Instance.SiteUtilsList[SiteName])), true);
		}

		void HandleCustomContextMenuEntry(Sites.ContextMenuEntry entry)
		{
			ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
			switch (entry.Action)
			{
				case Sites.ContextMenuEntry.UIAction.Execute:
					ExecuteCustomContextMenuEntry(entry);
					break;
				case Sites.ContextMenuEntry.UIAction.GetText:
					//todo: show input dialog and execute when confirmed
					//entry.UserInputText = text;
					//ExecuteCustomContextMenuEntry(entry);
					break;
				case Sites.ContextMenuEntry.UIAction.PromptYesNo:
					var dialogHandleId = ServiceRegistration.Get<IDialogManager>().ShowDialog(entry.DisplayText, entry.PromptText, DialogType.YesNoDialog, false, DialogButtonType.No);
					var dialogCloseWatcher = new DialogCloseWatcher(this, dialogHandleId, (dialogResult) =>
					{
						if (dialogResult == DialogResult.Yes)
						{
							ExecuteCustomContextMenuEntry(entry);
						}
					});
					break;
				case Sites.ContextMenuEntry.UIAction.ShowList:
					var menuItems = new ItemsList();
					foreach (var item in entry.SubEntries)
					{
						var listItem = new ListItem(Consts.KEY_NAME, item.DisplayText);
						listItem.AdditionalProperties.Add(Consts.KEY_MEDIA_ITEM, item);
						menuItems.Add(listItem);
					}
					ServiceRegistration.Get<IWorkflowManager>().NavigatePushTransient(
						WorkflowState.CreateTransientState("CustomContextItems", entry.DisplayText, true, "ovsDialogGenericItems", false, WorkflowType.Dialog),
						new NavigationContextConfig()
						{
							AdditionalContextVariables = new Dictionary<string, object>
							{
								{ Constants.CONTEXT_VAR_ITEMS, menuItems },
								{ Constants.CONTEXT_VAR_COMMAND, new CommandContainer<ListItem>((li)=>HandleCustomContextMenuEntry(li.AdditionalProperties[Consts.KEY_MEDIA_ITEM] as Sites.ContextMenuEntry)) }
							}
						});

					break;
			}
		}

		void ExecuteCustomContextMenuEntry(Sites.ContextMenuEntry entry)
		{
			var ovMainModel = ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelOV) as OnlineVideosWorkflowModel;
			var site = OnlineVideoSettings.Instance.SiteUtilsList[SiteName];
			BackgroundTask.Instance.Start<OnlineVideos.Sites.ContextMenuExecutionResult>(
				() =>
				{
					return site.ExecuteContextMenuEntry(Category, VideoInfo, entry);
				},
				(success, result) =>
				{
					if (success)
					{
						if (!string.IsNullOrEmpty(result.ExecutionResultMessage))
						{
							//todo: show message - but also execute the two following if statements
						}
						if (result.RefreshCurrentItems)
						{
							// discover and show videos of this category
							BackgroundTask.Instance.Start<List<VideoInfo>>(
								() =>
								{
									return site.getVideoList(Category);
								},
								(success2, videos) =>
								{
									if (success2)
									{
										ovMainModel.VideosList.Clear();
										videos.ForEach(r => { r.CleanDescriptionAndTitle(); ovMainModel.VideosList.Add(new VideoViewModel(r, Category, SiteName, SiteUtilName, false)); });
										if (site.HasNextPage) ovMainModel.VideosList.Add(new VideoViewModel(Translation.Instance.NextPage, "NextPage.png"));
										ovMainModel.VideosList.FireChange();
										ImageDownloader.GetImages<VideoInfo>(videos);
									}
								});
						}
						if (result.ResultItems != null && result.ResultItems.Count > 0)
						{
							ovMainModel.ShowSearchResults(result.ResultItems, entry.DisplayText);
						}
					}
				},
				entry.DisplayText);
		}

		#endregion

		static void SaveVideo_Step1(DownloadList saveItems, bool? enque = false)
		{
			if (enque != null)
			{
				if (DownloadManager.Instance.Contains(saveItems.CurrentItem))
				{
					// when the DownloadManager already contains the current DownloadInfo of the given list - show already downloading message
					ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", "[OnlineVideos.AlreadyDownloading]", DialogType.OkDialog, false, DialogButtonType.Ok);
					return;
				}

				// check if there is already a download running from this site - yes? -> enque | no -> start now
				if (enque == true && DownloadManager.Instance.Contains(saveItems.CurrentItem.Util.Settings.Name))
				{
					DownloadManager.Instance.Add(saveItems.CurrentItem.Util.Settings.Name, saveItems);
					return;
				}
			}

			try
			{
				if (!string.IsNullOrEmpty(saveItems.CurrentItem.Url))
				{
					var result = saveItems.CurrentItem.Util.getPlaylistItemUrl(saveItems.CurrentItem.VideoInfo, saveItems.ChosenPlaybackOption);
					SaveVideo_Step2(saveItems, new List<string>() { result }, enque);
				}
				else
				{
					var result = saveItems.CurrentItem.Util.getMultipleVideoUrls(saveItems.CurrentItem.VideoInfo);
					SaveVideo_Step2(saveItems, result, enque);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Error getting Urls for Download: '{0}'", ex.Message);
				ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", "[OnlineVideos.GettingPlaybackUrlsForVideo]", DialogType.OkDialog, false, DialogButtonType.Ok);
			}
		}
		
		static void SaveVideo_Step2(DownloadList saveItems, List<String> urls, bool? enque)
		{
			Utils.RemoveInvalidUrls(urls);

			// if no valid urls were returned show error msg
			if (urls == null || urls.Count == 0)
			{				
				ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", "[OnlineVideos.UnableToDownloadVideo]", DialogType.OkDialog, false, DialogButtonType.Ok);
				return;
			}

			// create download list if more than one url
			if (urls.Count > 1)
			{
				saveItems.DownloadItems = new List<DownloadInfo>();
				foreach (string url in urls)
				{
					VideoInfo vi = saveItems.CurrentItem.VideoInfo.CloneForPlayList(url, url == urls[0]);
					string url_new = url;
					if (url == urls[0])
					{
						url_new = saveItems.CurrentItem.Util.getPlaylistItemUrl(vi, string.Empty);
					}
					DownloadInfo pli = DownloadInfo.Create(vi, saveItems.CurrentItem.Category, saveItems.CurrentItem.Util);
					pli.Title = string.Format("{0} - {1} / {2}", vi.Title, (saveItems.DownloadItems.Count + 1).ToString(), urls.Count);
					pli.Url = url_new;
					pli.OverrideFolder = saveItems.CurrentItem.OverrideFolder;
					pli.OverrideFileName = saveItems.CurrentItem.OverrideFileName;
					saveItems.DownloadItems.Add(pli);
				}
				// make the first item the current to be saved now
				saveItems.CurrentItem = saveItems.DownloadItems[0];
				urls = new List<string>(new string[] { saveItems.CurrentItem.Url });
			}
			// show selection dialog for playback options
			VideoViewModel tempVi = new VideoViewModel(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Category, saveItems.CurrentItem.Util.Settings.Name, saveItems.CurrentItem.Util.Settings.UtilName, false);
			tempVi.ChoosePlaybackOptions(urls[0], (url) => { SaveVideo_Step3(saveItems, url, enque); }, enque == null); // skip dialog when downloading an item of a queue
		}
		
		static void SaveVideo_Step3(DownloadList saveItems, string url, bool? enque)
		{
			// check for valid url and cut off additional parameter
			if (String.IsNullOrEmpty(url) ||
				!Utils.IsValidUri((url.IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator) > 0) ? url.Substring(0, url.IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator)) : url))
			{
				ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", "[OnlineVideos.UnableToDownloadVideo]", DialogType.OkDialog, false, DialogButtonType.Ok);
				return;
			}

			saveItems.CurrentItem.Url = url;
			if (string.IsNullOrEmpty(saveItems.CurrentItem.Title)) saveItems.CurrentItem.Title = saveItems.CurrentItem.VideoInfo.Title;

			if (!string.IsNullOrEmpty(saveItems.CurrentItem.OverrideFolder))
			{
				if (!string.IsNullOrEmpty(saveItems.CurrentItem.OverrideFileName))
					saveItems.CurrentItem.LocalFile = Path.Combine(saveItems.CurrentItem.OverrideFolder, saveItems.CurrentItem.OverrideFileName);
				else
					saveItems.CurrentItem.LocalFile = Path.Combine(saveItems.CurrentItem.OverrideFolder, saveItems.CurrentItem.Util.GetFileNameForDownload(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Category, url));
			}
			else
			{
				saveItems.CurrentItem.LocalFile = Path.Combine(Path.Combine(OnlineVideoSettings.Instance.DownloadDir, saveItems.CurrentItem.Util.Settings.Name), saveItems.CurrentItem.Util.GetFileNameForDownload(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Category, url));
			}

			if (saveItems.DownloadItems != null && saveItems.DownloadItems.Count > 1)
			{
				saveItems.CurrentItem.LocalFile = string.Format(@"{0}\{1} - {2}#{3}{4}",
					Path.GetDirectoryName(saveItems.CurrentItem.LocalFile),
					Path.GetFileNameWithoutExtension(saveItems.CurrentItem.LocalFile),
					(saveItems.DownloadItems.IndexOf(saveItems.CurrentItem) + 1).ToString().PadLeft((saveItems.DownloadItems.Count).ToString().Length, '0'),
					(saveItems.DownloadItems.Count).ToString(),
					Path.GetExtension(saveItems.CurrentItem.LocalFile));
			}

			saveItems.CurrentItem.LocalFile = Utils.GetNextFileName(saveItems.CurrentItem.LocalFile);
			saveItems.CurrentItem.ThumbFile = string.IsNullOrEmpty(saveItems.CurrentItem.VideoInfo.ThumbnailImage) ? saveItems.CurrentItem.VideoInfo.ImageUrl : saveItems.CurrentItem.VideoInfo.ThumbnailImage;

			// make sure the target dir exists
			if (!(Directory.Exists(Path.GetDirectoryName(saveItems.CurrentItem.LocalFile))))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(saveItems.CurrentItem.LocalFile));
			}

			if (enque == true)
				DownloadManager.Instance.Add(saveItems.CurrentItem.Util.Settings.Name, saveItems);
			else if (enque == false)
				DownloadManager.Instance.Add(null, saveItems);

			System.Threading.Thread downloadThread = new System.Threading.Thread((System.Threading.ParameterizedThreadStart)delegate(object o)
			{
				DownloadList dlList = o as DownloadList;
				try
				{
					IDownloader dlHelper = null;
					if (dlList.CurrentItem.Url.ToLower().StartsWith("mms://")) dlHelper = new MMSDownloader();
					else dlHelper = new MPUrlSourceFilter.MPUrlSourceFilterDownloader();
					dlList.CurrentItem.Downloader = dlHelper;
					dlList.CurrentItem.Start = DateTime.Now;
					Log.Info("Starting download of '{0}' to '{1}' from Site '{2}'", dlList.CurrentItem.Url, dlList.CurrentItem.LocalFile, dlList.CurrentItem.Util.Settings.Name);
					Exception exception = dlHelper.Download(dlList.CurrentItem);
					if (exception != null) Log.Warn("Error downloading '{0}', Msg: {1}", dlList.CurrentItem.Url, exception.Message);
					SaveSubtitles(dlList.CurrentItem.VideoInfo, Path.ChangeExtension(dlList.CurrentItem.LocalFile, ".srt"));
					OnDownloadFileCompleted(dlList, exception);
				}
				catch (System.Threading.ThreadAbortException)
				{
					// the thread was aborted on purpose, let it finish gracefully
					System.Threading.Thread.ResetAbort();
				}
				catch (Exception ex)
				{
					Log.Warn("Error downloading '{0}', Msg: {1}", dlList.CurrentItem.Url, ex.Message);
					OnDownloadFileCompleted(dlList, ex);
				}
			});
			downloadThread.IsBackground = true;
			downloadThread.Name = "OVDownload";
			downloadThread.Start(saveItems);

			/* // todo : show download started notification?
			GUIDialogNotify dlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
			if (dlgNotify != null)
			{
				dlgNotify.Reset();
				dlgNotify.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
				dlgNotify.SetHeading(Translation.Instance.DownloadStarted);
				dlgNotify.SetText(saveItems.CurrentItem.Title);
				dlgNotify.DoModal(GUIWindowManager.ActiveWindow);
			}*/
		}
		
		static void SaveSubtitles(VideoInfo video, string destinationFileName)
		{
			Uri subtitleUri = null;
			bool validUri = !String.IsNullOrEmpty(video.SubtitleUrl) && Uri.TryCreate(video.SubtitleUrl, UriKind.Absolute, out subtitleUri);

			if (!string.IsNullOrEmpty(video.SubtitleText) || (validUri && !subtitleUri.IsFile))
			{
				Log.Info("Downloading subtitles to " + destinationFileName);
				string subs = string.IsNullOrEmpty(video.SubtitleText) ? Sites.SiteUtilBase.GetWebData(video.SubtitleUrl) : video.SubtitleText;
				if (!string.IsNullOrEmpty(subs))
					File.WriteAllText(destinationFileName, subs, System.Text.Encoding.UTF8);
			}
			else
				if (validUri && subtitleUri.IsFile)
				{
					Log.Info("Downloading subtitles to " + destinationFileName);
					File.Copy(subtitleUri.AbsolutePath, destinationFileName);
				}
		}
		
		static void OnDownloadFileCompleted(DownloadList saveItems, Exception error)
		{
			// notify the Util of the downloaded video that the download has stopped
			try
			{
				if (saveItems.CurrentItem != null && saveItems.CurrentItem.Util != null)
				{
					saveItems.CurrentItem.Util.OnDownloadEnded(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Url, (double)saveItems.CurrentItem.PercentComplete / 100.0d, error != null);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Error on Util.OnDownloadEnded: {0}", ex.ToString());
			}

			bool preventMessageDuetoAdult = (saveItems.CurrentItem.Util != null && saveItems.CurrentItem.Util.Settings.ConfirmAge && OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed);

			if (error != null && !saveItems.CurrentItem.Downloader.Cancelled)
			{
				if (!preventMessageDuetoAdult)
				{
					/* // todo : show download failed notification?
					GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
					if (loDlgNotify != null)
					{
						loDlgNotify.Reset();
						loDlgNotify.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
						loDlgNotify.SetHeading(Translation.Instance.Error);
						loDlgNotify.SetText(string.Format(Translation.Instance.DownloadFailed, saveItems.CurrentItem.Title));
						loDlgNotify.DoModal(GUIWindowManager.ActiveWindow);
					}*/
				}
			}
			else
			{
				try
				{
					// if the image given was an url -> check if thumb exists otherwise download
					if (saveItems.CurrentItem.ThumbFile.ToLower().StartsWith("http"))
					{
						string thumbFile = Utils.GetThumbFile(saveItems.CurrentItem.ThumbFile);
						if (File.Exists(thumbFile)) saveItems.CurrentItem.ThumbFile = thumbFile;
						else if (ImageDownloader.DownloadAndCheckImage(saveItems.CurrentItem.ThumbFile, thumbFile)) saveItems.CurrentItem.ThumbFile = thumbFile;
					}
					// save thumb for this video as well if it exists
					if (!saveItems.CurrentItem.ThumbFile.ToLower().StartsWith("http") && File.Exists(saveItems.CurrentItem.ThumbFile))
					{
						string localImageName = Path.Combine(
							Path.GetDirectoryName(saveItems.CurrentItem.LocalFile),
							Path.GetFileNameWithoutExtension(saveItems.CurrentItem.LocalFile))
							+ Path.GetExtension(saveItems.CurrentItem.ThumbFile);
						File.Copy(saveItems.CurrentItem.ThumbFile, localImageName, true);
					}
				}
				catch (Exception ex)
				{
					Log.Warn("Error saving thumbnail for download: {0}", ex.ToString());
				}

				// get file size
				int fileSize = saveItems.CurrentItem.KbTotal;
				if (fileSize <= 0)
				{
					try { fileSize = (int)((new FileInfo(saveItems.CurrentItem.LocalFile)).Length / 1024); }
					catch { }
				}

				Log.Info("{3} download of '{0}' - {1} KB in {2}", saveItems.CurrentItem.LocalFile, fileSize, (DateTime.Now - saveItems.CurrentItem.Start).ToString(), saveItems.CurrentItem.Downloader.Cancelled ? "Cancelled" : "Finished");

				if (!preventMessageDuetoAdult)
				{
					/* // todo : show download canceled or completed notification?
					GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
					if (loDlgNotify != null)
					{
						loDlgNotify.Reset();
						loDlgNotify.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
						if (saveItems.CurrentItem.Downloader.Cancelled)
							loDlgNotify.SetHeading(Translation.Instance.DownloadCancelled);
						else
							loDlgNotify.SetHeading(Translation.Instance.DownloadComplete);
						loDlgNotify.SetText(string.Format("{0}{1}", saveItems.CurrentItem.Title, fileSize > 0 ? " ( " + fileSize.ToString("n0") + " KB)" : ""));
						loDlgNotify.DoModal(GUIWindowManager.ActiveWindow);
					}*/
				}

				// invoke VideoDownloaded event
				/*if (VideoDownloaded != null)
				{
					try
					{
						VideoDownloaded(saveItems.CurrentItem.LocalFile, saveItems.CurrentItem.Util.Settings.Name, saveItems.CurrentItem.Category != null ? saveItems.CurrentItem.Category.RecursiveName() : "", saveItems.CurrentItem.Title);
					}
					catch (Exception ex)
					{
						Log.Warn("Error invoking external VideoDownloaded event handler: {0}", ex.ToString());
					}
				}*/
			}

			// download the next if list not empty and not last in list and not cancelled by the user
			string site = null;
			if (saveItems.DownloadItems != null && saveItems.DownloadItems.Count > 1 && !saveItems.CurrentItem.Downloader.Cancelled)
			{
				int currentDlIndex = saveItems.DownloadItems.IndexOf(saveItems.CurrentItem);
				if (currentDlIndex >= 0 && currentDlIndex + 1 < saveItems.DownloadItems.Count)
				{
					saveItems.CurrentItem = saveItems.DownloadItems[currentDlIndex + 1];
					SaveVideo_Step1(saveItems, null);
				}
				else
				{
					site = DownloadManager.Instance.Remove(saveItems);
				}
			}
			else
			{
				site = DownloadManager.Instance.Remove(saveItems);
			}

			if (!string.IsNullOrEmpty(site))
			{
				var continuationList = DownloadManager.Instance.GetNext(site);
				if (continuationList != null)
				{
					SaveVideo_Step1(continuationList, null);
				}
			}
		}
	}
}
