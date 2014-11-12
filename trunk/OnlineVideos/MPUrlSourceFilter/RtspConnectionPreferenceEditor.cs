using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents class for RTSP connection preference editor.
    /// </summary>
    public class RtspConnectionPreferenceEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if ((context != null) && (context.Instance != null) && (provider != null))
            {
                IWindowsFormsEditorService  editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (editorService != null)
                {
                    RtspUrlSettings settings = (RtspUrlSettings)context.Instance;
                    RtspConnectionPreference connectionPreference = new RtspConnectionPreference();

                    for (int i = 0; i < 3; i++)
                    {
                        if (i == settings.MulticastPreference)
                        {
                            connectionPreference.listBoxRtspConnectionPreference.Items.Add(OnlineVideoSettings.RtspUdpMulticast);
                            continue;
                        }

                        if (i == settings.UdpPreference)
                        {
                            connectionPreference.listBoxRtspConnectionPreference.Items.Add(OnlineVideoSettings.RtspUdp);
                            continue;
                        }

                        if (i == settings.SameConnectionTcpPreference)
                        {
                            connectionPreference.listBoxRtspConnectionPreference.Items.Add(OnlineVideoSettings.RtspSameConnection);
                            continue;
                        }
                    }

                    editorService.DropDownControl(connectionPreference);

                    try
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            String item = (String)connectionPreference.listBoxRtspConnectionPreference.Items[i];

                            if (item == OnlineVideoSettings.RtspSameConnection)
                            {
                                settings.SameConnectionTcpPreference = i;
                            }
                            if (item == OnlineVideoSettings.RtspUdp)
                            {
                                settings.UdpPreference = i;
                            }
                            if (item == OnlineVideoSettings.RtspUdpMulticast)
                            {
                                settings.MulticastPreference = i;
                            }
                        }
                    }
                    catch { }

                }
            }

            return value;
        }
    }
}
