using CoreServer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Security;

namespace CoreServer.Controllers
{
    [Authorize]
    public class FileStoreController : ApiController
    {
        FileStorageService service;

        private string userName;

        public FileStoreController()
        {
            userName = User.Identity.Name;
        }

        /// <summary>
        /// Uploads a file to the server. Should be used sparingly.
        /// </summary>
        /// <param name="file">The file being uploaded.</param>
        /// <param name="nameSpace">The namespace to use.</param>
        /// <returns>Response code describing whether the upload was successful.</returns>
        public async Task<HttpResponseMessage> PostAsync(string fileName, string nameSpace = "MiscFiles")
        {
            try
            {
                Stream contentStream = await Request.Content.ReadAsStreamAsync();

                service = new FileStorageService(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)), nameSpace);
                await service.SaveFileAsync(contentStream, fileName);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (System.IO.InvalidDataException)
            {
                throw new HttpResponseException(HttpStatusCode.NotAcceptable);
            }
        }

        /// <summary>
        /// Updates an existing file in the store with
        /// a new copy of the file.
        /// </summary>
        /// <param name="fileName">The file name to overwrite.</param>
        /// <param name="nameSpace">The namespace to save to.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutAsync(string fileName, string nameSpace = "MiscFiles")
        {
            try
            {
                Stream contentStream = await Request.Content.ReadAsStreamAsync();

                service = new FileStorageService(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)), nameSpace);
                await service.UpdateFileAsync(contentStream, fileName);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (System.IO.InvalidDataException)
            {
                throw new HttpResponseException(HttpStatusCode.NotAcceptable);
            }
        }

        /// <summary>
        /// Deletes a file from the server.
        /// </summary>
        /// <param name="fileName">The file identifier to delete.</param>
        /// <param name="nameSpace">The namespace to use.</param>
        /// <returns>Response code describing whether the deletion was successful.</returns>
        public HttpResponseMessage Delete(string fileName, string nameSpace = "MiscFiles")
        {
            try
            {
                service = new FileStorageService(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)), nameSpace);
                service.DeleteFile(fileName);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (InvalidDataException)
            {
                throw new HttpResponseException(HttpStatusCode.NotAcceptable);
            }
        }

        /// <summary>
        /// Retrieves a single file from the server.
        /// </summary>
        /// <param name="fileName">The file to retrieve.</param>
        /// <param name="nameSpace">The namespace to use.</param>
        /// <returns>The file, or a response code if not successful.</returns>
        public HttpResponseMessage Get(string fileName, string nameSpace = "MiscFiles")
        {
            try
            {
                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                service = new FileStorageService(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)), nameSpace);

                FileStream stream = service.RetrieveFile(fileName);

                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                result.Content.Headers.ContentLength = stream.Length;

                return result;
            }
            catch (FileNotFoundException)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Retrieves a list of all files in the current user scope.
        /// </summary>
        /// <param name="nameSpace">The namespace to use.</param>
        /// <returns>The file list, or a response code if not successful.</returns>
        [ResponseType(typeof(List<string>))]
        public HttpResponseMessage Get(string nameSpace = "MiscFiles")
        {
            try
            {
                service = new FileStorageService(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)), nameSpace);
                List<string> list = service.ListFiles();
                return Request.CreateResponse(HttpStatusCode.OK, list);
            }
            catch (DirectoryNotFoundException)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }
    }
}