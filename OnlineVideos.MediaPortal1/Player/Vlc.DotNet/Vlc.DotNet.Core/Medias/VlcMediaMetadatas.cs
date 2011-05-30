using System;
using System.ComponentModel;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.Media;

namespace Vlc.DotNet.Core.Medias
{
    public sealed class VlcMediaMetadatas : IDisposable
    {
        private readonly MediaBase myHostMediaBase;

        #region Metadatas

        /// <summary>
        /// Gets or sets the Album metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Album
        {
            get { return GetMetadata(Metadatas.Album); }
            set { SetMetadata(Metadatas.Album, value); }
        }

        /// <summary>
        /// Gets or sets the Artist metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Artist
        {
            get { return GetMetadata(Metadatas.Artist); }
            set { SetMetadata(Metadatas.Artist, value); }
        }

        /// <summary>
        /// Gets or sets the ArtworkURL metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string ArtworkURL
        {
            get { return GetMetadata(Metadatas.ArtworkURL); }
            set { SetMetadata(Metadatas.ArtworkURL, value); }
        }

        /// <summary>
        /// Gets or sets the Copyright metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Copyright
        {
            get { return GetMetadata(Metadatas.Copyright); }
            set { SetMetadata(Metadatas.Copyright, value); }
        }

        /// <summary>
        /// Gets or sets the Date metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Date
        {
            get { return GetMetadata(Metadatas.Date); }
            set { SetMetadata(Metadatas.Date, value); }
        }

        /// <summary>
        /// Gets or sets the Description metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Description
        {
            get { return GetMetadata(Metadatas.Description); }
            set { SetMetadata(Metadatas.Description, value); }
        }

        /// <summary>
        /// Gets or sets the EncodedBy metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string EncodedBy
        {
            get { return GetMetadata(Metadatas.EncodedBy); }
            set { SetMetadata(Metadatas.EncodedBy, value); }
        }

        /// <summary>
        /// Gets or sets the Genre metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Genre
        {
            get { return GetMetadata(Metadatas.Genre); }
            set { SetMetadata(Metadatas.Genre, value); }
        }

        /// <summary>
        /// Gets or sets the Language metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Language
        {
            get { return GetMetadata(Metadatas.Language); }
            set { SetMetadata(Metadatas.Language, value); }
        }

        /// <summary>
        /// Gets or sets the NowPlaying metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string NowPlaying
        {
            get { return GetMetadata(Metadatas.NowPlaying); }
            set { SetMetadata(Metadatas.NowPlaying, value); }
        }

        /// <summary>
        /// Gets or sets the Publisher metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Publisher
        {
            get { return GetMetadata(Metadatas.Publisher); }
            set { SetMetadata(Metadatas.Publisher, value); }
        }

        /// <summary>
        /// Gets or sets the Rating metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Rating
        {
            get { return GetMetadata(Metadatas.Rating); }
            set { SetMetadata(Metadatas.Rating, value); }
        }

        /// <summary>
        /// Gets or sets the Setting metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Setting
        {
            get { return GetMetadata(Metadatas.Setting); }
            set { SetMetadata(Metadatas.Setting, value); }
        }

        /// <summary>
        /// Gets or sets the Title metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string Title
        {
            get { return GetMetadata(Metadatas.Title); }
            set { SetMetadata(Metadatas.Title, value); }
        }

        /// <summary>
        /// Gets or sets the TrackID metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string TrackID
        {
            get { return GetMetadata(Metadatas.TrackID); }
            set { SetMetadata(Metadatas.TrackID, value); }
        }

        /// <summary>
        /// Gets or sets the TrackNumber metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string TrackNumber
        {
            get { return GetMetadata(Metadatas.TrackNumber); }
            set { SetMetadata(Metadatas.TrackNumber, value); }
        }

        /// <summary>
        /// Gets or sets the URL metadata
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string URL
        {
            get { return GetMetadata(Metadatas.URL); }
            set { SetMetadata(Metadatas.URL, value); }
        }

        /// <summary>
        /// Gets media metadata
        /// </summary>
        /// <param name="metadata">Media property</param>
        /// <returns>Metadata value</returns>
        private string GetMetadata(Metadatas metadata)
        {
            if (!VlcContext.HandleManager.MediasHandles.ContainsKey(myHostMediaBase))
                return null;
            if (VlcContext.InteropManager.MediaInterops.IsParsed.Invoke(VlcContext.HandleManager.MediasHandles[myHostMediaBase]) == 0)
                VlcContext.InteropManager.MediaInterops.Parse.Invoke(VlcContext.HandleManager.MediasHandles[myHostMediaBase]);
            try
            {
                return VlcContext.InteropManager.MediaInterops.GetMetadata.Invoke(
                    VlcContext.HandleManager.MediasHandles[myHostMediaBase],
                    metadata);
            }
            catch
            {
                return null;
            }
        }

        private void SetMetadata(Metadatas metadata, string value)
        {
            if (!VlcContext.HandleManager.MediasHandles.ContainsKey(myHostMediaBase))
                return;
            VlcContext.InteropManager.MediaInterops.SetMetadata.Invoke(
                VlcContext.HandleManager.MediasHandles[myHostMediaBase],
                metadata,
                value);
        }

        /// <summary>
        /// Save the metadatas
        /// </summary>
        public void Save()
        {
            if (!VlcContext.HandleManager.MediasHandles.ContainsKey(myHostMediaBase))
                return;
            VlcContext.InteropManager.MediaInterops.SaveMetadatas.Invoke(
                VlcContext.HandleManager.MediasHandles[myHostMediaBase]);
        }

        #endregion

        internal VlcMediaMetadatas(MediaBase mediaBase)
        {
            myHostMediaBase = mediaBase;
        }

        public void Dispose()
        {
        }
    }
}
