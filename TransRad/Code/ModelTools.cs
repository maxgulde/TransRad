/* Radiate Energy Transport Tests Project
 * 
 * Toolset
 * 
 * Author: Max Gulde
 * Last Update: 2018-05-14
 * 
 */

#region using

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

    }
}
