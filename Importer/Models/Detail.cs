using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Importer.Models
{
    [XmlRoot(ElementName="Detail")]
    public class Detail
    {
        [XmlAttribute(AttributeName="Name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName="Description")]
        public string Description { get; set; }
        [XmlAttribute(AttributeName="MappingType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MappingType MappingType { get; set; }
        [XmlAttribute(AttributeName="Delimiter")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Delimiter Delimiter { get; set; }
        [XmlAttribute(AttributeName="HasHeaders")]
        public bool HasHeaders { get; set; }
    }
}