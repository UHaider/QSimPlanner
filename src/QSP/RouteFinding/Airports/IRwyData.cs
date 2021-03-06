﻿using QSP.RouteFinding.Data.Interfaces;
using System;

namespace QSP.RouteFinding.Airports
{
    public interface IRwyData: ICoordinate, IEquatable<IRwyData>
    {
        string RwyIdent { get; }
        string Heading { get; }
        int LengthFt { get; }
        int WidthFt { get; }
        bool HasIlsInfo { get; }

        // The following 3 values can be used only if HasIlsInfo is true.
        bool IlsAvail { get; }
        string IlsFreq { get; }
        string IlsHeading { get; }
        
        int ElevationFt { get; }
        double GlideslopeAngle { get; }
        int ThresholdOverflyHeight { get; }
        string SurfaceType { get; }
        int RwyStatus { get; }
    }
}
