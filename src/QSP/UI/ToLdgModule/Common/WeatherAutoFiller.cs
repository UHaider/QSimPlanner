﻿using QSP.MathTools;
using QSP.Metar;
using System;
using System.Windows.Forms;
using static QSP.MathTools.Doubles;

namespace QSP.UI.ToLdgModule.Common
{
    public static class WeatherAutoFiller
    {
        /// <summary>
        /// Returns whether the operation was successful.
        /// </summary>
        public static bool Fill(
            string metar,
            TextBox windDirection,
            TextBox windSpeed,
            TextBox oat,
            ComboBox tempUnit,
            TextBox altimeter,
            ComboBox pressUnit)
        {
            var wind = ParaExtractor.GetWind(metar);
            int temp = ParaExtractor.GetTemp(metar);
            var press = ParaExtractor.GetPressure(metar);

            if (wind == null ||
                temp == int.MinValue ||
                press == null)
            {
                return false;
            }
            else
            {
                windSpeed.Text = RoundToInt(wind.Speed).ToString();

                windDirection.Text = 
                    ((RoundToInt( wind.Direction) - 1)
                    .Mod(360) + 1)
                    .ToString()
                    .PadLeft(3, '0');

                tempUnit.SelectedIndex = 0;
                oat.Text = temp.ToString();
                pressUnit.SelectedIndex = (int)press.PressUnit;
                altimeter.Text =
                    press.PressUnit == PressureUnit.inHg ?
                    Math.Round(press.Value, 2).ToString() :
                    RoundToInt(press.Value).ToString();

                return true;
            }
        }
    }
}