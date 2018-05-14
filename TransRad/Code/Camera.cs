using System;
using Microsoft.Xna.Framework;

namespace TransRad
{
    class Camera
    {

        #region fields

        public Matrix Projection { get; private set; }
        public Matrix View { get; private set; }
        public Matrix World { get; private set; }

        Vector3 Target;

        public float Az { get; private set; }
        public float El { get; private set; }
        const float MaxAz = 360;
        const float MaxEl = 89.999f;
        const float NearPlane = 0.01f;
        const float FarPlane = 10;
        const float ElevationOffset = 0;
        const float AzimuthOffset = 0;

        float Size;

        #endregion

        public Camera(float size)
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

            Size = size;

            // update
            Update();
        }

        public void Update()
        {
            // compute position from angles
            Vector3 Position;
            Position.X = (float)(Math.Cos(MathHelper.ToRadians(-El + ElevationOffset)) * Math.Cos(MathHelper.ToRadians(Az + AzimuthOffset)));
            Position.Y = (float)(Math.Cos(MathHelper.ToRadians(-El + ElevationOffset)) * Math.Sin(MathHelper.ToRadians(Az + AzimuthOffset)));
            Position.Z = (float)Math.Sin(MathHelper.ToRadians(-El + ElevationOffset));

            // set view and projection matrices
            View = Matrix.CreateLookAt(Position, Target, Vector3.Forward);
            Projection = Matrix.CreateOrthographic(Size, Size, NearPlane, FarPlane);
        }

        public void Rotate(float dAz, float dEl)
        {
            Az += dAz;
            El += dEl;
            // clamp
            Az = Az > MaxAz ? Az - 360 : Az;
            Az = Az < -MaxAz ? Az + 360 : Az;
            El = El > MaxEl ? MaxEl : El;
            El = El < -MaxEl ? -MaxEl : El;

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
