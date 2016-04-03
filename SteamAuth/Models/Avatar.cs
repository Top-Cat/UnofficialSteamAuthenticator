using System;
using System.IO;
using System.Net.Http;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace UnofficialSteamAuthenticator.Lib.Models
{
    public class Avatar : ModelBase
    {
        private readonly string fileName;
        private StorageFolder folder;

        public Avatar(ulong steamid)
        {
            this.fileName = steamid + ".jpg";
            this.Img = new BitmapImage();

            this.Init();
        }

        public BitmapImage Img { get; }

        public async void SetUri(Uri uri)
        {
            try
            {
                StorageFile file = await this.folder.GetFileAsync(this.fileName);

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
            this.folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("avatarCache", CreationCollisionOption.OpenIfExists);

            this.Get();
        }

        private async void Get()
        {
            try
            {
                StorageFile file = await this.folder.GetFileAsync(this.fileName);

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
            try
            {
                var httpClient = new HttpClient();
                byte[] data = await httpClient.GetByteArrayAsync(uri);

                StorageFile file = await this.folder.CreateFileAsync(this.fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, data);

                this.Get();
            }
            catch (Exception)
            {
                // No network or no permission??, don't retry
            }
        }
    }
}
