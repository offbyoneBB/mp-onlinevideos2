using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace OnlineVideos.MPUrlSourceFilter
{
    public static class UrlBuilder
    {

        public static String GetFilterUrl(object sourceFilter, Sites.SiteUtilBase siteUtil, String url)
        {
            OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.IFilterState filterState = sourceFilter as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.IFilterState;
            OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.IFilterStateEx filterStateEx = sourceFilter as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.IFilterStateEx;

            if (filterStateEx != null)
            {
                // MediaPortal IPTV filter and url source splitter

                int index = url.IndexOf(OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.SimpleUrl.ParameterSeparator);

                if (index != (-1))
                {
                    OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.SimpleUrl filterUrl = null;
                    OnlineVideos.MPUrlSourceFilter.SimpleUrl simpleUrl = null;

                    String encodedContent = url.Substring(index + OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.SimpleUrl.ParameterSeparator.Length);
                    Byte[] rawContent = Convert.FromBase64String(encodedContent);

                    using (MemoryStream stream = new MemoryStream(rawContent.Length))
                    {
                        stream.Write(rawContent, 0, rawContent.Length);
                        stream.Seek(0, SeekOrigin.Begin);

                        BinaryFormatter serializer = new BinaryFormatter();
                        simpleUrl = serializer.Deserialize(stream) as OnlineVideos.MPUrlSourceFilter.SimpleUrl;
                    }

                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.AfhsManifestUrl(simpleUrl.Uri) : filterUrl;
                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.RtmpUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpUrl(simpleUrl.Uri) : filterUrl;
                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.HttpUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.HttpUrl(simpleUrl.Uri) : filterUrl;
                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.RtspUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtspUrl(simpleUrl.Uri) : filterUrl;
                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.UdpRtpUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.UdpRtpUrl(simpleUrl.Uri) : filterUrl;

                    if (filterUrl == null)
                    {
                        throw new OnlineVideosException(Translation.Instance.UnableToPlayVideo);
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.AfhsManifestUrl afhsFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.AfhsManifestUrl;
                        OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl afhsSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl;

                        afhsFilterUrl.SegmentFragmentUrlExtraParameters = afhsSimpleUrl.SegmentFragmentUrlExtraParameters;
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.HttpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.HttpUrl httpFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.HttpUrl;
                        OnlineVideos.MPUrlSourceFilter.HttpUrl httpSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.HttpUrl;

                        httpFilterUrl.Cookies.Add(httpSimpleUrl.Cookies);
                        httpFilterUrl.IgnoreContentLength = httpSimpleUrl.IgnoreContentLength;
                        httpFilterUrl.Referer = httpSimpleUrl.Referer;
                        httpFilterUrl.UserAgent = httpSimpleUrl.UserAgent;
                        httpFilterUrl.Version = httpSimpleUrl.Version;

                        httpFilterUrl.OpenConnectionSleepTime = siteUtil.HttpSettings.OpenConnectionSleepTime;
                        httpFilterUrl.OpenConnectionTimeout = siteUtil.HttpSettings.OpenConnectionTimeout;
                        httpFilterUrl.TotalReopenConnectionTimeout = siteUtil.HttpSettings.TotalReopenConnectionTimeout;
                        httpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.HttpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.HttpSettings.NetworkInterface : String.Empty;

                        httpFilterUrl.DumpProtocolInputData = siteUtil.HttpSettings.DumpProtocolInputData;
                        httpFilterUrl.DumpProtocolOutputData = siteUtil.HttpSettings.DumpProtocolOutputData;
                        httpFilterUrl.DumpParserInputData = siteUtil.HttpSettings.DumpParserInputData;
                        httpFilterUrl.DumpParserOutputData = siteUtil.HttpSettings.DumpParserOutputData;
                        httpFilterUrl.DumpOutputPinData = siteUtil.HttpSettings.DumpOutputPinData;
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.RtmpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpUrl rtmpFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpUrl;
                        OnlineVideos.MPUrlSourceFilter.RtmpUrl rtmpSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.RtmpUrl;

                        rtmpFilterUrl.App = rtmpSimpleUrl.App;
                        CopyRtmpArbitraryDataV2(rtmpFilterUrl.ArbitraryData, rtmpSimpleUrl.ArbitraryData);
                        rtmpFilterUrl.Auth = rtmpSimpleUrl.Auth;
                        rtmpFilterUrl.BufferTime = rtmpSimpleUrl.BufferTime;
                        rtmpFilterUrl.FlashVersion = rtmpSimpleUrl.FlashVersion;
                        rtmpFilterUrl.Jtv = rtmpSimpleUrl.Jtv;
                        rtmpFilterUrl.Live = rtmpSimpleUrl.Live;
                        rtmpFilterUrl.PageUrl = rtmpSimpleUrl.PageUrl;
                        rtmpFilterUrl.Playlist = rtmpSimpleUrl.Playlist;
                        rtmpFilterUrl.PlayPath = rtmpSimpleUrl.PlayPath;
                        rtmpFilterUrl.Start = rtmpSimpleUrl.Start;
                        rtmpFilterUrl.Stop = rtmpSimpleUrl.Stop;
                        rtmpFilterUrl.Subscribe = rtmpSimpleUrl.Subscribe;
                        rtmpFilterUrl.SwfAge = rtmpSimpleUrl.SwfAge;
                        rtmpFilterUrl.SwfUrl = rtmpSimpleUrl.SwfUrl;
                        rtmpFilterUrl.SwfVerify = rtmpSimpleUrl.SwfVerify;
                        rtmpFilterUrl.TcUrl = rtmpSimpleUrl.TcUrl;
                        rtmpFilterUrl.Token = rtmpSimpleUrl.Token;

                        rtmpFilterUrl.OpenConnectionSleepTime = siteUtil.RtmpSettings.OpenConnectionSleepTime;
                        rtmpFilterUrl.OpenConnectionTimeout = siteUtil.RtmpSettings.OpenConnectionTimeout;
                        rtmpFilterUrl.TotalReopenConnectionTimeout = siteUtil.RtmpSettings.TotalReopenConnectionTimeout;
                        rtmpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.RtmpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.RtmpSettings.NetworkInterface : String.Empty;

                        rtmpFilterUrl.DumpProtocolInputData = siteUtil.RtmpSettings.DumpProtocolInputData;
                        rtmpFilterUrl.DumpProtocolOutputData = siteUtil.RtmpSettings.DumpProtocolOutputData;
                        rtmpFilterUrl.DumpParserInputData = siteUtil.RtmpSettings.DumpParserInputData;
                        rtmpFilterUrl.DumpParserOutputData = siteUtil.RtmpSettings.DumpParserOutputData;
                        rtmpFilterUrl.DumpOutputPinData = siteUtil.RtmpSettings.DumpOutputPinData;
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.RtspUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtspUrl rtspFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtspUrl;
                        OnlineVideos.MPUrlSourceFilter.RtspUrl rtspSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.RtspUrl;

                        rtspFilterUrl.IgnorePayloadType = rtspSimpleUrl.IgnorePayloadType;
                        rtspFilterUrl.MulticastPreference = rtspSimpleUrl.MulticastPreference;
                        rtspFilterUrl.SameConnectionPreference = rtspSimpleUrl.SameConnectionPreference;
                        rtspFilterUrl.UdpPreference = rtspSimpleUrl.UdpPreference;

                        rtspFilterUrl.OpenConnectionSleepTime = siteUtil.RtspSettings.OpenConnectionSleepTime;
                        rtspFilterUrl.OpenConnectionTimeout = siteUtil.RtspSettings.OpenConnectionTimeout;
                        rtspFilterUrl.TotalReopenConnectionTimeout = siteUtil.RtspSettings.TotalReopenConnectionTimeout;
                        rtspFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.RtspSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.RtspSettings.NetworkInterface : String.Empty;
                        rtspFilterUrl.ClientPortMax = siteUtil.RtspSettings.ClientPortMin;
                        rtspFilterUrl.ClientPortMin = siteUtil.RtspSettings.ClientPortMax;

                        rtspFilterUrl.DumpProtocolInputData = siteUtil.RtspSettings.DumpProtocolInputData;
                        rtspFilterUrl.DumpProtocolOutputData = siteUtil.RtspSettings.DumpProtocolOutputData;
                        rtspFilterUrl.DumpParserInputData = siteUtil.RtspSettings.DumpParserInputData;
                        rtspFilterUrl.DumpParserOutputData = siteUtil.RtspSettings.DumpParserOutputData;
                        rtspFilterUrl.DumpOutputPinData = siteUtil.RtspSettings.DumpOutputPinData;
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.UdpRtpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.UdpRtpUrl udpFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.UdpRtpUrl;
                        OnlineVideos.MPUrlSourceFilter.UdpRtpUrl udpSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.UdpRtpUrl;

                        udpFilterUrl.OpenConnectionSleepTime = siteUtil.UdpRtpSettings.OpenConnectionSleepTime;
                        udpFilterUrl.OpenConnectionTimeout = siteUtil.UdpRtpSettings.OpenConnectionTimeout;
                        udpFilterUrl.TotalReopenConnectionTimeout = siteUtil.UdpRtpSettings.TotalReopenConnectionTimeout;
                        udpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.UdpRtpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.UdpRtpSettings.NetworkInterface : String.Empty;
                        udpFilterUrl.ReceiveDataCheckInterval = siteUtil.UdpRtpSettings.ReceiveDataCheckInterval;

                        udpFilterUrl.DumpProtocolInputData = siteUtil.UdpRtpSettings.DumpProtocolInputData;
                        udpFilterUrl.DumpProtocolOutputData = siteUtil.UdpRtpSettings.DumpProtocolOutputData;
                        udpFilterUrl.DumpParserInputData = siteUtil.UdpRtpSettings.DumpParserInputData;
                        udpFilterUrl.DumpParserOutputData = siteUtil.UdpRtpSettings.DumpParserOutputData;
                        udpFilterUrl.DumpOutputPinData = siteUtil.UdpRtpSettings.DumpOutputPinData;
                    }

                    filterUrl.LiveStream = simpleUrl.LiveStream;

                    //filterUrl.CacheFolder
                    //filterUrl.MaximumLogSize
                    //filterUrl.MaximumPlugins
                    //filterUrl.Verbosity

                    return filterUrl.ToString();
                }
                else
                {
                    // create filter url with url factory

                    OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.SimpleUrl simpleUrl = OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.UrlFactory.CreateUrl(url);

                    //if (simpleUrl is OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.AfhsManifestUrl)
                    //{
                    //    OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.AfhsManifestUrl afhsFilterUrl = simpleUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.AfhsManifestUrl;
                    //}

                    if (simpleUrl is OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.HttpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.HttpUrl httpFilterUrl = simpleUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.HttpUrl;

                        httpFilterUrl.OpenConnectionSleepTime = siteUtil.HttpSettings.OpenConnectionSleepTime;
                        httpFilterUrl.OpenConnectionTimeout = siteUtil.HttpSettings.OpenConnectionTimeout;
                        httpFilterUrl.TotalReopenConnectionTimeout = siteUtil.HttpSettings.TotalReopenConnectionTimeout;
                        httpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.HttpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.HttpSettings.NetworkInterface : String.Empty;

                        httpFilterUrl.DumpProtocolInputData = siteUtil.HttpSettings.DumpProtocolInputData;
                        httpFilterUrl.DumpProtocolOutputData = siteUtil.HttpSettings.DumpProtocolOutputData;
                        httpFilterUrl.DumpParserInputData = siteUtil.HttpSettings.DumpParserInputData;
                        httpFilterUrl.DumpParserOutputData = siteUtil.HttpSettings.DumpParserOutputData;
                        httpFilterUrl.DumpOutputPinData = siteUtil.HttpSettings.DumpOutputPinData;
                    }

                    if (simpleUrl is OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpUrl rtmpFilterUrl = simpleUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpUrl;

                        rtmpFilterUrl.OpenConnectionSleepTime = siteUtil.RtmpSettings.OpenConnectionSleepTime;
                        rtmpFilterUrl.OpenConnectionTimeout = siteUtil.RtmpSettings.OpenConnectionTimeout;
                        rtmpFilterUrl.TotalReopenConnectionTimeout = siteUtil.RtmpSettings.TotalReopenConnectionTimeout;
                        rtmpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.RtmpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.RtmpSettings.NetworkInterface : String.Empty;

                        rtmpFilterUrl.DumpProtocolInputData = siteUtil.RtmpSettings.DumpProtocolInputData;
                        rtmpFilterUrl.DumpProtocolOutputData = siteUtil.RtmpSettings.DumpProtocolOutputData;
                        rtmpFilterUrl.DumpParserInputData = siteUtil.RtmpSettings.DumpParserInputData;
                        rtmpFilterUrl.DumpParserOutputData = siteUtil.RtmpSettings.DumpParserOutputData;
                        rtmpFilterUrl.DumpOutputPinData = siteUtil.RtmpSettings.DumpOutputPinData;
                    }

                    if (simpleUrl is OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtspUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtspUrl rtspFilterUrl = simpleUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtspUrl;

                        rtspFilterUrl.OpenConnectionSleepTime = siteUtil.RtspSettings.OpenConnectionSleepTime;
                        rtspFilterUrl.OpenConnectionTimeout = siteUtil.RtspSettings.OpenConnectionTimeout;
                        rtspFilterUrl.TotalReopenConnectionTimeout = siteUtil.RtspSettings.TotalReopenConnectionTimeout;
                        rtspFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.RtspSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.RtspSettings.NetworkInterface : String.Empty;
                        rtspFilterUrl.ClientPortMax = siteUtil.RtspSettings.ClientPortMin;
                        rtspFilterUrl.ClientPortMin = siteUtil.RtspSettings.ClientPortMax;

                        rtspFilterUrl.DumpProtocolInputData = siteUtil.RtspSettings.DumpProtocolInputData;
                        rtspFilterUrl.DumpProtocolOutputData = siteUtil.RtspSettings.DumpProtocolOutputData;
                        rtspFilterUrl.DumpParserInputData = siteUtil.RtspSettings.DumpParserInputData;
                        rtspFilterUrl.DumpParserOutputData = siteUtil.RtspSettings.DumpParserOutputData;
                        rtspFilterUrl.DumpOutputPinData = siteUtil.RtspSettings.DumpOutputPinData;
                    }

                    if (simpleUrl is OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.UdpRtpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.UdpRtpUrl udpFilterUrl = simpleUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.UdpRtpUrl;

                        udpFilterUrl.OpenConnectionSleepTime = siteUtil.UdpRtpSettings.OpenConnectionSleepTime;
                        udpFilterUrl.OpenConnectionTimeout = siteUtil.UdpRtpSettings.OpenConnectionTimeout;
                        udpFilterUrl.TotalReopenConnectionTimeout = siteUtil.UdpRtpSettings.TotalReopenConnectionTimeout;
                        udpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.UdpRtpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.UdpRtpSettings.NetworkInterface : String.Empty;
                        udpFilterUrl.ReceiveDataCheckInterval = siteUtil.UdpRtpSettings.ReceiveDataCheckInterval;

                        udpFilterUrl.DumpProtocolInputData = siteUtil.UdpRtpSettings.DumpProtocolInputData;
                        udpFilterUrl.DumpProtocolOutputData = siteUtil.UdpRtpSettings.DumpProtocolOutputData;
                        udpFilterUrl.DumpParserInputData = siteUtil.UdpRtpSettings.DumpParserInputData;
                        udpFilterUrl.DumpParserOutputData = siteUtil.UdpRtpSettings.DumpParserOutputData;
                        udpFilterUrl.DumpOutputPinData = siteUtil.UdpRtpSettings.DumpOutputPinData;
                    }

                    return (simpleUrl != null) ? simpleUrl.ToString() : url;
                }
            }
            else
            {
                // MediaPortal Url Source Splitter

                int index = url.IndexOf(OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.SimpleUrl.ParameterSeparator);
                if (index != (-1))
                {
                    OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.SimpleUrl filterUrl = null;
                    OnlineVideos.MPUrlSourceFilter.SimpleUrl simpleUrl = null;

                    String encodedContent = url.Substring(index + OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.SimpleUrl.ParameterSeparator.Length);
                    Byte[] rawContent = Convert.FromBase64String(encodedContent);

                    using (MemoryStream stream = new MemoryStream(rawContent.Length))
                    {
                        stream.Write(rawContent, 0, rawContent.Length);
                        stream.Seek(0, SeekOrigin.Begin);

                        BinaryFormatter serializer = new BinaryFormatter();
                        simpleUrl = serializer.Deserialize(stream) as OnlineVideos.MPUrlSourceFilter.SimpleUrl;
                    }

                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.AfhsManifestUrl(simpleUrl.Uri) : filterUrl;
                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.RtmpUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpUrl(simpleUrl.Uri) : filterUrl;
                    filterUrl = ((filterUrl == null) && (simpleUrl is OnlineVideos.MPUrlSourceFilter.HttpUrl)) ? new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.HttpUrl(simpleUrl.Uri) : filterUrl;

                    if (filterUrl == null)
                    {
                        throw new OnlineVideosException(Translation.Instance.UnableToPlayVideo);
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.AfhsManifestUrl afhsFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.AfhsManifestUrl;
                        OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl afhsSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.AfhsManifestUrl;

                        afhsFilterUrl.SegmentFragmentUrlExtraParameters = afhsSimpleUrl.SegmentFragmentUrlExtraParameters;
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.HttpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.HttpUrl httpFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.HttpUrl;
                        OnlineVideos.MPUrlSourceFilter.HttpUrl httpSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.HttpUrl;

                        httpFilterUrl.Cookies.Add(httpSimpleUrl.Cookies);
                        httpFilterUrl.IgnoreContentLength = httpSimpleUrl.IgnoreContentLength;
                        httpFilterUrl.Referer = httpSimpleUrl.Referer;
                        httpFilterUrl.UserAgent = httpSimpleUrl.UserAgent;
                        httpFilterUrl.Version = httpSimpleUrl.Version;

                        httpFilterUrl.OpenConnectionMaximumAttempts = siteUtil.HttpSettings.TotalReopenConnectionTimeout / siteUtil.HttpSettings.OpenConnectionTimeout;
                        httpFilterUrl.ReceiveDataTimeout = siteUtil.HttpSettings.OpenConnectionTimeout;
                        httpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.HttpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.HttpSettings.NetworkInterface : String.Empty;
                    }

                    if (simpleUrl is OnlineVideos.MPUrlSourceFilter.RtmpUrl)
                    {
                        OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpUrl rtmpFilterUrl = filterUrl as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpUrl;
                        OnlineVideos.MPUrlSourceFilter.RtmpUrl rtmpSimpleUrl = simpleUrl as OnlineVideos.MPUrlSourceFilter.RtmpUrl;

                        rtmpFilterUrl.App = rtmpSimpleUrl.App;
                        CopyRtmpArbitraryDataV1(rtmpFilterUrl.ArbitraryData, rtmpSimpleUrl.ArbitraryData);
                        rtmpFilterUrl.Auth = rtmpSimpleUrl.Auth;
                        rtmpFilterUrl.BufferTime = rtmpSimpleUrl.BufferTime;
                        rtmpFilterUrl.FlashVersion = rtmpSimpleUrl.FlashVersion;
                        rtmpFilterUrl.Jtv = rtmpSimpleUrl.Jtv;
                        rtmpFilterUrl.Live = rtmpSimpleUrl.Live;
                        rtmpFilterUrl.PageUrl = rtmpSimpleUrl.PageUrl;
                        rtmpFilterUrl.Playlist = rtmpSimpleUrl.Playlist;
                        rtmpFilterUrl.PlayPath = rtmpSimpleUrl.PlayPath;
                        rtmpFilterUrl.Start = rtmpSimpleUrl.Start;
                        rtmpFilterUrl.Stop = rtmpSimpleUrl.Stop;
                        rtmpFilterUrl.Subscribe = rtmpSimpleUrl.Subscribe;
                        rtmpFilterUrl.SwfAge = rtmpSimpleUrl.SwfAge;
                        rtmpFilterUrl.SwfUrl = rtmpSimpleUrl.SwfUrl;
                        rtmpFilterUrl.SwfVerify = rtmpSimpleUrl.SwfVerify;
                        rtmpFilterUrl.TcUrl = rtmpSimpleUrl.TcUrl;
                        rtmpFilterUrl.Token = rtmpSimpleUrl.Token;

                        rtmpFilterUrl.OpenConnectionMaximumAttempts = siteUtil.RtmpSettings.TotalReopenConnectionTimeout / siteUtil.RtmpSettings.OpenConnectionTimeout;
                        rtmpFilterUrl.ReceiveDataTimeout = siteUtil.RtmpSettings.OpenConnectionTimeout;
                        rtmpFilterUrl.NetworkInterface = (String.CompareOrdinal(siteUtil.RtmpSettings.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? siteUtil.RtmpSettings.NetworkInterface : String.Empty;
                    }

                    filterUrl.LiveStream = simpleUrl.LiveStream;

                    //filterUrl.CacheFolder
                    //filterUrl.MaximumLogSize
                    //filterUrl.MaximumPlugins
                    //filterUrl.Verbosity

                    return filterUrl.ToString();
                }
                else
                {
                    return url;
                }
            }
        }


        private static void CopyRtmpArbitraryDataV1(OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpArbitraryDataCollection destination, OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataCollection source)
        {
            foreach (var arbitraryData in source)
            {
                OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpArbitraryData data = null;

                switch (arbitraryData.DataType)
                {
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Boolean:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpBooleanArbitraryData arbitraryDataBool = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpBooleanArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpBooleanArbitraryData(arbitraryDataBool.Name, arbitraryDataBool.Value);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Number:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpNumberArbitraryData arbitraryDataNumber = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpNumberArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpNumberArbitraryData(arbitraryDataNumber.Name, arbitraryDataNumber.Value);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.String:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpStringArbitraryData arbitraryDataString = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpStringArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpStringArbitraryData(arbitraryDataString.Name, arbitraryDataString.Value);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Object:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpObjectArbitraryData arbitraryDataObject = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpObjectArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpObjectArbitraryData(arbitraryDataObject.Name);

                            CopyRtmpArbitraryDataV1((data as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpObjectArbitraryData).Objects, arbitraryDataObject.Objects);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Null:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpNullArbitraryData arbitraryDataNull = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpNullArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1.RtmpNullArbitraryData(arbitraryDataNull.Name);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                destination.Add(data);
            }
        }

        private static void CopyRtmpArbitraryDataV2(OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpArbitraryDataCollection destination, OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataCollection source)
        {
            foreach (var arbitraryData in source)
            {
                OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpArbitraryData data = null;

                switch (arbitraryData.DataType)
                {
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Boolean:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpBooleanArbitraryData arbitraryDataBool = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpBooleanArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpBooleanArbitraryData(arbitraryDataBool.Name, arbitraryDataBool.Value);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Number:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpNumberArbitraryData arbitraryDataNumber = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpNumberArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpNumberArbitraryData(arbitraryDataNumber.Name, arbitraryDataNumber.Value);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.String:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpStringArbitraryData arbitraryDataString = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpStringArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpStringArbitraryData(arbitraryDataString.Name, arbitraryDataString.Value);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Object:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpObjectArbitraryData arbitraryDataObject = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpObjectArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpObjectArbitraryData(arbitraryDataObject.Name);

                            CopyRtmpArbitraryDataV2((data as OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpObjectArbitraryData).Objects, arbitraryDataObject.Objects);
                        }
                        break;
                    case OnlineVideos.MPUrlSourceFilter.RtmpArbitraryDataType.Null:
                        {
                            OnlineVideos.MPUrlSourceFilter.RtmpNullArbitraryData arbitraryDataNull = arbitraryData as OnlineVideos.MPUrlSourceFilter.RtmpNullArbitraryData;

                            data = new OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.RtmpNullArbitraryData(arbitraryDataNull.Name);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                destination.Add(data);
            }
        }

    }
}
