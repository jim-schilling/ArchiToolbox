using System.Xml.Linq;

namespace ArchiToolbox.Utility
{
    public static class XExtensions
    {
        public static bool HasAttribute(this XElement element, XName attributeName)
        {
            return element.Attribute(attributeName) != null;
        }
    }
}
