using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreDynamic.Providers
{
    public class AdfsServiceException : Exception
    {
        public AdfsServiceException(string message, Exception innerException) : base(message)
        {
        }
    }
    public interface IAdfsServiceProvider
    {
        void Disconnect();
        Task Logout();
        Task GetAuthorizationToken();
        string GetAuthorizationHeader();
        string GetUserFullName();
    }

    public class AdfsServiceProvider : IAdfsServiceProvider
    {
        private static string adfsAuthority = "https://login.towerdevs.xyz/adfs";
        public static string coreServerUrl = "https://coreserver.core";
        private static string clientId = "bd7f3c17-cb37-45b0-b720-7a486ec05317";

        private TokenCache cache;
        private AuthenticationContext context;
        private AuthenticationResult token;

        private static IFileSystem TokenFileSystem = FileSystem.Current;
        private IFolder TokenFolder;
        private IFile TokenFile;
        private IPlatformParameters Parameters;

        //private bool requireExplicitAuthentication = false;

        public AdfsServiceProvider(IServiceCollection collection)
        {
            Parameters = collection.BuildServiceProvider().GetRequiredService<IPlatformParameters>();

            Task.Run(async () => { await InitializeCache(); });
        }

        private async Task InitializeCache()
        {
            TokenFolder = await TokenFileSystem.LocalStorage.CreateFolderAsync("CoreDynamic", CreationCollisionOption.OpenIfExists);
            cache = new TokenCache();
            context = new AuthenticationContext(adfsAuthority, false, cache);

            try
            {
                TokenFile = await TokenFolder.GetFileAsync("credential.cache");

                string Credential = await TokenFile.ReadAllTextAsync();
                byte[] SerializedCredential = Convert.FromBase64String(Credential);
                cache.Deserialize(SerializedCredential);
            }
            catch (FileNotFoundException)
            {
                TokenFile = await TokenFolder.CreateFileAsync("credential.cache", CreationCollisionOption.ReplaceExisting);
                SaveCache();
            }
        }

        public async Task Logout()
        {
            Disconnect();
            await PurgeCache();
        }

        public void Disconnect()
        {
            token = null;
        }

        private async Task PurgeCache()
        {
            await TokenFile.DeleteAsync();
            cache = new TokenCache();
            context = new AuthenticationContext(adfsAuthority, false, cache);
            //requireExplicitAuthentication = true;
        }

        private async void SaveCache()
        {
            byte[] SerializedCredential = cache.Serialize();

            await TokenFile.WriteAllTextAsync(Convert.ToBase64String(SerializedCredential));
        }

        public async Task GetAuthorizationToken()
        {
            try
            {
                token = await context.AcquireTokenAsync(coreServerUrl, clientId, new Uri(coreServerUrl), Parameters);
            }
            catch (AdalServiceException)
            {
                await PurgeCache();
                TokenFile = await TokenFolder.CreateFileAsync("credential.cache", CreationCollisionOption.ReplaceExisting);
                token = await context.AcquireTokenAsync(coreServerUrl, clientId, new Uri(coreServerUrl), Parameters);
            }
            
            SaveCache();
        }

        public string GetAuthorizationHeader()
        {
            return token.CreateAuthorizationHeader();
        }

        public string GetUserFullName()
        {
            return token.UserInfo.DisplayableId;
        }
    }
}
