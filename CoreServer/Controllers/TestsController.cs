using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace CoreServer.Controllers
{
    [Authorize]
    public class TestsController : ApiController
    {
        // GET api/tests
        public Dictionary<string, dynamic> Get()
        {
            return new Dictionary<string, dynamic>() { { "TestResult", "Success" } };
        }

        // POST api/tests
        public Dictionary<string, dynamic> Post([FromBody]Dictionary<string, dynamic> value)
        {
            if (value.ContainsKey("TestString"))
                if (value["TestString"] == "Hello World")
                    return new Dictionary<string, dynamic>() { { "TestResult", "Success" } };

            return new Dictionary<string, dynamic>() { { "TestResult", "Failure" } };
        }
    }
}