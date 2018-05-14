/* Radiate Energy Transport Tests Project
 * 
 * Global settings
 * 
 * Author: Max Gulde
 * Last Update: 2018-05-14
 * 
 */

#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace TransRad
{
    static class Settings
    {
        public static int D_ScreenWidth = 1000;
        public static int D_ScreenHeight = 500;
        public static int D_ViewportSize = D_ScreenWidth / 2;

        public static int I_KeyDelay = 200;

        public static float C_RotSpeedAz = 1;
        public static float C_RotSpeedEl = 1;
        public static float C_MaxAz = 360;
        public static float C_MaxEl = 89.999f;
        public static float C_NearPlane = 0.01f;
        public static float C_FarPlane = 10;
        public static float C_ElevationOffset = 0;
        public static float C_AzimuthOffset = 0;
    }
}
