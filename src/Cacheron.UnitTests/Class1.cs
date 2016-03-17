using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using NUnit.Framework;
using Shouldly;

namespace Cacheron.UnitTests {
    [TestFixture]
    public class Class1 {
        [Test]
        public void Foo() {
            var es = new EntityChange {
                ChangeType = "update",
                EntityId = Guid.NewGuid(),
                EntityName = "contact",
                FieldChanges = new Dictionary<string, object>() {
                    { "name", "Jeremy" },
                    { "lastmodifiedby", new EntityReference("systemuser", Guid.NewGuid()) },
                    { "preferred", new OptionSetValue(5) }
                }
            };
            var json = NotifyEntityChangePlugin.ToJson(es);
            Console.WriteLine(json);
            Assert.Pass();
        }

        [Test]
        public void Bar() {
            var json = "{\"ChangeType\":\"Update\",\"ChangedAtUtc\":\"\\/Date(1457017097000)\\/\",\"CorrelationId\":\"57162cd3-c07c-4036-9d64-a7e00fc7cac2\",\"EntityId\":\"8443e3bd-4ee1-e511-80d3-005056873be7\",\"EntityName\":\"contact\",\"FieldChanges\":[{\"Key\":\"mobilephone\",\"Value\":\"07980\"},{\"Key\":\"contactid\",\"Value\":\"8443e3bd-4ee1-e511-80d3-005056873be7\"},{\"Key\":\"modifiedon\",\"Value\":\"\\/Date(1457017097000)\\/\"},{\"Key\":\"modifiedby\",\"Value\":{\"__type\":\"EntityReference:http:\\/\\/schemas.microsoft.com\\/xrm\\/2011\\/Contracts\",\"Id\":\"fd14318f-01df-e511-80d2-005056873be7\",\"LogicalName\":\"systemuser\",\"Name\":null}},{\"Key\":\"modifiedonbehalfby\",\"Value\":null}]}";
            var serializer = new DataContractJsonSerializer(typeof(EntityChange), new[] { typeof(EntityReference), typeof(OptionSetValue) });
            var ms = new MemoryStream(Encoding.Unicode.GetBytes(json));
            var update = serializer.ReadObject(ms) as EntityChange;
            update.EntityName.ShouldBe("contact");
        }
    }
}
