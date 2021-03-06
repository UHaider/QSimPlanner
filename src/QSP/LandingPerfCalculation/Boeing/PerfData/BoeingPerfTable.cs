﻿namespace QSP.LandingPerfCalculation.Boeing.PerfData
{
    // Landing performance table for various runway surface conditions, 
    // flap setting, etc.
    //
    // Every aircraft corresponds to one PerfTable.
    //
    public class BoeingPerfTable : IPerfTableItem
    {
        // All lengths in meter, all weights in KG.
        public double WeightRef { get; private set; }
        public double WeightStep { get; private set; }

        private string[] autoBrkDry;
        private string[] autoBrkWet;

        public TableDry DataDry { get; private set; }
        public TableWet DataWet { get; private set; }

        public string[] BrakesAvailable(SurfaceCondition item)
        {
            return item == SurfaceCondition.Dry ? autoBrkDry : autoBrkWet;
        }

        public string[] Flaps { get; private set; }     
        public string[] Reversers { get; private set; }
        public double Multiplier { get; set; } = 1;

        public BoeingPerfTable(
            double WeightRef,
            double WeightStep,
            string[] autoBrkDry,
            string[] autoBrkWet,
            string[] Flaps,
            string[] Reversers,
            TableDry DataDry,
            TableWet DataWet)
        {
            this.WeightRef = WeightRef;
            this.WeightStep = WeightStep;
            this.autoBrkDry = autoBrkDry;
            this.autoBrkWet = autoBrkWet;
            this.Flaps = Flaps;
            this.Reversers = Reversers;
            this.DataDry = DataDry;
            this.DataWet = DataWet;
        }
    }
}
