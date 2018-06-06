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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace TransRad
{
    public static class Tools
    {

        #region mesh

        public static Model ApplyCustomEffect(Model model, Effect effect)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = effect;
                }
            }

            return model;
        }

        public static Vector3 GetMeshCenter(ModelMesh m)
        {
            return m.BoundingSphere.Center;
        }

        public static float GetMeshDiameter(ModelMesh m)
        {
            return 2 * m.BoundingSphere.Radius;
        }

        #endregion

        #region math

        public static Color GetColorFromIndex(int index, int maxIndex)
        {
            float MinValue = 0.2f;
            float MaxValue = 0.8f;

            float ColorStep = (MaxValue - MinValue) / (maxIndex - 1);
            float GrayValue = MinValue + index * ColorStep;

            return new Color(GrayValue, GrayValue, GrayValue);
        }

        #endregion

        #region io

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

        #endregion

        #region misc

        // Returns a list with all values within the enumeration.
        public static List<T> GetEnumValues<T>()
        {
            List<T> List = new List<T>();
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                List.Add(item);
            }
            return List;
        }


        #endregion
    }

    #region types

    public enum AAFace
    {
        XPlus,
        XMinus,
        YPlus,
        YMinus,
        ZPlus,
        ZMinus
    }

    #endregion
}
