using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cacheron {
    public class EntityChange {
        public DateTime ChangedAtUtc { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityName { get; set; }
        private Dictionary<string, object> fieldChanges = new Dictionary<string, object>();
        /// <summary>A collection of fields, including the entity ID field and any fields affected by the change.</summary>
        public Dictionary<string, object> FieldChanges {
            get { return fieldChanges; }
            set { fieldChanges = value; }
        }
        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var f in fieldChanges.Where(f => f.Value != null)) {
                sb.Append(f.Key + "=" + f.Value);
            }
            return (sb.ToString());
        }
    }
}