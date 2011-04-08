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
            System.ComponentModel.CategoryAttribute attr = new System.ComponentModel.CategoryAttribute("OnlineVideosUserConfiguration");
            var props = ((System.ComponentModel.ICustomTypeDescriptor)selectedSite).GetProperties(new Attribute[] { attr });
            List<System.ComponentModel.PropertyDescriptor> actualProps = new List<System.ComponentModel.PropertyDescriptor>();
            foreach (System.ComponentModel.PropertyDescriptor prop in props) if (prop.Attributes.Contains(attr)) actualProps.Add(prop);
            if (actualProps.Count > 0)
            {
                bool changes = false;
                int selectIndex = 0;
                do
                {
                    GUIDialogMenu dlgSiteOptions = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                    if (dlgSiteOptions == null) return selectedSite;
                    dlgSiteOptions.Reset();
                    dlgSiteOptions.SetHeading(5);
                    foreach (var ovsUserCfg in actualProps)
                    {
                        string value = ovsUserCfg.GetValue(selectedSite).ToString();
                        if (ovsUserCfg.Attributes.Contains(new System.ComponentModel.PasswordPropertyTextAttribute(true)))
                        {
                            value = new string('*', value.Length);
                        }
                        var descAttr = ovsUserCfg.Attributes[typeof(DescriptionAttribute)];
                        dlgSiteOptions.Add(new GUIListItem(ovsUserCfg.DisplayName, value, "", false, null) /*{ Label3 = descAttr != null ? ((DescriptionAttribute)descAttr).Description : string.Empty }*/);
                    }
                    dlgSiteOptions.SelectedLabel = selectIndex;
                    dlgSiteOptions.DoModal(GUIWindowManager.ActiveWindow);
                    selectIndex = dlgSiteOptions.SelectedLabel;
                    if (dlgSiteOptions.SelectedId == -1) break;
                    else
                    {
                        System.ComponentModel.PropertyDescriptor prop = actualProps.First(a => a.DisplayName == dlgSiteOptions.SelectedLabelText);
                        if (prop.PropertyType.Equals(typeof(bool)))
                        {
                            GUIDialogMenu dlgTrueFalse = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                            dlgTrueFalse.Reset();
                            dlgTrueFalse.SetHeading(prop.DisplayName);
                            dlgTrueFalse.Add(true.ToString());
                            dlgTrueFalse.Add(false.ToString());
                            bool value = (bool)prop.GetValue(selectedSite);
                            if (!value) dlgTrueFalse.SelectedLabel = 1;
                            dlgTrueFalse.DoModal(GUIWindowManager.ActiveWindow);
                            if (dlgTrueFalse.SelectedId != -1)
                            {
                                bool newValue = bool.Parse(dlgTrueFalse.SelectedLabelText);
                                if (value != newValue)
                                {
                                    prop.SetValue(selectedSite, newValue);
                                    changes = true;
                                }
                            }
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            GUIDialogMenu dlgEnum = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                            dlgEnum.Reset();
                            dlgEnum.SetHeading(prop.DisplayName);
                            string value = prop.GetValue(selectedSite).ToString();
                            int i = 0;
                            foreach (string e in Enum.GetNames(prop.PropertyType))
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
                                    prop.SetValue(selectedSite, Enum.Parse(prop.PropertyType, dlgEnum.SelectedLabelText));
                                    changes = true;
                                }
                            }
                        }
                        else
                        {
                            string value = prop.GetValue(selectedSite).ToString();
                            string newValue = (string)value.Clone();
                            if (GUIOnlineVideos.GetUserInputString(ref newValue, prop.Attributes.Contains(new System.ComponentModel.PasswordPropertyTextAttribute(true))))
                            {
                                if (value != newValue)
                                {
                                    try
                                    {
                                        object result = Convert.ChangeType(newValue, prop.PropertyType);
                                        prop.SetValue(selectedSite, result);
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
                                            dlg_error.SetLine(1, Translation.Error);
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
                    // create new instance of this site with reset settings
                    SerializableSettings s = new SerializableSettings() { Sites = new BindingList<SiteSettings>() };
                    s.Sites.Add(selectedSite.Settings);
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    Utils.SiteSettingsToXml(s, ms);
                    ms.Position = 0;
                    SiteSettings originalSettings = Utils.SiteSettingsFromXml(new StreamReader(ms))[0];
                    Sites.SiteUtilBase newSite = SiteUtilFactory.CreateFromShortName(selectedSite.Settings.UtilName, originalSettings);
                    OnlineVideoSettings.Instance.SiteUtilsList[originalSettings.Name] = newSite;
                    return newSite;
                }
            }
            return selectedSite;
        }
    }
}
