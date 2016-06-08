using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace QSP.Common.Options
{
    public class AppOptions
    {
        public string NavDataLocation { get; set; }
        public bool PromptBeforeExit { get; set; }
        public bool AutoDLTracks { get; set; }
        public bool AutoDLWind { get; set; }
        public List<RouteExportCommand> ExportCommands { get; set; }

        public AppOptions()
        {
            // Default options.
            NavDataLocation = "";
            PromptBeforeExit = true;
            AutoDLTracks = true;
            AutoDLWind = true;
            ExportCommands = new List<RouteExportCommand>();
        }

        public AppOptions(XDocument xmlFile)
        {
            var root = xmlFile.Root;

            NavDataLocation = root.Element("DatabasePath").Value;
            PromptBeforeExit = bool.Parse(
                root.Element("PromptBeforeExit").Value);
            AutoDLTracks = bool.Parse(root.Element("AutoDLNats").Value);
            AutoDLWind = bool.Parse(root.Element("AutoDLWind").Value);

            var exports = root.Element("ExportOptions");

            ExportCommands = new List<RouteExportCommand>();

            foreach (var i in exports.Elements())
            {
                ExportCommands.Add(
                    new RouteExportCommand(
                        i.Name.LocalName,
                        i.Element("Path").Value,
                        bool.Parse(i.Element("Enabled").Value)));
            }
        }

        public XElement ToXml()
        {
            var exports = new XElement[ExportCommands.Count];

            for (int i = 0; i < ExportCommands.Count; i++)
            {
                var command = ExportCommands[i];

                exports[i] = new XElement(
                    command.Format,
                    new XElement[] {
                        new XElement("Enabled", command.Enabled.ToString()),
                        new XElement("Path", command.FilePath)});
            }

            var exportOptions = new XElement("ExportOptions", exports);

            return new XElement("AppOptions", new XElement[] {
                new XElement("DatabasePath", NavDataLocation),
                new XElement("PromptBeforeExit", PromptBeforeExit.ToString()),
                new XElement("AutoDLNats", AutoDLTracks.ToString()),
                new XElement("AutoDLWind", AutoDLWind.ToString()),
                exportOptions});
        }

        public RouteExportCommand GetExportCommand(string format)
        {
            foreach (var i in ExportCommands)
            {
                if (i.Format == format)
                {
                    return i;
                }

            }
            return null;
        }

    }

    public class RouteExportCommand
    {
        public string Format { get; private set; }
        public string FilePath { get; private set; }
        public bool Enabled { get; private set; }

        public RouteExportCommand(string Format, string FilePath, bool Enabled)
        {
            this.Format = Format;
            this.FilePath = FilePath;
            this.Enabled = Enabled;
        }
    }
}
