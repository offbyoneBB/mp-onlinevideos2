using System;
using System.Reflection;
using System.Windows.Forms;

namespace OnlineVideos.MediaPortal1
{
    public class BindableCheckedListBox : CheckedListBox
    {
        protected override void OnItemCheck(ItemCheckEventArgs ice)
        {
            base.OnItemCheck(ice);

            if (!SettingCheckStatesInternally && ice.NewValue != CheckState.Indeterminate)
            {
                Type t = (((BindingSource)base.DataSource).DataSource).GetType().GetGenericArguments()[0];
                PropertyInfo pi = t.GetProperty(CheckedMember);
                pi.SetValue(base.Items[ice.Index], ice.NewValue == CheckState.Checked, null);
            }
        }

        public string CheckedMember { get; set; }

        protected bool SettingCheckStatesInternally = false;

        protected override void SetItemsCore(System.Collections.IList value)
        {
            base.SetItemsCore(value);

            if (!((BindingSource)base.DataSource).IsBindingSuspended)
            {
                Type t = (((BindingSource)base.DataSource).DataSource).GetType().GetGenericArguments()[0];
                PropertyInfo pi = t.GetProperty(CheckedMember);
                try
                {
                    SettingCheckStatesInternally = true;
                    for (int i = 0; i < value.Count; i++)
                    {
                        base.SetItemChecked(i, (bool)pi.GetValue(value[i], null));
                    }
                }
                finally
                {
                    SettingCheckStatesInternally = false;
                }
            }
        }
    }
}