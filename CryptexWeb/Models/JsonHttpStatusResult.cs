using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace CryptexWeb.Models
{
    public class JsonHttpStatusResult : JsonResult
    {
        private readonly HttpStatusCode _httpStatus;

        public JsonHttpStatusResult(object data, HttpStatusCode httpStatus)
        {
            Data = data;
            _httpStatus = httpStatus;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            context.RequestContext.HttpContext.Response.StatusCode = (int)_httpStatus;
            base.ExecuteResult(context);
        }
    }


}