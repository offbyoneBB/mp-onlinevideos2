using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UiComponents.Media.General;

namespace OnlineVideos.MediaPortal2
{
	public class SiteViewModel : ListItem
    {
		protected AbstractProperty _nameProperty;
		public AbstractProperty NameProperty { get { return _nameProperty; } }
		public string Name
		{
			get { return (string)_nameProperty.GetValue(); }
			set { _nameProperty.SetValue(value); }
		}

		protected AbstractProperty _descriptionProperty;
		public AbstractProperty DescriptionProperty { get { return _descriptionProperty; } }
		public string Description
		{
			get { return (string)_descriptionProperty.GetValue(); }
			set { _descriptionProperty.SetValue(value); }
		}

		protected AbstractProperty _languageProperty;
		public AbstractProperty LanguageProperty { get { return _languageProperty; } }
		public string Language
		{
			get { return (string)_languageProperty.GetValue(); }
			set { _languageProperty.SetValue(value); }
		}

        protected Sites.SiteUtilBase _site;
        public Sites.SiteUtilBase Site
        {
            get { return _site; }
        }

		public void RecreateSite()
		{
			var newUtilInstance = SiteUtilFactory.CloneFreshSiteFromExisting(Site);
			OnlineVideoSettings.Instance.SiteUtilsList[Name] = newUtilInstance;
			_site = newUtilInstance;
			UserSettingsChanged = false;
		}
        
        public SiteViewModel(Sites.SiteUtilBase site) 
			: base(Consts.KEY_NAME, site.Settings.Name)
        {
            _site = site;

			_nameProperty = new WProperty(typeof(string), site.Settings.Name);
			_languageProperty = new WProperty(typeof(string), site.Settings.Language);
			_descriptionProperty = new WProperty(typeof(string), site.Settings.Description);
			_contextMenuEntriesProperty = new WProperty(typeof(ItemsList), null);
			_settingsListProperty = new WProperty(typeof(ItemsList), null);
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
			var ctxEntries = new ItemsList();
			ctxEntries.Add(
				new ListItem(Consts.KEY_NAME, new StringId("[OnlineVideos.RemoveFromMySites]")) 
				{ 
					Command = new MethodDelegateCommand(() => RemoveSite()) 
				});
			if (Site.GetUserConfigurationProperties().Count > 0)
				ctxEntries.Add(
					new ListItem(Consts.KEY_NAME, "Change Settings") 
					{
						Command = new MethodDelegateCommand(() => ConfigureSite()) 
					});
			return ctxEntries;
		}

		void RemoveSite()
		{
			var model = ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelOV) as OnlineVideosWorkflowModel;
			// remove from displayed list
			model.SitesList.Remove(this);
			model.SitesList.FireChange();
			// remove from persisted list and save
			if (OnlineVideoSettings.Instance.RemoveSite(Name))
				OnlineVideoSettings.Instance.SaveSites();

			ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
		}

		void ConfigureSite()
		{
			ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
			// go to settings screen
			IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePush(Guids.WorkflowStateSiteSettings, new NavigationContextConfig() 
			{ 
				NavigationContextDisplayLabel = string.Format("{0} {1}", Name, LocalizationHelper.Translate("[Configuration.Name]"))
			});
		}

		#endregion

		#region Site User Setting

		protected AbstractProperty _settingsListProperty;
		public AbstractProperty SettingsListProperty
		{
			get
			{
				if (!_settingsListProperty.HasValue()) // create entries upon first use
					_settingsListProperty.SetValue(CreateSettingsList());
				return _settingsListProperty;
			}
		}
		public ItemsList SettingsList
		{
			get { return (ItemsList)_settingsListProperty.GetValue(); }
			set { _settingsListProperty.SetValue(value); }
		}

		ItemsList CreateSettingsList()
		{
			var list = new ItemsList();

			foreach (var propDef in Site.GetUserConfigurationProperties())
			{
				list.Add(new SiteSettingViewModel(this, propDef));
			}
			return list;
		}

		public bool UserSettingsChanged { get; set; }

		#endregion
	}
}
