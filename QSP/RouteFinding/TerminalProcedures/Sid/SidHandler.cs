using QSP.RouteFinding.Airports;
using QSP.RouteFinding.Containers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static QSP.Core.QspCore;
using static QSP.LibraryExtension.Lists;
using static QSP.RouteFinding.RouteFindingCore;
using static QSP.Utilities.ErrorLogger;
using QSP.RouteFinding.AirwayStructure;
using static QSP.RouteFinding.Utilities;
using static QSP.RouteFinding.WaypointAirwayConnector;

namespace QSP.RouteFinding.TerminalProcedures.Sid
{
    public class SidHandler
    {
        private string icao;
        private WaypointList wptList;
        private AirportManager airportList;
        private SidCollection sidCollection;

        public SidHandler(string icao) : this(icao, AppSettings.NavDBLocation, WptList, AirportList)
        {
        }

        public SidHandler(string icao, string navDBLocation, WaypointList wptList, AirportManager airportList)
        {
            this.icao = icao;
            this.wptList = wptList;
            this.airportList = airportList;
            ReadFromFile(navDBLocation);
        }
        
        /// <param name="navDBLocation">The file path, which is e.g., PROC\RCTP.txt\</param>
        /// <exception cref="LoadSidFileException"></exception>
        private void ReadFromFile(string navDBLocation)
        {
            string fileLocation = navDBLocation + "\\PROC\\" + icao + ".txt";
            string allTxt = null;

            try
            {
                allTxt = File.ReadAllText(fileLocation);
            }
            catch (Exception ex)
            {
                throw new LoadSidFileException("Failed to read " + fileLocation + ".", ex);
            }

            sidCollection = new SidReader(allTxt).Parse();
        }

        /// <summary>
        /// Find all SID available for the runway. Two SIDs only different in transitions are regarded as different. 
        /// If none is available an empty list is returned.
        /// </summary>
        /// <param name="rwy">Runway Ident</param>
        public List<string> GetSidList(string rwy)
        {
            return sidCollection.GetSidList(rwy);
        }

        /// <summary>
        /// Add necessary waypoints and neighbors for SID computation to WptList, and returns the index of Orig. rwy in WptList.
        /// </summary>
        public int AddSidsToWptList(string rwy, List<string> sid)
        {
            return new SidAdder(icao, sidCollection).AddSidsToWptList(rwy, sid);
        }

        /// <summary>
        /// Gets a tuple containing the name of SID/STAR and transition.
        /// </summary>
        public static Tuple<string, string> SplitSidStarTransition(string sidStar)
        {
            string sidName = null;
            string transName = null;

            if (sidStar.IndexOf('.') != -1)
            {
                sidName = sidStar.Substring(0, sidStar.IndexOf('.'));
                transName = sidStar.Substring(sidStar.IndexOf('.') + 1);
            }
            else
            {
                sidName = sidStar;
                transName = "";
            }
            return new Tuple<string, string>(sidName, transName);
        }

        /// <summary>
        /// Returns total distance of the SID and the last wpt, regardless whether the last wpt is in wptList.
        /// If there isn't any waypoint in the SID (e.g. a vector after takeoff), this returns a distance of 0.0   
        /// and the origin runway (e.g. KLAX25L).
        /// <param name="rwy">The runway identifier. e.g. 25R </param>
        /// <param name="origRwy">The waypoint representing the origin runway.</param>
        /// </summary>
        /// <exception cref="SidNotFoundException"></exception>
        public SidInfo InfoForAnalysis(string rwy, string sid)
        {
            return sidCollection.GetSidInfo(sid, rwy, new Waypoint(icao + rwy, airportList.RwyLatLon(icao, rwy)));
        }
    }
}