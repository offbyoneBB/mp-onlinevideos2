using System.Xml;
using System;

namespace Vattenmelon.Nrk.Parser.Xml
{
    public class XmlParser
    {
        protected XmlDocument doc;
        protected string url;

        protected void InternalLoadXmlDocument()
        {
            doc = new XmlDocument();
            XmlTextReader reader = new XmlTextReader(url);
            doc.Load(reader);
            reader.Close();
        }

    }
}
