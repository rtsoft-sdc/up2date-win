using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Up2dateService.Installers.Choco
{
    public class ChocoNugetInfo
    {
        private ChocoNugetInfo(string id, string title, string version, string publisher)
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
            try
            {
                using (ZipArchive zipFile = ZipFile.OpenRead(fullFilePath))
                {
                    ZipArchiveEntry nuspec = zipFile.Entries.FirstOrDefault(zipArchiveEntry => zipArchiveEntry.Name.Contains(".nuspec"));
                    if (nuspec == null) return null;

                    using (Stream nuspecStream = nuspec.Open())
                    {
                        using (StreamReader sr = new StreamReader(nuspecStream, Encoding.UTF8))
                        {
                            string xmlData = sr.ReadToEnd();
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(xmlData);
                            string id = doc.GetElementsByTagName("id").Count > 0
                                ? doc.GetElementsByTagName("id")[0].InnerText
                                : null;
                            string title = doc.GetElementsByTagName("title").Count > 0
                                ? doc.GetElementsByTagName("title")[0].InnerText
                                : null;
                            string version = doc.GetElementsByTagName("version").Count > 0
                                ? doc.GetElementsByTagName("version")[0].InnerText
                                : null;
                            string publisher = doc.GetElementsByTagName("authors").Count > 0
                                ? doc.GetElementsByTagName("authors")[0].InnerText
                                : null;
                            return new ChocoNugetInfo(id, title, version, publisher);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}