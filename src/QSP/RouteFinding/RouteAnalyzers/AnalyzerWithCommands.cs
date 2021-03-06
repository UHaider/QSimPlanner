﻿using QSP.AviationTools.Coordinates;
using QSP.RouteFinding.Airports;
using QSP.RouteFinding.AirwayStructure;
using QSP.RouteFinding.Containers;
using QSP.RouteFinding.Data.Interfaces;
using QSP.RouteFinding.Finder;
using QSP.RouteFinding.RandomRoutes;
using QSP.RouteFinding.RouteAnalyzers.Extractors;
using QSP.RouteFinding.Routes;
using QSP.RouteFinding.TerminalProcedures.Sid;
using QSP.RouteFinding.TerminalProcedures.Star;
using System;
using System.Collections.Generic;
using System.Linq;
using static QSP.RouteFinding.Routes.RouteExtensions;

namespace QSP.RouteFinding.RouteAnalyzers
{
    // Uses BasicRouteAnalyzer, with the additional functionality of 
    // reading airports and SIDs/STARs.
    //
    // 1. Input: The string array, consisting of airport icao code (ICAO),
    //    airway (AWY), waypoint (WPT), "AUTO" and "RAND" sysbols.
    //           
    // 2. All characters should be capital.
    //
    // 3. Format: {ICAO, SID, WPT, AWY, WPT, ... , WPT, STAR, ICAO}
    //    (1) First ICAO must be identical to origin icao code. Last 
    //        ICAO must be identical to dest icao code.
    //    (2) Both ICAO can be omitted.
    //    (3) If an airway is DCT (direct), it has to be omitted. The 
    //        route will be a direct between the two waypoints.
    //    (4) SID/STAR can be omitted. The route will be a direct from/to 
    //        airport.
    //    (5) All WPT, except for the last waypoint of the SID or first 
    //        one of the STAR, must either exist in wptList or is represented 
    //        with lat/lon (COORD). In the latter case, the format must be supported
    //        by method Formatter.ParseLatLon.
    //    (6) "AUTO" or "RAND" can only appear before or after ICAO, or 
    //        between two waypoints.
    //    (7) If the route is empty, a direct route from origin to 
    //        destination runway is returned.
    //             
    // 4. All cases of SID/STAR, specified in SidAdder/StarAdder are 
    //    supported. 
    //    (1) Suppose SID1 ends with a vector to a waypoint P, where P is 
    //        in wptList. "SID1 P ..." is valid.

    //        (The reason for this rule is that the route string 
    //        generated by our RouteFinder can be parsed correctly.)
    //
    // 5. If the format is wrong, an InvalidIdentifierException will be
    //    thrown with an message describing the place where the 
    //    problem occurs.
    //
    // 6. "AUTO" finds the shortest route between the specified waypoints.
    //     If it's the first entry, then a route between departure runway 
    //     and first waypoint is found. The case for last entry is similar.
    //     Similarly, "RAND" finds a random route.
    //
    public class AnalyzerWithCommands
    {
        private readonly WaypointList wptList;
        private readonly WaypointListEditor editor;
        private readonly AirportManager airportList;
        private readonly SidCollection sids;
        private readonly StarCollection stars;

        private readonly string origIcao;
        private readonly string origRwy;
        private readonly string destIcao;
        private readonly string destRwy;
        private RouteString route;

        private Waypoint origRwyWpt;
        private Waypoint destRwyWpt;

        public AnalyzerWithCommands(
            RouteString route,
            string origIcao,
            string origRwy,
            string destIcao,
            string destRwy,
            AirportManager airportList,
            WaypointList wptList,
            WaypointListEditor editor,
            SidCollection sids,
            StarCollection stars)
        {
            this.route = route;
            this.origIcao = origIcao;
            this.origRwy = origRwy;
            this.destIcao = destIcao;
            this.destRwy = destRwy;
            this.airportList = airportList;
            this.wptList = wptList;
            this.editor = editor;
            this.sids = sids;
            this.stars = stars;
        }

        /// <exception cref="Exception"></exception>
        public Route Analyze()
        {
            SetRwyWpts();
            if (route.Count == 0) return DirectRoute();

            EnsureNoConsectiveCommands(route);
            route = RemoveIcaos(route);
            AddLatLonWaypointsToEditor();

            var subRoutes = EntryGrouping.Group(new RouteString(route));
            var analyzed = TransformSubRoutes(subRoutes);
            var final = ComputeCommands(analyzed);

            editor.Undo();

            return final.Connect();
        }

        private void AddLatLonWaypointsToEditor()
        {
            foreach (var i in route)
            {
                var coords = Formatter.ParseLatLon(i);
                if (coords != null)
                {
                    editor.AddWaypoint(new Waypoint(i, coords));
                }
            }
        }
        
        private Route DirectRoute()
        {
            var route = new Route();
            route.AddLastWaypoint(origRwyWpt);
            route.AddLastWaypoint(destRwyWpt, "DCT");
            return route;
        }

        private RouteString RemoveIcaos(RouteString route)
        {
            bool firstIsIcao = route[0] == origIcao;
            bool lastIsIcao = route.Last() == destIcao;

            int skipHead = firstIsIcao ? 1 : 0;
            int skipTail = lastIsIcao ? 1 : 0;

            return route
                .Skip(skipHead)
                .Take(route.Count - skipHead - skipTail)
                .ToRouteString();
        }

        private static void EnsureNoConsectiveCommands(RouteString route)
        {
            string[] commands = { "AUTO", "RAND" };
            for (int i = 0; i < route.Count - 1; i++)
            {
                var first = route[i];
                var second = route[i + 1];

                if (commands.Contains(first) && commands.Contains(second))
                {
                    throw new ArgumentException($"{first} cannot be followed by {second}");
                }
            }
        }

        private void SetRwyWpts()
        {
            origRwyWpt = new Waypoint(
                origIcao + origRwy,
                airportList.FindRwy(origIcao, origRwy));

            destRwyWpt = new Waypoint(
                destIcao + destRwy,
                airportList.FindRwy(destIcao, destRwy));
        }

        private IReadOnlyList<SubRoute> TransformSubRoutes(IReadOnlyList<RouteSegment> segments)
        {
            return segments.Select((seg, index) =>
            {
                if (seg.IsAuto) return SubRoute.Auto();
                if (seg.IsRand) return SubRoute.Rand();

                var count = segments.Count;
                var routeStr = seg.RouteString;
                if (index == 0) return ComputeTerminalRoute(routeStr, true, count == 1);
                if (index == count - 1) return ComputeTerminalRoute(routeStr, false, true);
                return GetAutoSelectRoute(routeStr).ToSubRoute();
            }).ToList();
        }

        private Route GetAutoSelectRoute(RouteString r)
        {
            var analyzer = new AutoSelectAnalyzer(r, origRwyWpt, destRwyWpt, wptList);
            return analyzer.Analyze();
        }

        private SubRoute ComputeTerminalRoute(RouteString item, bool isOrig, bool isDest)
        {
            Route origRoute = null;
            Route destRoute = null;

            if (isOrig)
            {
                var sidExtract = new SidExtractor(item, origRwy, origRwyWpt, wptList, sids)
                    .Extract();

                origRoute = sidExtract.OrigRoute;
                item = sidExtract.RemainingRoute;
            }

            if (isDest)
            {
                var starExtract = new StarExtractor(item, destRwy, destRwyWpt, wptList, stars)
                    .Extract();

                destRoute = starExtract.DestRoute;
                item = starExtract.RemainingRoute;
            }

            var remainRoute = origRoute == null
                ? GetAutoSelectRoute(item)
                : GetBasicRoute(item, wptList.FindByWaypoint(origRoute.LastWaypoint));

            return new[] { origRoute, remainRoute, destRoute }
                .Where(r => r != null)
                .Connect()
                .ToSubRoute();
        }

        private Route GetBasicRoute(RouteString r, int firstWptIndex)
        {
            var analyzer = new BasicRouteAnalyzer(r, wptList, firstWptIndex);
            return analyzer.Analyze();
        }

        private List<Route> ComputeCommands(IReadOnlyList<SubRoute> analyzed)
        {
            return analyzed.Select((subRoute, index) =>
            {
                if (subRoute.IsAuto) return FindRoute(analyzed, index);
                if (subRoute.IsRand) return GetRandRoute(analyzed, index);
                return subRoute.Route;
            }).ToList();
        }

        private Route GetRandRoute(IReadOnlyList<SubRoute> analyzed, int index)
        {
            var startEnd = GetStartEndWpts(analyzed, index);

            var randRoute = FinderFactory.GetInstance()
                .Find(startEnd.Start, startEnd.End)
                .ToRoute();

            // The first and last waypoint idents generated by the 
            // random route are based on lat/lon and may be different
            // than expected.
            randRoute.Nodes.RemoveFirst();
            randRoute.AddFirstWaypoint(startEnd.Start, "DCT");

            randRoute.Nodes.RemoveLast();
            randRoute.AddLastWaypoint(startEnd.End, "DCT");

            return randRoute;
        }

        /// <exception cref="Exception"></exception>
        private Route FindRoute(IReadOnlyList<SubRoute> analyzed, int index)
        {
            var routeFinder = new RouteFinder(wptList);

            if (index == 0)
            {
                if (index == analyzed.Count - 1)
                {
                    return new RouteFinderFacade(wptList, airportList)
                        .FindRoute(
                            origIcao, origRwy, sids, sids.GetSidList(origRwy),
                            destIcao, destRwy, stars, stars.GetStarList(destRwy));
                }
                else
                {
                    int wptTo = wptList.FindByWaypoint(analyzed[index + 1].Route.FirstWaypoint);

                    return new RouteFinderFacade(wptList, airportList)
                        .FindRoute(origIcao, origRwy, sids, sids.GetSidList(origRwy), wptTo);
                }
            }
            else
            {
                if (index == analyzed.Count - 1)
                {
                    int wptFrom = wptList.FindByWaypoint(analyzed[index - 1].Route.LastWaypoint);

                    return new RouteFinderFacade(wptList, airportList)
                         .FindRoute(wptFrom, destIcao, destRwy, stars, stars.GetStarList(destRwy));
                }
                else
                {
                    int wptFrom = wptList.FindByWaypoint(analyzed[index - 1].Route.LastWaypoint);
                    int wptTo = wptList.FindByWaypoint(analyzed[index + 1].Route.FirstWaypoint);
                    return routeFinder.FindRoute(wptFrom, wptTo);
                }
            }
        }

        private WptPair GetStartEndWpts(IReadOnlyList<SubRoute> subRoutes, int index)
        {
            var start = index == 0
                ? origRwyWpt
                : subRoutes[index - 1].Route.LastWaypoint;

            var end = index == subRoutes.Count - 1
                ? destRwyWpt
                : subRoutes[index + 1].Route.FirstWaypoint;

            return new WptPair() { Start = start, End = end };
        }

        private struct WptPair { public Waypoint Start, End; }
    }
}
