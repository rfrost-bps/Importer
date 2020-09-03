using System.Xml.Serialization;

namespace Importer.Models
{
    [XmlRoot(ElementName="item")]
    public class Item {
        [XmlAttribute(AttributeName="ColumnNumber")]
        public int ColumnNumber { get; set; }
        [XmlAttribute(AttributeName="AtomField")]
        public string AtomField { get; set; }
        [XmlAttribute(AttributeName="AtomTable")]
        public string AtomTable { get; set; }
        [XmlAttribute(AttributeName="DefaultValue")]
        public string DefaultValue { get; set; }
        [XmlAttribute(AttributeName="ImportField")]
        public string ImportField { get; set; }
        [XmlAttribute(AttributeName="IsAdditional")]
        public bool IsAdditional { get; set; }
        [XmlAttribute(AttributeName="DateFormat")]
        public string DateFormat { get; set; }
    }
}