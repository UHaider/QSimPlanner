﻿using QSP.RouteFinding.AirwayStructure;
using QSP.RouteFinding.Containers;
using QSP.RouteFinding.Tracks.Interaction;
using System.Collections.Generic;

namespace QSP.RouteFinding.Tracks.Common
{
    public class TrackAdder
    {
        private WaypointList wptList;
        private WaypointListEditor editor;
        private StatusRecorder recorder;
        private TrackType type;

        public TrackAdder(WaypointList wptList,
                          WaypointListEditor editor,
                          StatusRecorder recorder,
                          TrackType type)
        {
            this.wptList = wptList;
            this.editor = editor;
            this.recorder = recorder;
            this.type = type;
        }

        public void AddToWaypointList<T>(T nodes)
            where T : IEnumerable<TrackNodes>
        {
            foreach (var i in nodes)
            {
                AddTrackToWptList(i);
            }
        }

        private void AddTrackToWptList(TrackNodes item)
        {
            try
            {
                AddMainRoute(item);

                foreach (var i in item.ConnectionRoutes)
                {
                    AddPairs(i);
                }
            }
            catch
            {
                recorder.AddEntry(
                    StatusRecorder.Severity.Caution,
                    "Failed to process track " + item.Ident + ".",
                    type);
            }
        }

        private void AddPairs(WptPair item)
        {
            double dis = wptList.Distance(item.IndexFrom, item.IndexTo);
            editor.AddNeighbor(
                item.IndexFrom,
                item.IndexTo,
                new Neighbor("DCT", AirwayType.Enroute, dis));
        }

        private void AddMainRoute(TrackNodes nodes)
        {
            var rte = nodes.MainRoute;

            int indexStart = AddFirstWpt(rte.FirstWaypoint);
            int indexEnd = AddLastWpt(rte.LastWaypoint);

            var neighbor = new Neighbor(
                nodes.AirwayIdent, AirwayType.Enroute, rte.GetTotalDistance());

            editor.AddNeighbor(indexStart, indexEnd, neighbor);
        }

        //returns the index of added wpt in wptList
        private int AddFirstWpt(Waypoint wpt)
        {
            int x = wptList.FindByWaypoint(wpt);

            if (x >= 0)
            {
                if (wptList.EdgesToCount(x) == 0)
                {
                    // No other wpt have this wpt as a neighbor, 
                    // need to find nearby wpt to connect

                    var k = WaypointAirwayConnector.ToAirway(
                        wpt.Lat, wpt.Lon, wptList);

                    foreach (var m in k)
                    {
                        int index = m.Index;
                        double dis = wptList.Distance(x, index);

                        editor.AddNeighbor(
                            index,
                            x,
                            new Neighbor("DCT", AirwayType.Enroute, dis));
                    }
                }
                return x;
            }

            throw new TrackWaypointNotFoundException(
                $"Waypoint {wpt.ID} is not found.");
        }

        private int AddLastWpt(Waypoint wpt)
        {
            int x = wptList.FindByWaypoint(wpt);

            if (x >= 0)
            {
                if (wptList.EdgesFromCount(x) == 0)
                {
                    var k = WaypointAirwayConnector.FromAirway(
                        wpt.Lat, wpt.Lon, wptList);

                    foreach (var m in k)
                    {
                        int index = m.Index;
                        double dis = wptList.Distance(x, index);

                        editor.AddNeighbor(x, index,
                            new Neighbor("DCT", AirwayType.Enroute, dis));
                    }
                }
                return x;
            }
            throw new TrackWaypointNotFoundException(
                $"Waypoint {wpt.ID} is not found.");
        }
    }
}
