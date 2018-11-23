using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CoreDynamic.Providers
{
    public class CoreServerRequest
    {
        private static HttpClient client = new HttpClient();
        private IAdfsServiceProvider serviceProvider;
        private string endpoint;
        private string fileNameSpace;

        private string coreServerUrl = Strings.CoreServerUrl;
        public enum FileSendMode
        {
            Create,
            Update
        }

        public CoreServerRequest(string endpoint, IAdfsServiceProvider serviceProvider, string fileNameSpace = "MiscFiles")
        {
            // No longer needed for UWP
            //#if DEBUG
              //coreServerUrl = Strings.CoreServerDevelopmentUrl;
            //#endif

            this.endpoint = endpoint;
            this.serviceProvider = serviceProvider;
            this.fileNameSpace = fileNameSpace;
        }

        private void AddAuthorization(ref HttpRequestMessage request)
        {
            string AuthenticationHeader = serviceProvider.GetAuthorizationHeader();
            request.Headers.TryAddWithoutValidation("Authorization", AuthenticationHeader);
        }

        /// <summary>
        /// Sends a request to the current server endpoint.
        /// </summary>
        /// <param name="values">JSON values to pass to the server.</param>
        /// <returns>String representing a JSON object.</returns>
        public async Task<string> SendRequestAsync(string values = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, coreServerUrl + "/api/" + endpoint);
            AddAuthorization(ref request);

            if (values != null)
            {
                request.Method = HttpMethod.Post;
                request.Content = new StringContent(values, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task CreateFileAsync(string filePath, Stream fileStream = null)
        {
            if (fileStream != null)
                await SendFileAsync(new StreamContent(fileStream), filePath, FileSendMode.Create);

            if (fileStream == null)
                return; //temp TODO: remove this
                //await SendFileAsync(new StreamContent(File.OpenRead(filePath)), filePath, FileSendMode.Create);
        }

        public async Task UpdateFileAsync(string filePath, Stream fileStream = null)
        {
            if (fileStream != null)
                await SendFileAsync(new StreamContent(fileStream), filePath, FileSendMode.Update);

            if (fileStream == null)
                return; //temp TODO: remove this
                //await SendFileAsync(new StreamContent(File.OpenRead(filePath)), filePath, FileSendMode.Update);
        }

        public async Task SendFileAsync(StreamContent fileStream, string fileName, FileSendMode mode)
        {
            HttpRequestMessage request = null;

            if (mode == FileSendMode.Create)
                request = new HttpRequestMessage(HttpMethod.Post, coreServerUrl + "/api/FileStore?fileName=" + fileName + "&nameSpace=" + fileNameSpace);

            if (mode == FileSendMode.Update)
                request = new HttpRequestMessage(HttpMethod.Put, coreServerUrl + "/api/FileStore?fileName=" + fileName + "&nameSpace=" + fileNameSpace);


            AddAuthorization(ref request);

            request.Content = fileStream;

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteFileAsync(string fileName)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, coreServerUrl + "/api/FileStore?fileName=" + fileName + "&nameSpace=" + fileNameSpace);
            AddAuthorization(ref request);

            HttpResponseMessage response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, coreServerUrl + "/api/FileStore?fileName=" + fileName + "&nameSpace=" + fileNameSpace);
            AddAuthorization(ref request);

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<List<string>> ListFilesAsync()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, coreServerUrl + "/api/FileStore?nameSpace=" + fileNameSpace);
            AddAuthorization(ref request);

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());
        }
    }
}
