using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Cacheron.RegisterFileGenerator {
    internal class Program {
        private static void Main(string[] args) {
            Console.WindowWidth = 140;
            Console.WindowHeight = 120;

            var register = new Register();
            register.Solutions.Add(new Solution(typeof(NotifyEntityChangePlugin).Assembly));

            var serializer = new XmlSerializer(typeof(Register));
            var xns = new XmlSerializerNamespaces();
            xns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xns.Add("xsd", "http://www.w3.org/2001/XMLSchema");
            xns.Add(String.Empty, "http://schemas.microsoft.com/crm/2011/tools/pluginregistration");

            xns.Add(string.Empty, string.Empty);
            using (var sw = new Utf8StringWriter()) {
                using (var xw = XmlWriter.Create(sw)) {
                    serializer.Serialize(xw, register, xns);
                    File.WriteAllText(@"D:\Projects\Cacheron\src\CacheronPackage\register.xml", Prettify(sw.ToString()), Encoding.UTF8);
                }
            }
        }

        public class Utf8StringWriter : StringWriter {
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }

        private static string Prettify(string xml) {
            using (var ms = new MemoryStream()) {
                using (var xtw = new XmlTextWriter(ms, Encoding.UTF8)) {
                    var doc = new XmlDocument();
                    doc.LoadXml(xml);
                    var root = doc.DocumentElement;
                    var xmlnsAttribute = doc.CreateAttribute("xmlns");
                    xmlnsAttribute.Value = "http://schemas.microsoft.com/crm/2011/tools/pluginregistration";
                    root.Attributes.Append(xmlnsAttribute);
                    var stepLists = doc.SelectNodes("//Register/Solutions/Solution/PluginTypes/Plugin/Steps");
                    foreach (XmlElement stepList in stepLists) {
                        var clearNode = doc.CreateNode(XmlNodeType.Element, "clear", null);
                        stepList.PrependChild(clearNode);
                    }
                    xtw.Formatting = Formatting.Indented;
                    doc.WriteContentTo(xtw);
                    xtw.Flush();
                    ms.Flush();
                    ms.Position = 0;
                    using (var sr = new StreamReader(ms)) return (sr.ReadToEnd());
                }
            }
        }
    }
    [XmlRoot]
    public class Register {
        public List<Solution> Solutions { get; set; }

        public Register() {
            Solutions = new List<Solution>();
        }
    }

    //public class PluginType {

    //    public PluginType() { }

    //    public PluginType(PluginBase.PluginRegistration steps) {
    //        this.Plugin = new Plugin(steps);
    //    }

    //    public Plugin Plugin { get; set; }
    //}

    //public class Plugin {
    //    public Plugin() { }

    //    public Plugin(PluginBase.PluginRegistration plugin) {
    //        this.Plugin = plugin;
    //    }

    //    public PluginBase.PluginRegistration Plugin { get; set; }
    //}



    public class Solution {
        public Solution() {
            PluginTypes = new List<PluginBase.PluginRegistration>();
        }
        private readonly Assembly assembly;

        public Solution(Assembly assembly)
            : this() {
            this.assembly = assembly;
            foreach (var type in assembly.GetTypes().Where(t => t.BaseType == typeof(PluginBase))) {
                var plugin = Activator.CreateInstance(type) as PluginBase;
                if (plugin != null) PluginTypes.Add(plugin.ExportRegistrations());
            }
        }

        [XmlAttribute]
        public string Assembly {
            get {
                var path = new FileInfo(new Uri(assembly.CodeBase).LocalPath);
                return (path.Name);
            }
            set { }
        }

        [XmlAttribute]
        public Guid Id {
            get {
                var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
                return (Guid.Parse(attribute.Value));
            }
            set { }
        }

        [XmlArray]
        [XmlArrayItem("Plugin")]
        public List<PluginBase.PluginRegistration> PluginTypes { get; set; }

        [XmlAttribute]
        public string IsolationMode {
            get { return ("Sandbox"); }
            set { }
        }

        [XmlAttribute]
        public string SourceType {
            get { return ("Database"); }
            set { }
        }

    }
}
