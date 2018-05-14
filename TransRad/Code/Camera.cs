/* Radiate Energy Transport Tests Project
 * 
 * Camera control.
 * 
 * Author: Max Gulde
 * Last Update: 2018-05-14
 * 
 */

#region using

using System;
using Microsoft.Xna.Framework;

#endregion

namespace TransRad
{
    class Camera
    {

        #region fields

        public Matrix Projection { get; private set; }
        public Matrix View { get; private set; }
        public Matrix World { get; private set; }

        public float Az { get; private set; }
        public float El { get; private set; }
        public float ImageSize { get; private set; }

        #endregion

        public Camera(float imageSize)
        {
            // set view and projection matrices
            World = Matrix.Identity;
            View = Matrix.Identity;
            Projection = Matrix.Identity;

            // set elevation and azimuth
            Az = 0;
            El = 0;
            ImageSize = imageSize;

            // update
            Update();
        }

        public void Update()
        {
            // compute position from angles
            Vector3 Position;
            Position.X = (float)(Math.Cos(MathHelper.ToRadians(-El + Settings.C_ElevationOffset)) * Math.Cos(MathHelper.ToRadians(Az + Settings.C_AzimuthOffset)));
            Position.Y = (float)(Math.Cos(MathHelper.ToRadians(-El + Settings.C_ElevationOffset)) * Math.Sin(MathHelper.ToRadians(Az + Settings.C_AzimuthOffset)));
            Position.Z = (float)Math.Sin(MathHelper.ToRadians(-El + Settings.C_ElevationOffset));

            Vector3 Target = Vector3.Zero;
            SetPositionTarget(Position, Target, ImageSize);
        }

        public void SetPositionTarget(Vector3 position, Vector3 target, float imageSize)
        {
            // set view and projection matrices
            View = Matrix.CreateLookAt(position, target, Vector3.Forward);
            Projection = Matrix.CreateOrthographic(imageSize, imageSize, Settings.C_NearPlane, Settings.C_FarPlane);
            //Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, NearPlane, FarPlane);
        }

        public void Rotate(float dAz, float dEl)
        {
            Az += dAz;
            El += dEl;
            // clamp
            Az = Az > Settings.C_MaxAz ? Az - 360 : Az;
            Az = Az < -Settings.C_MaxAz ? Az + 360 : Az;
            El = El > Settings.C_MaxEl ? Settings.C_MaxEl : El;
            El = El < -Settings.C_MaxEl ? -Settings.C_MaxEl : El;

            Update();
        }

        public void Orient(float az, float el)
        {
            Az = az;
            El = el;

            Update();
        }
    }
}
