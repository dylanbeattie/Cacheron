using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Cacheron {
    public class EntityChangeTracker {
        private readonly Entity e;

        public EntityChangeTracker(Entity obj) {
            e = obj;
            ((INotifyPropertyChanging)obj).PropertyChanging += ItemOnPropertyChanging;
            ((INotifyPropertyChanged)obj).PropertyChanged += ItemOnPropertyChanged;
        }

        private readonly IDictionary<string, KeyValuePair<object, object>> changetracker = new Dictionary<string, KeyValuePair<object, object>>();

        public Entity GetUpdateEntity() {
            var retval = new Entity { Id = e.Id, LogicalName = e.LogicalName };
            foreach (var change in changetracker) {
                if (change.Value.Key != null && change.Value.Key.Equals(change.Value.Value) || change.Value.Key == change.Value.Value) continue;
                var attr = (AttributeLogicalNameAttribute)e.GetType().GetMembers().First(m => m.Name == change.Key).GetCustomAttributes(typeof(AttributeLogicalNameAttribute), true).First();
                retval.Attributes[attr.LogicalName] = change.Value.Value;
            }
            if (retval.Attributes.Any()) return retval;
            return null;
        }

        private void ItemOnPropertyChanging(object sender, PropertyChangingEventArgs propertyChangingEventArgs) {
            var name = propertyChangingEventArgs.PropertyName;
            if (changetracker.ContainsKey(name)) return;
            var value = sender.GetType().GetProperty(name).GetValue(sender, null);
            var values = new KeyValuePair<object, object>(value, null);
            changetracker.Add(name, values);
        }

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangingEventArgs) {
            var name = propertyChangingEventArgs.PropertyName;
            var values = changetracker[name];
            var newValue = sender.GetType().GetProperty(name).GetValue(sender, null);
            changetracker[name] = new KeyValuePair<object, object>(values.Key, newValue);
        }
    }
}