using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Up2dateService.SetupManager
{
    public class ChocoNugetInfo
    {
        public ChocoNugetInfo(string id, string title, string version, string publisher)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException($@"'{nameof(id)}' cannot be null or whitespace.", nameof(id));

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
            using (var zipFile = ZipFile.OpenRead(fullFilePath))
            {
                var nuspec = zipFile.Entries.First(zipArchiveEntry => zipArchiveEntry.Name.Contains(".nuspec"));
                using (var nuspecStream = nuspec.Open())
                {
                    using (var sr = new StreamReader(nuspecStream, Encoding.UTF8))
                    {
                        var xmlData = sr.ReadToEnd();
                        var doc = new XmlDocument();
                        doc.LoadXml(xmlData);
                        var id = doc.GetElementsByTagName("id")[0] != null
                            ? doc.GetElementsByTagName("id")[0].InnerText
                            : string.Empty;
                        var title = doc.GetElementsByTagName("title")[0] != null
                            ? doc.GetElementsByTagName("title")[0].InnerText
                            : string.Empty;
                        var version = doc.GetElementsByTagName("version")[0] != null
                            ? doc.GetElementsByTagName("version")[0].InnerText
                            : string.Empty;
                        var publisher = doc.GetElementsByTagName("authors")[0] != null
                            ? doc.GetElementsByTagName("authors")[0].InnerText
                            : string.Empty;
                        return new ChocoNugetInfo(id, title, version, publisher);
                    }
                }
            }
        }
    }
}