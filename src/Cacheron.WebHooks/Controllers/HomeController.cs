using System;
using System.Collections.Generic;
using System.Data.EntityClient;
using System.Data.Services;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Cacheron.WebHooks.Controllers {
    public class HomeController : Controller {

        private static EntityCache Entities {
            get { return (MvcApplication.Entities); }
        }
        private static readonly List<string> Requests = new List<string>();

        public ActionResult Fnord() {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}", Request.HttpMethod, Request.RawUrl);
            foreach (var key in Request.Headers.AllKeys) sb.AppendLine(key + " : " + Request.Headers[key]);
            sb.AppendLine();
            var json = new System.IO.StreamReader(Request.InputStream).ReadToEnd();
            sb.AppendLine(json);
            Requests.Add(sb.ToString());
            var serializer = new DataContractJsonSerializer(typeof(EntityChange), new[] { typeof(EntityReference), typeof(OptionSetValue) });
            var update = serializer.ReadObject(new MemoryStream(Encoding.Unicode.GetBytes(json))) as EntityChange;
            Entities.Update(update);
            return (Content("OK!"));
        }

        public ActionResult Dump() {
            return (View(Entities.Values.SelectMany(e => e.Values.Select(x => x.ToListing()))));
        }
        public ActionResult Index() {
            return (View(Requests));
        }
    }

    public class Listing {
        public string FullName { get; set; }
        public Dictionary<string, string> ContactDetails = new Dictionary<string, string>();
    }
    public static class EntityExtensions {

        public static Listing ToListing(this Dictionary<string, object> entity) {
            var listing = new Listing();
            listing.FullName = entity["firstname"] + " " + entity["lastname"];
            foreach (var key in entity.Keys.Where(key => entity.ContainsKey("new_publish" + key) && entity["new_publish" + key] is bool && ((bool)entity["new_publish" + key]))) { listing.ContactDetails.Add(key, entity[key].ToString()); }
            return (listing);
        }
    }
}
