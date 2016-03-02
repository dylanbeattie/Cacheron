using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
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

        private void Notify(LocalPluginContext localContext) {
            if (localContext == null) throw new ArgumentNullException("localContext");
            var context = localContext.PluginExecutionContext;
            if (context == null || context.Depth > 1) return;

            var record = (Entity)context.InputParameters["Target"];
            var update = new EntityChange() {
                ChangedAtUtc = (DateTime)record.Attributes["modifiedon"],
                CorrelationId = context.CorrelationId,
                EntityId = record.Id,
                EntityName = record.LogicalName
            };

            //var unwantedAttributes = new[] { "modifiedon", "modifiedby", "modifiedonbehalfby", record.LogicalName + "id" };
            update.FieldChanges = record.Attributes.ToDictionary(x => x.Key, x => x.Value);
            using (var wc = new WebClient()) { wc.UploadString("http://requestb.in/1d6razq1", update.ToString()); }
        }

    }
}
