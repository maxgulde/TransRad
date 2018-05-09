/*
 * Author: Max Gulde
 * Last Update: 2018-05-09
 */

#region using

using System;
using Microsoft.Xna.Framework;

#endregion

namespace ObjectViewer
{
    public class Camera
    {
        #region settings

        public static float NearPlane = 0.001f;
        public static float FarPlane = 100f;
        public static float maxAz = 360;
        public static float maxEl = 89.999f;
        public static float ElevationOffset = 0;
        public static float AzimuthOffset = 90;
        public static float RotationSpeedAzimuth = 0.1f;
        public static float RotationSpeedElevation = 0.1f;
        public static int DisplaySize = 2;
        public static int ScreenSize = 800;
        public static int Distance = 1;
        public static float FoV = 90;
        bool PerspectiveView = false;

        #endregion

        #region fields

        public Matrix Projection { get; private set; }
        public Matrix View { get; private set; }
        public Matrix World { get; private set; }

        Vector3 Target;

        public float Az { get; private set; }
        public float El { get; private set; }

        #endregion

        public Camera()
        {
            // set view and projection matrices
            Projection = Matrix.Identity;
            View = Matrix.Identity;
            World = Matrix.Identity;

            // Target is always center
            Target = Vector3.Zero;

            // set elevation and azimuth
            Az = 0;
            El = 0;

            // update
            Update();
        }

        public void Update()
        {
            // compute position from angles
            Vector3 Position;
            Position.X = Distance * (float)(Math.Cos(MathHelper.ToRadians(-El + ElevationOffset)) * Math.Cos(MathHelper.ToRadians(Az + AzimuthOffset)));
            Position.Y = Distance * (float)(Math.Cos(MathHelper.ToRadians(-El + ElevationOffset)) * Math.Sin(MathHelper.ToRadians(Az + AzimuthOffset)));
            Position.Z = Distance * (float)Math.Sin(MathHelper.ToRadians(-El + ElevationOffset));

            // set view and projection matrices
            View = Matrix.CreateLookAt(Position, Target, Vector3.Forward);
            if (PerspectiveView)
            {
                Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FoV), 1, NearPlane, FarPlane);
            }
            else
            {
                Projection = Matrix.CreateOrthographic(DisplaySize, DisplaySize, NearPlane, FarPlane);
            }
        }

        public void Rotate(float dAz, float dEl)
        {
            Az += dAz * RotationSpeedAzimuth;
            El += dEl * RotationSpeedElevation;
            // clamp
            Az = Az > maxAz ? Az - 360 : Az;
            Az = Az < -maxAz ? Az + 360 : Az;
            El = El > maxEl ? maxEl : El;
            El = El < -maxEl ? -maxEl : El;

            Update();
        }

        public void Orient(float az, float el)
        {
            Az = az;
            El = el;

            Update();
        }

        public void SwitchViewMode()
        {
            PerspectiveView = !PerspectiveView;
        }
    }
}
