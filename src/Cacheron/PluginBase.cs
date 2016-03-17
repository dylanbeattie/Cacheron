using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;

namespace Cacheron {
    /// <summary>Base class for Spotlight-authored CRM plugins.</summary>
    /// <remarks>This class steals several ideas from the Microsoft CRM plugin base class - the local 
    /// plugin context and the trace provider - but eschews the fragile and unnecessary tuple-based event registration syntax.</remarks>
    public abstract class PluginBase : IPlugin {
        public class LocalPluginContext {
            internal IServiceProvider ServiceProvider { get; private set; }

            internal IOrganizationService OrganizationService { get; private set; }

            internal IPluginExecutionContext PluginExecutionContext { get; private set; }

            internal ITracingService TracingService { get; private set; }

            internal LocalPluginContext(IServiceProvider serviceProvider) {
                if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");
                // Obtain the execution context service from the service provider.
                PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                // Obtain the tracing service from the service provider.
                TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                // Obtain the Organization Service factory service from the service provider
                var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                // Use the factory to generate the Organization Service.
                OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);
            }

            internal void Trace(string message) {
                if (string.IsNullOrWhiteSpace(message) || TracingService == null) return;
                if (PluginExecutionContext != null) message = String.Format("{0}, Correlation Id: {1}, Initiating User: {2}", message, PluginExecutionContext.CorrelationId, PluginExecutionContext.InitiatingUserId);
                TracingService.Trace(message);
            }
        }

        protected IServiceContext ServiceContext { get; private set; }
        private List<PluginRegistrationStep> handlers;

        protected List<PluginRegistrationStep> RegisteredHandlers {
            get { return (handlers ?? (handlers = new List<PluginRegistrationStep>())); }
        }


        protected void Register(CrmPluginStepStage stage, SdkMessageName message, string entityname, Action<LocalPluginContext> handler) {
            var step = new PluginRegistrationStep(stage, message, entityname, handler);
            RegisteredHandlers.Add(step);
        }

        protected virtual void Execute(LocalPluginContext context) {
            var matchingHandlers = RegisteredHandlers.Where(s => s.ShouldRunIn(context));
            foreach (var step in matchingHandlers) {
                if (step.Handler == null) return;
                context.Trace(string.Format(CultureInfo.InvariantCulture, "{0} is firing for Entity: {1}, Message: {2}", GetType(), context.PluginExecutionContext.PrimaryEntityName, context.PluginExecutionContext.MessageName));
                ServiceContext = ServiceContext ?? new PluginServiceContext(context.OrganizationService, context.TracingService);
                try { step.Handler.Invoke(context); } catch (InvalidPluginExecutionException e) {
                    if (context.PluginExecutionContext.Mode == (int)CrmPluginStepMode.Asynchronous) ServiceContext.Trace(e.ToString());
                    throw;
                }
            }
        }

        /// <summary>This is the execute method that's actually invoked by the CRM runtime when this plugin fires.</summary>
        public void Execute(IServiceProvider serviceProvider) {
            if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");
            var localContext = new LocalPluginContext(serviceProvider);
            localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", GetType().Name));
            try { Execute(localContext); } catch (FaultException<OrganizationServiceFault> e) {
                localContext.Trace(String.Format(CultureInfo.InvariantCulture, "FaultException: {0}", e));
                throw;
            } catch (Exception e) {
                localContext.Trace(String.Format(CultureInfo.InvariantCulture, "Exception: {0}", e));
                throw;
            } finally { localContext.Trace(String.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", GetType().Name)); }
        }

        public PluginRegistration ExportRegistrations() {
            var registration = new PluginRegistration {
                Description = this.GetType().Name,
                FriendlyName = this.GetType().Name,
                Name = this.GetType().FullName,
                Id = Guid.NewGuid(),
                TypeName = this.GetType().FullName,
                Steps = RegisteredHandlers
            };
            return (registration);
        }


        public class PluginRegistration {
            [XmlAttribute]
            public string Description { get; set; }

            [XmlAttribute]
            public string FriendlyName { get; set; }

            [XmlAttribute]
            public string Name { get; set; }

            [XmlAttribute]
            public Guid Id { get; set; }

            [XmlAttribute]
            public string TypeName { get; set; }

            [XmlArray]
            [XmlArrayItem("Step")]
            public List<PluginRegistrationStep> Steps { get; set; }
        }

        [XmlRoot("Step")]
        public class PluginRegistrationStep {
            public PluginRegistrationStep() { }

            public bool ShouldRunIn(LocalPluginContext context) {
                return (
                    ((int)this.Stage) == context.PluginExecutionContext.Stage
                        &&
                        this.Message.ToString() == context.PluginExecutionContext.MessageName
                        &&
                        this.EntityName == context.PluginExecutionContext.PrimaryEntityName
                    );
            }

            public PluginRegistrationStep(CrmPluginStepStage stage, SdkMessageName message, string entityName, Action<LocalPluginContext> handler) {
                Stage = stage;
                Message = message;
                EntityName = entityName;
                Handler = handler;
            }

            [XmlElement("Images")]
            public string[] Images = new string[0];

            [XmlAttribute]
            public CrmPluginStepStage Stage { get; set; }

            [XmlAttribute(AttributeName = "MessageName")]
            public SdkMessageName Message { get; set; }

            [XmlAttribute(AttributeName = "PrimaryEntityName")]
            public string EntityName { get; set; }

            [XmlIgnore]
            public Action<LocalPluginContext> Handler { get; set; }

            [XmlAttribute]
            public string CustomConfiguration {
                get { return (String.Empty); }
            }

            [XmlAttribute]
            public string Name {
                get { return (this.Stage + this.EntityName + this.Message); }
                set { }
            }

            [XmlAttribute]
            public string Description {
                get { return (String.Format("{0} of {1} {2}", this.Stage, this.EntityName, this.Message)); }
                set { }
            }

            [XmlAttribute]
            public Guid Id {
                get { return (Guid.Empty); }
                set { }
            }

            [XmlAttribute]
            public string Mode {
                get { return ("Asynchronous"); }
                set { }
            }

            [XmlAttribute]
            public int Rank {
                get { return (1); }
                set { }
            }

            [XmlAttribute]
            public string SupportedDeployment {
                get { return ("ServerOnly"); }
                set { }
            }
        }
    }
}
