﻿using QSP.AviationTools.Coordinates;
using System;
using QSP.RouteFinding.Data.Interfaces;
using static QSP.MathTools.Angles;

namespace QSP.MathTools.Vectors
{
    public static class Vector3DExtension
    {
        public static Vector3D LatLonToVector3D(double lat, double lon)
        {
            return Vector3D.FromSphericalCoords(
                1.0, Math.PI * 0.5 - ToRadian(lat), ToRadian(lon));
        }

        public static Vector3D ToVector3D(this ICoordinate item)
        {
            return LatLonToVector3D(item.Lat, item.Lon);
        }
        
        public static LatLon ToLatLon(this Vector3D item)
        {
            return new LatLon(
                ToDegree(Math.PI * 0.5 - item.Phi), ToDegree(item.Theta));
        }
    }
}
