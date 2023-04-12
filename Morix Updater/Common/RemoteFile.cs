using System.Xml;

namespace Morix
{
    public class RemoteFile
    {
        public string Name { get; set; }
        public string Version { get; set; }
        
        public RemoteFile(RemoteFile file)
        {
            this.Name = file.Name;
            this.Version = file.Version;
        }

        public RemoteFile(XmlNode node)
        {
            this.Name = node.Attributes["name"].Value;
            this.Version = node.Attributes["version"].Value;
        }
    }
}