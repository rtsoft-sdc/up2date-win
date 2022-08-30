using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Xml;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class ChocoNugetInfo
    {
        public ChocoNugetInfo(string id, string title, string version, string publisher)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($@"'{nameof(id)}' cannot be null or whitespace.", nameof(id));
            }

            Id = id;
            Title = title;
            Version = version;
            Publisher = publisher;
        }

        public string Id { get; }
        public string Title { get; }
        public string Version { get; }
        public string Publisher { get; }

        public static ChocoNugetInfo GetInfo(string fullFilePath)
        {
            using (ZipArchive zipFile = ZipFile.OpenRead(fullFilePath))
            {
                ZipArchiveEntry nuspec = zipFile.Entries.First(zipArchiveEntry => zipArchiveEntry.Name.Contains(".nuspec"));
                using (Stream nuspecStream = nuspec.Open())
                {
                    using (StreamReader sr = new StreamReader(nuspecStream, Encoding.UTF8))
                    {
                        string xmlData = sr.ReadToEnd();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlData);
                        string id = doc.GetElementsByTagName("id")[0].InnerText;
                        string title = doc.GetElementsByTagName("title")[0].InnerText;
                        string version = doc.GetElementsByTagName("version")[0].InnerText;
                        string publisher = doc.GetElementsByTagName("authors")[0].InnerText;
                        return new ChocoNugetInfo(id, title, version, publisher);
                    }
                }
            }
        }

    }
}