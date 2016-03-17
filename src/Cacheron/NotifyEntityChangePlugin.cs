using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace Cacheron {
    public class NotifyEntityChangePlugin : PluginBase {

        private static readonly string[] Entities = { "account", "contact", "salesorder", "invoice", "contract", "lead", "opportunity" };

        /// <summary>Initializes a new instance of the <see cref="NotifyEntityChangePlugin"/> class.</summary>
        /// <param name="serviceContext"></param>
        public NotifyEntityChangePlugin() {
            // public NotifyEntityChangePlugin(IServiceContext serviceContext) {

            foreach (var entity in Entities) {
                Register(CrmPluginStepStage.PostOutsideTransaction, SdkMessageName.Create, entity, Notify);
                Register(CrmPluginStepStage.PostOutsideTransaction, SdkMessageName.Update, entity, Notify);
                Register(CrmPluginStepStage.PostOutsideTransaction, SdkMessageName.Delete, entity, Notify);
            }
        }

        // Bulk imports will fire the plugin with PluginExecutionContext.Depth=3, so 
        // the "traditional" depth > 1 filter will prevent this plugin running on
        // records created during bulk import. Which is bad.
        private const int MAX_DEPTH = 3;
        private void Notify(LocalPluginContext localContext) {
            if (localContext == null) throw new ArgumentNullException("localContext");
            var context = localContext.PluginExecutionContext;

            if (context == null || context.Depth > MAX_DEPTH) return;

            var record = (Entity)context.InputParameters["Target"];
            var update = new EntityChange() {
                ChangeType = context.MessageName,
                ChangedAtUtc = (DateTime)record.Attributes["modifiedon"],
                CorrelationId = context.CorrelationId,
                EntityId = record.Id,
                EntityName = record.LogicalName
            };

            //var unwantedAttributes = new[] { "modifiedon", "modifiedby", "modifiedonbehalfby", record.LogicalName + "id" };
            update.FieldChanges = record.Attributes.ToDictionary(x => x.Key, Describe);

            using (var wc = new WebClient()) { wc.UploadString("http://cacheron.win00101/home/fnord", ToJson(update)); }
        }


        public static string ToJson(object o) {
            var serializer = new DataContractJsonSerializer(o.GetType(), new[] { typeof(EntityReference), typeof(OptionSetValue) });
            using (var ms = new MemoryStream()) {
                serializer.WriteObject(ms, o);
                return (Encoding.Default.GetString(ms.ToArray()));
            }
        }

        private object Describe(KeyValuePair<string, object> change) {
            if (change.Value is EntityReference) return change.Value;
            if (change.Value is OptionSetValue) return change.Value;
            if (change.Value is Money) return ((Money)change.Value).Value;
            return change.Value;
        }
    }
}
