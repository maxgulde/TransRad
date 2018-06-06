/* Radiate Energy Transport Tests Project
 * 
 * Global settings
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-06
 * 
 */

#region using

using Microsoft.Xna.Framework;

#endregion

namespace TransRad
{
    static class Settings
    {
        public static int D_ScreenWidth = 2048;
        public static int D_ScreenHeight = D_ScreenWidth / 2;
        public static int D_ViewportSize = D_ScreenWidth / 2;
        public static int D_PixelPerViewport = D_ViewportSize * D_ViewportSize;
        public static int D_FontPadding = 10;
        public static float D_DefaultImageSize = 4f;
        public static float D_PointerScale = 0.1f;
        public static int D_HemiCubeResolution = D_ScreenWidth / 4;
        public static float D_BBoxAlpha = 0.3f;

        public static int I_KeyDelay = 200;

        public static float C_RotSpeedAz = 1;
        public static float C_RotSpeedEl = 1;
        public static float C_MaxAz = 360;
        public static float C_MaxEl = 89.999f;
        public static float C_NearPlane = 0.001f;
        public static float C_FarPlane = 10;
        public static float C_ElevationOffset = 0;
        public static float C_AzimuthOffset = 0;

        public static Color F_BBoxColor = Color.DarkBlue;

        public static bool f_DrawBoundingBoxes = false;
        public static bool f_DrawPointer = true;
        public static bool f_DrawMultiplierMap = false;
        public static bool f_ComputeArea = false;
        public static bool f_ComputationRunning = false;
    }
}
