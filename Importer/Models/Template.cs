using System.Xml.Serialization;

namespace Importer.Models
{
    [XmlRoot(ElementName="Template")]
    public class Template {
        [XmlElement(ElementName="detail")]
        public Detail Detail { get; set; }
        [XmlElement(ElementName="ImportTemplateItems")]
        public ImportTemplateItems ImportTemplateItems { get; set; }
    }
}