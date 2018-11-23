using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Providers;
using CoreServer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Xml;
using System.Xml.Schema;

namespace CoreServer.Controllers.DRX
{
    [Authorize]
    public class DRXFileMetadataController : ApiController
    {
        // POST api/values
        public IDictionary<int, IDictionary<string, dynamic>> Post(List<int> drxDocuments)
        {
            FileStorageService fss = new FileStorageService(Convert.ToBase64String(Encoding.UTF8.GetBytes(User.Identity.Name)), "DRXStore");

            IDictionary<int, IDictionary<string, dynamic>> metaData = new Dictionary<int, IDictionary<string, dynamic>>();

            foreach (int id in drxDocuments)
            {
                try
                {
                    using (FileStream stream = fss.RetrieveFile("DRX_" + Convert.ToString(id) + ".drx"))
                    {
                        if (stream != null)
                        {
                            using (IFileProvider fileProvider = new FileProvider(stream))
                            {
                                metaData.Add(id, fileProvider.GetMetadata());
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    metaData.Add(id, null); 
                }
            }

            return metaData;
        }
    }
}