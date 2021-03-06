using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Microsoft.WindowsAzure.MobileServices.Sync;
using PCLStorage;
using Plugin.Media;
using XamarinTodo.iOS.Services;
using XamarinTodo.Services;

[assembly: Xamarin.Forms.Dependency(typeof(iOSFileProvider))]
namespace XamarinTodo.iOS.Services
{
    public class iOSFileProvider : IFileProvider
    {
		public async Task<string> CopyItemFileAsync(string itemId, string filePath)
		{
			string fileName = System.IO.Path.GetFileName(filePath);
			string targetPath = await GetLocalFilePathAsync(itemId, fileName);

			var sourceFile = await FileSystem.Current.GetFileFromPathAsync(filePath);
			var sourceStream = await sourceFile.OpenAsync(FileAccess.Read);

			var targetFile = await  FileSystem.Current.LocalStorage.CreateFileAsync(targetPath, CreationCollisionOption.ReplaceExisting);
			using (var targetStream = await targetFile.OpenAsync(FileAccess.ReadAndWrite))
			{
				await sourceStream.CopyToAsync(targetStream);
			}

			return targetPath;
		}

		public async Task DeleteLocalFileAsync(MobileServiceFile fileName)
		{
			string localPath = await GetLocalFilePathAsync(fileName.ParentId, fileName.Name);
			var checkExists = await FileSystem.Current.LocalStorage.CheckExistsAsync(localPath);

			if (checkExists == ExistenceCheckResult.FileExists)
			{
				var file = await FileSystem.Current.LocalStorage.GetFileAsync(localPath);
				await file.DeleteAsync();
			}
		}

		public async Task DownloadFileAsync<T>(IMobileServiceSyncTable<T> table, MobileServiceFile file, string filename)
        {
            var path = await GetLocalFilePathAsync(file.ParentId, file.Name);
            await table.DownloadFileAsync(file, path);
        }

        public async Task<IMobileServiceFileDataSource> GetFileDataSource(MobileServiceFileMetadata metadata)
        {
            var path = await GetLocalFilePathAsync(metadata.ParentDataItemId, metadata.FileName);
			return new PathMobileServiceFileDataSource(path);
        }

        public async Task<string> GetImageAsync()
        {
            try
            {
                await CrossMedia.Current.Initialize();
                var file = await CrossMedia.Current.PickPhotoAsync();
                return file.Path;
            }
            catch (TaskCanceledException) { }
            return null;
        }

        public Task<string> GetItemFilesPathAsync()
        {
            string path = PortablePath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TodoItemFiles");
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);
            return Task.FromResult(path);
        }

		public async Task<string> GetLocalFilePathAsync(string itemId, string fileName)
		{
			string recordFilesPath = PortablePath.Combine(await GetItemFilesPathAsync(), itemId);

			var checkExists = await FileSystem.Current.LocalStorage.CheckExistsAsync(recordFilesPath);
			if (checkExists == ExistenceCheckResult.NotFound)
			{
				await FileSystem.Current.LocalStorage.CreateFolderAsync(recordFilesPath, CreationCollisionOption.ReplaceExisting);
			}

			return PortablePath.Combine(recordFilesPath, fileName);
		}
	}
}