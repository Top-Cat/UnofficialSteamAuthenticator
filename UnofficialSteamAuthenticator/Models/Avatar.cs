using System;
using System.IO;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using UnofficialSteamAuthenticator.Models;

namespace UnofficialSteamAuthenticator
{
    internal class Avatar : ModelBase
    {
        private readonly string FileName;
        private StorageFolder Folder;

        public Avatar(ulong steamid)
        {
            this.FileName = steamid + ".jpg";
            this.Img = new BitmapImage();

            this.Init();
        }

        public BitmapImage Img { get; }

        public async void SetUri(Uri uri)
        {
            try
            {
                StorageFile file = await this.Folder.GetFileAsync(this.FileName);

                if ((DateTime.Now - file.DateCreated).Days > 1)
                {
                    // Refresh cache
                    // The cached image will be shown until we finish the download
                    this.Store(uri);
                }
            }
            catch (FileNotFoundException)
            {
                this.Store(uri);
            }
        }

        private async void Init()
        {
            // Create virtual store and file stream. Check for duplicate files
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            this.Folder = await folder.CreateFolderAsync("avatarCache", CreationCollisionOption.OpenIfExists);

            try
            {
                StorageFile file = await this.Folder.GetFileAsync(this.FileName);

                IRandomAccessStream stream = await file.OpenReadAsync();
                this.Img.SetSource(stream);
            }
            catch (FileNotFoundException)
            {
                // No image in cache
            }
        }

        private async void Store(Uri uri)
        {
            // Download avatar into the cache
            StorageFile file = await this.Folder.CreateFileAsync(this.FileName, CreationCollisionOption.OpenIfExists);

            var downloader = new BackgroundDownloader();
            DownloadOperation dl = downloader.CreateDownload(uri, file);

            DownloadOperation res = await dl.StartAsync();

            IRandomAccessStream stream = await file.OpenReadAsync();
            this.Img.SetSource(stream);
        }
    }
}
