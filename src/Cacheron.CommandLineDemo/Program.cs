using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using StackExchange.Redis;

namespace Cacheron.CommandLineDemo {
    class Program {
        static void Main(string[] args) {
            //var fields = new string[] { "firstname", "lastname", "spot_artistreference" };
            //var connectionString = ConfigurationManager.ConnectionStrings["ConnectionStrings.Crm2015.Spotlight"].ConnectionString;
            ////var connection = CrmConnection.Parse(connectionString);
            ////var org = new OrganizationService(connection);
            //var creds = new ClientCredentials();
            //creds.Windows.ClientCredential = new NetworkCredential("a", "a", "SANDBOX");
            //var crmUri = new Uri("http://sandbox-crm2015.sandbox.local/Tamandua/XRMServices/2011/Organization.svc");
            //using (var crm = new OrganizationServiceProxy(crmUri, null, creds, null)) {
            //    var entityList = new EntityCollection();
            //    var sb = new StringBuilder();
            //    sb.AppendLine("<fetch version='1.0' page='{1}' paging-cookie='{0}' count='5000' output-format='xml-platform' mapping='logical' distinct='false'>");
            //    sb.AppendFormat("<entity name='contact'>");
            //    foreach (var field in fields) sb.AppendFormat("<attribute name='{0}'/>", field);
            //    sb.AppendLine("</entity></fetch>");
            //    var xml = sb.ToString();
            //    var page = 1;
            //    var stopwatch = new Stopwatch();
            //    do {
            //        entityList = crm.RetrieveMultiple(new FetchExpression(String.Format(xml, SecurityElement.Escape(entityList.PagingCookie), page++)));
            //        foreach (var e in entityList.Entities) {
            //            stopwatch.Start();
            //            Console.WriteLine("Updating {0} {1}", e.Attributes["firstname"], e.Attributes["lastname"]);
            //            // Console.ReadKey(false);
            //            if (!e.Attributes.ContainsKey("spot_artistreference")) e.Attributes.Add("spot_artistreference", String.Empty);
            //            if (String.IsNullOrEmpty(e.Attributes["spot_artistreference"].ToString())) e.Attributes["spot_artistreference"] = NextReference();
            //            e.Attributes["emailaddress1"] = String.Format("{0}.{1}@example.com", e.Attributes["firstname"], e.Attributes["lastname"]).ToLowerInvariant();
            //            crm.Update(e);
            //            stopwatch.Stop();
            //            File.AppendAllText(@"D:\crm_logs.csv", String.Format("{0},{1}{2}", e.Id, stopwatch.ElapsedMilliseconds, Environment.NewLine));
            //            stopwatch.Reset();
            //        }
            //    } while (entityList.MoreRecords);
            //}
            var creds = new ClientCredentials();
            var crmUri = new Uri("https://staging-spotlightuk.api.crm4.dynamics.com/XRMServices/2011/Organization.svc");
            creds.UserName.UserName = "telegram.staging@spotlightuk.onmicrosoft.com";
            creds.UserName.Password = "4IR0hjw3o07FlK2G";
            using (var crm = new OrganizationServiceProxy(crmUri, null, creds, null)) {
                var entityList = new EntityCollection();
                var sb = new StringBuilder();
                sb.AppendLine("<fetch version='1.0' page='{1}' paging-cookie='{0}' count='5000' output-format='xml-platform' mapping='logical' distinct='false'>");
                sb.AppendFormat("<entity name='{0}'>", "contact");
                foreach (var field in new[] { "firstname", "lastname", "spot_artistreference"})
                    sb.AppendFormat("<attribute name='{0}'/>", field);
                sb.AppendLine("</entity></fetch>");
                var xml = sb.ToString();
                var page = 1;
                do {
                    entityList = crm.RetrieveMultiple(new FetchExpression(String.Format(xml, SecurityElement.Escape(entityList.PagingCookie), page++)));
                    foreach (var e in entityList.Entities) Console.WriteLine(e);
                } while (entityList.MoreRecords);
            }

        }

        private static int counter = 9900000;
        private static readonly Random coin = new Random();
        public static string NextReference() {
            counter++;
            return (coin.Next(0, 2) == 1 ? "M" : "F") + counter;
        }
    }
}
