/* Radiate Energy Transport Tests Project
 * 
 * Data struct for single satellite component.
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-06
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
    public class SatComponent
    {
        #region fields

        public string Name { get; private set; }
        float DimX, DimY, DimZ;
        float AreaX, AreaY, AreaZ, AreaTotal;
        public ModelMesh Mesh { get; private set; }

        GraphicsDevice GFX;
        Effect Effect;

        public BoundingBox BBox { get; private set; }
        List<VertexPositionColor[]> BBoxVertices;

        #endregion

        #region ctr

        public SatComponent(ModelMesh mesh, Effect effect, GraphicsDevice gfx)
        {
            Mesh = mesh;
            Name = mesh.Name;
            GFX = gfx;
            Effect = effect;
            Console.WriteLine("\tLoaded component <" + Name + ">");
            CreareBoundingBox();
            UpdateEffect(effect);
        }

        #endregion

        #region bounding box

        private void CreareBoundingBox()
        {
            // Initialize minimum and maximum corners of the bounding box to max and min values
            Vector3 Min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 Max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            // Loop all parts of the mesh to get bounding box size.
            foreach (ModelMeshPart meshPart in Mesh.MeshParts)
            {
                // Vertex buffer parameters
                int VertexStride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                int VertexBufferSize = meshPart.NumVertices * VertexStride;

                // Get vertex data as float
                float[] VertexData = new float[VertexBufferSize / sizeof(float)];
                meshPart.VertexBuffer.GetData(VertexData);

                // Iterate through vertices (possibly) growing bounding box, all calculations are done in world space
                for (int i = 0; i < VertexBufferSize / sizeof(float); i += VertexStride / sizeof(float))
                {
                    //Vector3 transformedPosition = Vector3.Transform(new Vector3(VertexData[i], VertexData[i + 1], VertexData[i + 2]), worldTransform);
                    Vector3 Position = new Vector3(VertexData[i], VertexData[i + 1], VertexData[i + 2]);

                    Min = Vector3.Min(Min, Position);
                    Max = Vector3.Max(Max, Position);
                }
            }
            BBox = new BoundingBox(Min, Max);

            // Compute dimensions and areas.
            ComputeDimensions();

            // Create bounding box vertices.
            Vector3[] Corners = BBox.GetCorners();
            BBoxVertices = new List<VertexPositionColor[]>()
            {
                GetFaceVertices(Corners, new int[] { 0, 1, 2, 0, 2, 3 }),   // front
                GetFaceVertices(Corners, new int[] { 4, 5, 6, 4, 6, 7 }),   // back
                GetFaceVertices(Corners, new int[] { 4, 0, 3, 4, 3, 7 }),   // left
                GetFaceVertices(Corners, new int[] { 5, 1, 2, 5, 2, 6 }),   // right
                GetFaceVertices(Corners, new int[] { 3, 2, 6, 3, 6, 7 }),   // top
                GetFaceVertices(Corners, new int[] { 4, 5, 1, 4, 1, 0 })    // bottom
            };

            Console.WriteLine("\tCreated Bounding Box  of size " + DimX.ToString("F3") + " x " + DimY.ToString("F3") + " x " + DimZ.ToString("F3") + ".");
        }

        private Vector3 GetBoundingBoxCenter()
        {
            Vector3 Max = BBox.Max;
            Vector3 Min = BBox.Min;
            return Min + (Max - Min) / 2;
        }

        #endregion

        #region dimension

        private void ComputeDimensions()
        {
            // Lengths
            DimX = BBox.Max.X - BBox.Min.X;
            DimY = BBox.Max.Y - BBox.Min.Y;
            DimZ = BBox.Max.Z - BBox.Min.Z;

            // Surface areas
            AreaX = DimY * DimZ;
            AreaY = DimY * DimZ;
            AreaZ = DimX * DimY;
            AreaTotal = 2 * (AreaX + AreaY + AreaZ);
        }

        public float GetBBoxFaceArea(AAFace face)
        {
            switch (face)
            {
                case AAFace.XMinus:
                case AAFace.XPlus:
                    return AreaX;
                case AAFace.YMinus:
                case AAFace.YPlus:
                    return AreaY;
                case AAFace.ZMinus:
                case AAFace.ZPlus:
                    return AreaZ;
                default:
                    return -1;
            }
        }

        public float GetBBoxTotalArea()
        {
            return AreaTotal;
        }

        #endregion

        #region faces

        private VertexPositionColor[] GetFaceVertices(Vector3[] corners, int[] idx)
        {
            VertexPositionColor[] Verts = new VertexPositionColor[6];
            for (int i = 0; i < idx.Length; i++)
            {
                Verts[i] = new VertexPositionColor(corners[idx[i]], Settings.F_BBoxColor);
            }
            return Verts;
        }

        public Vector3 GetFaceCenter(AAFace face)
        {
            Vector3 Center = GetBoundingBoxCenter();
            switch (face)
            {
                case AAFace.XMinus:
                    Center.X -= DimX / 2;
                    break;
                case AAFace.XPlus:
                    Center.X += DimX / 2;
                    break;
                case AAFace.YMinus:
                    Center.Y -= DimY / 2;
                    break;
                case AAFace.YPlus:
                    Center.Y += DimY / 2;
                    break;
                case AAFace.ZMinus:
                    Center.Z -= DimZ / 2;
                    break;
                case AAFace.ZPlus:
                    Center.Z += DimZ / 2;
                    break;
            }

            return Center;
        }

        public Vector3 GetFaceDirection(Vector3 cameraPosition, AAFace face)
        {
            Vector3 Direction = cameraPosition;
            switch (face)
            {
                case AAFace.XMinus:
                    Direction.X -= 0.1f;
                    break;
                case AAFace.XPlus:
                    Direction.X += 0.1f;
                    break;
                case AAFace.YMinus:
                    Direction.Y -= 0.1f;
                    Direction.X += 0.00001f; // Include a small tilt to avoid a Gimbal Lock
                    break;
                case AAFace.YPlus:
                    Direction.Y += 0.1f;
                    Direction.X += 0.00001f;
                    break;
                case AAFace.ZMinus:
                    Direction.Z -= 0.1f;
                    break;
                case AAFace.ZPlus:
                    Direction.Z += 0.1f;
                    break;
            }
            return Direction;
        }

        #endregion

        #region effect

        private void UpdateEffect(Effect effect)
        {
            // Loop all parts of the mesh
            foreach (ModelMeshPart meshPart in Mesh.MeshParts)
            {
                meshPart.Effect = effect;
            }
        }

        #endregion

        #region draw

        public void DrawMesh(Camera cam, Vector3 color)
        {
            foreach (Effect eff in Mesh.Effects)
            {
                eff.CurrentTechnique = eff.Techniques["BasicColorDrawing"];
                eff.Parameters["WorldViewProjection"].SetValue(cam.World * cam.View * cam.Projection);
                eff.Parameters["ComponentColor"].SetValue(color);
                eff.Parameters["Alpha"].SetValue(1.0f);
            }
            Mesh.Draw();
        }

        public void DrawBoundingBox(Camera cam)
        {
            foreach (AAFace face in Tools.GetEnumValues<AAFace>())
            {
                DrawBoundingBoxFace(cam, face);
            }
        }

        public void DrawBoundingBoxFace(Camera cam, AAFace face)
        {
            // Setup effect
            Effect.CurrentTechnique = Effect.Techniques["AlphaColorDrawing"];
            Effect.Parameters["WorldViewProjection"].SetValue(cam.World * cam.View * cam.Projection);
            Effect.Parameters["ComponentColor"].SetValue(Settings.F_BBoxColor.ToVector3());
            Effect.Parameters["Alpha"].SetValue(Settings.D_BBoxAlpha);

            // Get vertices
            VertexPositionColor[] Verts = BBoxVertices[(int)face];

            // Draw vertices
            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GFX.DrawUserPrimitives(PrimitiveType.TriangleList, Verts, 0, Verts.Length / 3);
            }
        }

        #endregion
    }
}
