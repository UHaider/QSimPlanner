using System.Collections.Generic;
using System.Linq;

namespace QSP.WindAloft
{
    public static class DescendForcast
    {
        public static IEnumerable<(Wind, double temp)> Generate(IWxTableCollection windTables,
            double lat, double lon, IEnumerable<double> flightLevels)
        {
            return flightLevels.Select(fl =>
            {
                var UVWind = windTables.GetWindUV(lat, lon, fl * 100.0);
                var temp = windTables.GetTemp(lat, lon, fl * 100.0);
                return (Wind.FromUV(UVWind), temp);
            });
        }
    }
}
