﻿using QSP.UI.Models.FuelPlan;
using QSP.UI.Views.FuelPlan.Routes.Actions;
using System;

namespace QSP.UI.Views.FuelPlan.Routes
{
    public interface IRouteFinderView : ISupportActionContextMenu, IRefreshForNavDataChange
    {
        event EventHandler OrigIcaoChanged;
        event EventHandler DestIcaoChanged;
        
        string OrigIcao { get; }
        string DestIcao { get; }
        string OrigRwy { get; }
        string DestRwy { get; }
        
        IRouteFinderRowView OrigRow { get; }
        IRouteFinderRowView DestRow { get; }
    }

    public static class IRouteFinderViewExtension
    {
        public static bool IsAirportToAirport(this IRouteFinderView v)
        {
            return v.OrigRow.IsAirport && v.DestRow.IsAirport;
        }
    }
}