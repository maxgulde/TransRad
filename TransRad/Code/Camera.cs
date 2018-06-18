/* Radiate Energy Transport Tests Project
 * 
 * Camera control. Coordinate system is Y up!
 * 
 * Author: Max Gulde
 * Last Update: 2018-05-28
 * 
 */

#region using

using System;
using Microsoft.Xna.Framework;

#endregion

namespace TransRad
{
    public class Camera
    {

        #region fields

        public Matrix Projection { get; private set; }
        public Matrix View { get; private set; }
        public Matrix World { get; private set; }

        public float Az { get; private set; }
        public float El { get; private set; }
        public float ImageSize { get; private set; }

        public bool IsPerspective { get; private set; }

        #endregion

        #region ctr

        public Camera(float imageSize, bool isPerspective = false)
        {
            // set view and projection matrices
            World = Matrix.Identity;
            View = Matrix.Identity;
            Projection = Matrix.Identity;

            // set elevation and azimuth
            Az = 0;
            El = 0;
            ImageSize = imageSize; // Can also be a field of view
            IsPerspective = isPerspective;

            // update
            Update();
        }

        #endregion

        #region update

        public void Update()
        {
            // compute position from angles
            Vector3 Position;
            Position.X = (float)(Math.Cos(MathHelper.ToRadians(El + Settings.C_ElevationOffset)) * Math.Cos(MathHelper.ToRadians(Az + Settings.C_AzimuthOffset)));
            Position.Z = (float)(Math.Cos(MathHelper.ToRadians(El + Settings.C_ElevationOffset)) * Math.Sin(MathHelper.ToRadians(Az + Settings.C_AzimuthOffset)));
            Position.Y = (float)Math.Sin(MathHelper.ToRadians(El + Settings.C_ElevationOffset));

            Vector3 Target = Vector3.Zero;
            SetPositionTarget(Position, Target);
        }

        public void SetPositionTarget(Vector3 position, Vector3 target, bool isPerspective = false)
        {
            // set view and projection matrices
            View = Matrix.CreateLookAt(position, target, Vector3.Up);
            if (IsPerspective)
            {
                Projection = Matrix.CreatePerspectiveFieldOfView(ImageSize, 1, Settings.C_NearPlane, Settings.C_FarPlane);
            }
            else
            {
                Projection = Matrix.CreateOrthographic(ImageSize, ImageSize, Settings.C_NearPlane, Settings.C_FarPlane);
            }
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

        public void TranslateX(float step)
        {
            World = Matrix.CreateTranslation(step, 0, 0);
        }

        public void TranslateZ(float step)
        {
            World = Matrix.CreateTranslation(0, 0, step);
        }

        public void Zoom(float zoom)
        {
            if (!IsPerspective)
            {
                ImageSize *= zoom;
                Update();
            }
        }

        #endregion
    }
}
