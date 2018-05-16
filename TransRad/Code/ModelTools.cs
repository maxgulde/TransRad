/* Radiate Energy Transport Tests Project
 * 
 * Toolset
 * 
 * Author: Max Gulde
 * Last Update: 2018-05-14
 * 
 */

#region using

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace TransRad
{
    public static class Tools
    {
        public static Vector3 GetMeshCenter(ModelMesh m)
        {
            return m.BoundingSphere.Center;
        }

        public static float GetMeshDiameter(ModelMesh m)
        {
            return 2 * m.BoundingSphere.Radius;
        }

        public static void PrintMatrix(float[,] m)
        {
            int Rows = m.GetLength(0);
            int Cols = m.GetLength(1);

            for (int cc = 0; cc < Cols; cc++)
            {
                for (int rr = 0; rr < Rows; rr++)
                {
                    Console.Write(m[cc, rr].ToString("F6") + ", ");
                }
                Console.WriteLine();
            }
        }

    }
}
