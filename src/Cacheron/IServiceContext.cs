using System;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Cacheron {
    public interface IServiceContext {
        void Trace(string format, params object[] args);
        IQueryable<TEntity> CreateQuery<TEntity>() where TEntity : Entity;
        void Update<TEntity>(TEntity entity, Action mutator) where TEntity : Entity;
        TEntity Create<TEntity>(TEntity entity) where TEntity : Entity;
        OrganizationResponse Execute(OrganizationRequest request);
        bool Detach(Entity entity);
    }
}