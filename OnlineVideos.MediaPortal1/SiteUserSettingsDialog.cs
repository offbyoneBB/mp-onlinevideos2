using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using System.IO;
using System.ComponentModel;

namespace OnlineVideos.MediaPortal1
{
    public static class SiteUserSettingsDialog
    {
        public static Sites.SiteUtilBase ShowDialog(Sites.SiteUtilBase selectedSite)
        {
			List<OnlineVideos.Reflection.FieldPropertyDescriptorByRef> actualProps = selectedSite.GetUserConfigurationProperties();
            if (actualProps.Count > 0)
            {
                bool changes = false;
                int selectIndex = 0;
                do
                {
                    int windowId = GUIDialogSiteUserSettings.GUIDIALOGMENU_ONLINEVIDEO; // try our special dialog first
                    GUIDialogMenu dlgSiteOptions = (GUIDialogMenu)GUIWindowManager.GetWindow(windowId) as GUIDialogSiteUserSettings;
                    if (dlgSiteOptions == null || !((GUIDialogSiteUserSettings)dlgSiteOptions).IsAvailable) // if not available use the default one
                    {
                        windowId = (int)GUIWindow.Window.WINDOW_DIALOG_MENU; 
                        dlgSiteOptions = (GUIDialogMenu)GUIWindowManager.GetWindow(windowId);
                    }
                    if (dlgSiteOptions == null) return selectedSite;
                    dlgSiteOptions.Reset();
                    dlgSiteOptions.SetHeading(string.Format("{0}: {1}", selectedSite.Settings.Name, GUILocalizeStrings.Get(5)));
                    foreach (var ovsUserCfg in actualProps)
                    {
                        /*object valueO = ovsUserCfg.GetValue(selectedSite);
                        string value = valueO != null ? valueO.ToString() : string.Empty;*/
						string value = selectedSite.GetConfigValueAsString(ovsUserCfg);
                        if (ovsUserCfg.IsPassword)
                        {
                            value = new string('*', value.Length);
                        }
                        string desc = ovsUserCfg.Description;
                        dlgSiteOptions.Add(new GUIListItem(ovsUserCfg.DisplayName, value, "", false, null) 
                        {
                            // don't set Label3 if we are not using our custom dialog
                            Label3 = windowId == GUIDialogSiteUserSettings.GUIDIALOGMENU_ONLINEVIDEO && !string.IsNullOrEmpty(desc) ? desc : string.Empty 
                        });
                    }
                    dlgSiteOptions.SelectedLabel = selectIndex;
                    dlgSiteOptions.DoModal(GUIWindowManager.ActiveWindow);
                    selectIndex = dlgSiteOptions.SelectedLabel;
                    if (dlgSiteOptions.SelectedId == -1) break;
                    else
                    {
						OnlineVideos.Reflection.FieldPropertyDescriptorByRef prop = actualProps.First(a => a.DisplayName == dlgSiteOptions.SelectedLabelText);
                        if (prop.IsBool)
                        {
                            GUIDialogMenu dlgTrueFalse = (GUIDialogMenu)GUIWindowManager.GetWindow(windowId);
                            dlgTrueFalse.Reset();
                            dlgTrueFalse.SetHeading(string.Format("{0}: {1}", selectedSite.Settings.Name, prop.DisplayName));
                            dlgTrueFalse.Add(true.ToString());
                            dlgTrueFalse.Add(false.ToString());
                            string value = selectedSite.GetConfigValueAsString(prop);
							if (value == false.ToString()) dlgTrueFalse.SelectedLabel = 1;
                            dlgTrueFalse.DoModal(GUIWindowManager.ActiveWindow);
                            if (dlgTrueFalse.SelectedId != -1)
                            {
								if (value != dlgTrueFalse.SelectedLabelText)
                                {
									selectedSite.SetConfigValueFromString(prop, dlgTrueFalse.SelectedLabelText);
                                    changes = true;
                                }
                            }
                        }
                        else if (prop.IsEnum)
                        {
                            GUIDialogMenu dlgEnum = (GUIDialogMenu)GUIWindowManager.GetWindow(windowId);
                            dlgEnum.Reset();
                            dlgEnum.SetHeading(string.Format("{0}: {1}", selectedSite.Settings.Name, prop.DisplayName));
							string value = selectedSite.GetConfigValueAsString(prop);
                            int i = 0;
                            foreach (string e in prop.GetEnumValues())
                            {
                                dlgEnum.Add(e);
                                if (e == value) dlgEnum.SelectedLabel = i;
                                i++;
                            }
                            dlgEnum.DoModal(GUIWindowManager.ActiveWindow);
                            if (dlgEnum.SelectedId != -1)
                            {
                                if (value != dlgEnum.SelectedLabelText)
                                {
									selectedSite.SetConfigValueFromString(prop, dlgEnum.SelectedLabelText);
                                    changes = true;
                                }
                            }
                        }
                        else
                        {
							string value = selectedSite.GetConfigValueAsString(prop);
                            string newValue = (string)value.Clone();
                            if (GUIOnlineVideos.GetUserInputString(ref newValue, prop.IsPassword))
                            {
                                if (value != newValue)
                                {
                                    try
                                    {
										selectedSite.SetConfigValueFromString(prop, newValue);
                                        changes = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        // conversion from string not possible, show error
                                        GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                                        if (dlg_error != null)
                                        {
                                            dlg_error.Reset();
                                            dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
											dlg_error.SetLine(1, Translation.Instance.Error);
                                            dlg_error.SetLine(2, ex.Message);
                                            dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } while (true);

                if (changes)
                {
                    var newUtilInstance = Sites.SiteUtilFactory.CloneFreshSiteFromExisting(selectedSite);
					OnlineVideoSettings.Instance.SiteUtilsList[newUtilInstance.Settings.Name] = newUtilInstance;
					return newUtilInstance;
                }
            }
            return selectedSite;
        }
    }

    public class GUIDialogSiteUserSettings : GUIDialogMenu
    {
        public const int GUIDIALOGMENU_ONLINEVIDEO = 4760;
        public override int GetID { get { return GUIDIALOGMENU_ONLINEVIDEO; } set { } }

        public bool IsAvailable { get; protected set; }

        public override bool Init()
        {
            IsAvailable = File.Exists(GUIGraphicsContext.Skin + @"\myonlinevideos.DialogMenu.xml");
            // 1. MP doesn't actually load the skin yet when calling Load, because SupportsDelayedLoad is true by default
            // 2. returning false from Init won't prevent the Window from being added to the list of Windows, because return value is never evaluated
            // 3. use our IsAvailable Property to find out if the skin (File Exists check) is available
            return Load(GUIGraphicsContext.Skin + @"\myonlinevideos.DialogMenu.xml");
        }

        public override bool OnMessage(GUIMessage message)
        {
            bool result = base.OnMessage(message);

            if (message.Message == GUIMessage.MessageType.GUI_MSG_ITEM_FOCUS_CHANGED && message.TargetWindowId == GUIDIALOGMENU_ONLINEVIDEO && message.SenderControlId == 3)
            {
                if (base.listView.SelectedListItem != null)
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.DialogSelectedItemDescription", base.listView.SelectedListItem.Label3);
                }
                else
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.DialogSelectedItemDescription", string.Empty);
                }
            }

            return result;
        }
    }
}
