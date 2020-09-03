using System.Collections.Generic;
using System.Xml.Serialization;

namespace Importer.Models
{
    [XmlRoot(ElementName="ImportTemplateItems")]
    public class ImportTemplateItems {
        [XmlElement(ElementName="item")]
        public List<Item> Item { get; set; }
    }
}