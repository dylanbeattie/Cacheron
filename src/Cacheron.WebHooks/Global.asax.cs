using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security;
using System.ServiceModel.Description;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Cacheron.WebHooks {
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class EntityCache : Dictionary<string, Dictionary<Guid, Dictionary<string, object>>> {
        public void Update(EntityChange change) {
            foreach (var fieldChange in change.FieldChanges) {
                Update(change.EntityName, change.EntityId, fieldChange.Key, fieldChange.Value);
            }
        }

        public void Update(string entity, Guid id, string field, object value) {
            if (!this.ContainsKey(entity)) this.Add(entity, new Dictionary<Guid, Dictionary<string, object>>());
            if (!this[entity].ContainsKey(id)) this[entity].Add(id, new Dictionary<string, object>());
            this[entity][id][field] = value;
        }

    }

    public class MvcApplication : System.Web.HttpApplication {

        private static readonly EntityCache entities = new EntityCache();
        public static EntityCache Entities { get { return (entities); } }

        protected void Application_Start() {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            PopulateCache("contact", "firstname", "lastname", "emailaddress1", "mobilephone", "new_publishemailaddress1");
            PopulateCache("contract", "firstname", "lastname", "emailaddress1", "mobilephone", "new_publishemailaddress1");
        }

        private void PopulateCache(string entityName, params string[] fields) {
            var connectionString = ConfigurationManager.ConnectionStrings["ConnectionStrings.Crm2015.Spotlight"].ConnectionString;
            //var connection = CrmConnection.Parse(connectionString);
            //var org = new OrganizationService(connection);
            var creds = new ClientCredentials();
            creds.Windows.ClientCredential = new NetworkCredential("a", "a", "SANDBOX");
            var crmUri = new Uri("http://sandbox-crm2015.sandbox.local/Tamandua/XRMServices/2011/Organization.svc");
            using (var crm = new OrganizationServiceProxy(crmUri, null, creds, null)) {
                var entityList = new EntityCollection();
                var sb = new StringBuilder();
                sb.AppendLine("<fetch version='1.0' page='{1}' paging-cookie='{0}' count='5000' output-format='xml-platform' mapping='logical' distinct='false'>");
                sb.AppendFormat("<entity name='{0}'>", entityName);
                foreach (var field in fields) sb.AppendFormat("<attribute name='{0}'/>", field);
                sb.AppendLine("</entity></fetch>");
                var xml = sb.ToString();
                var page = 1;
                entities[entityName] = new Dictionary<Guid, Dictionary<string, object>>();
                do {
                    entityList = crm.RetrieveMultiple(new FetchExpression(String.Format(xml, SecurityElement.Escape(entityList.PagingCookie), page++)));
                    foreach (var e in entityList.Entities) { entities[entityName][e.Id] = e.Attributes.ToDictionary(a => a.Key, a => a.Value); }
                } while (entityList.MoreRecords);
            }
        }
    }
}