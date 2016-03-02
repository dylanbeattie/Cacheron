using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;

namespace Cacheron {
    public class PluginServiceContext : IServiceContext {
        private readonly IOrganizationService service;
        private readonly ITracingService traceService;
        private readonly OrganizationServiceContext proxiedContext;

        public PluginServiceContext(IOrganizationService service, ITracingService traceService) {
            this.service = service;
            this.traceService = traceService;
            proxiedContext = new OrganizationServiceContext(service) { MergeOption = MergeOption.NoTracking };
        }

        public virtual IQueryable<TEntity> CreateQuery<TEntity>() where TEntity : Entity {
            return proxiedContext.CreateQuery<TEntity>();
        }

        public virtual void Update<TEntity>(TEntity entity, Action mutator) where TEntity : Entity {
            var tracker = new EntityChangeTracker(entity);
            mutator();
            var changes = tracker.GetUpdateEntity();
            if (changes == null) return;
            if (changes.Id == Guid.Empty) throw new InvalidOperationException("Entity has no Id. Are You trying to create an entity?");
            service.Execute(new UpdateRequest { Target = changes });
        }

        public virtual TEntity Create<TEntity>(TEntity entity) where TEntity : Entity {
            if (entity == null) throw new ArgumentException("entity");
            if (entity.Id != Guid.Empty) throw new InvalidOperationException("new entities should not have an Id");
            entity.Id = ((CreateResponse)service.Execute(new CreateRequest { Target = entity })).id;
            return entity;
        }

        public virtual void Trace(string format, params object[] args) {
            if (traceService != null) traceService.Trace(format, args);
        }

        public virtual OrganizationResponse Execute(OrganizationRequest request) {
            return proxiedContext.Execute(request);
        }

        public virtual bool Detach(Entity entity) {
            return proxiedContext.Detach(entity);
        }
    }
}