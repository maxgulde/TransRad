/* Radiate Energy Transport Tests Project
 * 
 * HemiCube data structure: Order is front, left, right, up, down
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-06
 * 
 */

#region using

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace TransRad
{
    public class HemiCube
    {
        #region fields

        public List<RenderTarget2D> RTIndividual { get; private set; }
        public Texture2D MultiplierMap { get; private set; }
        public Texture2D MultiplierMapN { get; private set; }
        public Texture2D UnityMap { get; private set; }
        //public Texture2D UnityMapN { get; private set; }
        public float CoveredAreaRatio { get; private set; }

        #endregion

        #region ctr

        public HemiCube(GraphicsDevice gfx)
        {
            // Set up rendertargets
            RTIndividual = new List<RenderTarget2D>();
            for (int i = 0; i < 5; i++)
            {
                RTIndividual.Add(new RenderTarget2D(gfx, Settings.D_HemiCubeResolution, Settings.D_HemiCubeResolution, false, SurfaceFormat.Color, DepthFormat.Depth16));
            }
            // Create multiplier map
            Stopwatch SW = new Stopwatch();
            SW.Start();
            GenerateMultiplierMap(gfx);
            long time = SW.ElapsedMilliseconds;
            SW.Stop();
            Console.WriteLine("Created multiplier map (" + time.ToString() + " ms).");
        }

        #endregion

        #region multiplier map

        // See http://web.archive.org/web/20071001024020/http://freespace.virgin.net/hugo.elias/radiosity/radiosity.htm for a tutorial

        void GenerateMultiplierMap(GraphicsDevice gfx)
        {
            int HemiCubeSize = Settings.D_HemiCubeResolution;
            int MapSize = HemiCubeSize * 2;
            int PixelsPerMap = MapSize * MapSize;
            int CutoutSize = HemiCubeSize / 2;
            Vector2 Center = new Vector2(HemiCubeSize, HemiCubeSize);
            Vector2 BorderL = new Vector2(0, HemiCubeSize);
            Vector2 BorderR = new Vector2(MapSize, HemiCubeSize);
            Vector2 BorderU = new Vector2(HemiCubeSize, 0);
            Vector2 BorderD = new Vector2(HemiCubeSize, MapSize);
            List<float> Angles = new List<float>();
            float Background = 0;

            // Ratio of used to total pixels in texture
            CoveredAreaRatio = (float)(PixelsPerMap - HemiCubeSize * HemiCubeSize) / PixelsPerMap;

            // Create arrays for result storage
            float[] Map = new float[PixelsPerMap];
            float[] MapUnity = new float[PixelsPerMap];
            float Sum = 0;

            for (int i = 0; i < PixelsPerMap; i++)
            {
                int x = i % MapSize;
                int y = (int)Math.Floor((double)i / MapSize);
                Vector2 xy = new Vector2(x, y);
                Angles.Clear();

                #region crop out edges

                if (x < CutoutSize && y < CutoutSize)
                {
                    Map[i] = Background;
                    continue;
                }
                if (x < CutoutSize && y >= HemiCubeSize + CutoutSize)
                {
                    Map[i] = Background;
                    continue;
                }
                if (x >= HemiCubeSize + CutoutSize && y < CutoutSize)
                {
                    Map[i] = Background;
                    continue;
                }
                if (x >= HemiCubeSize + CutoutSize && y >= HemiCubeSize + CutoutSize)
                {
                    Map[i] = Background;
                    continue;
                }

                #endregion

                // Compute distances to center and borders
                Vector2 ToCenter = Center - xy;
                Vector2 ToBorderL = BorderL - xy;
                Vector2 ToBorderR = BorderR - xy;
                Vector2 ToBorderU = BorderU - xy;
                Vector2 ToBorderD = BorderD - xy;

                // Compute (pseudo-)angles of every position
                float AngleC = ToCenter.Length() / HemiCubeSize * MathHelper.PiOver2;
                Angles.Add(AngleC);
                Angles.Add(ToBorderL.Length() / HemiCubeSize * MathHelper.PiOver2);
                Angles.Add(ToBorderR.Length() / HemiCubeSize * MathHelper.PiOver2);
                Angles.Add(ToBorderU.Length() / HemiCubeSize * MathHelper.PiOver2);
                Angles.Add(ToBorderD.Length() / HemiCubeSize * MathHelper.PiOver2);

                // Set Lambertian value
                float Lambert = AngleC >= MathHelper.PiOver2 ? 0 : (float)Math.Cos(AngleC);

                // Select smallest angle
                float Angle = Angles.Min();

                // Set Shape value
                float Shape = Angle >= MathHelper.PiOver2 ? 0 : (float)Math.Cos(Angle);

                // Save product in map
                float Product = Lambert * Shape;
                Map[i] = Product;
                MapUnity[i] = 1.0f;
                Sum += Product;
            }

            // Create final texture (not normalized)
            MultiplierMap = new Texture2D(gfx, MapSize, MapSize, false, SurfaceFormat.Single);
            MultiplierMap.SetData(Map);
            UnityMap = new Texture2D(gfx, MapSize, MapSize, false, SurfaceFormat.Single);
            UnityMap.SetData(MapUnity);

            // Normalize map
            for (int i = 0; i < PixelsPerMap; i++)
            {
                Map[i] /= Sum;
                //MapUnity[i] /= (3 * HemiCubeSize * HemiCubeSize);
            }

            // Create final texture (normalized)
            MultiplierMapN = new Texture2D(gfx, MapSize, MapSize, false, SurfaceFormat.Single);
            MultiplierMapN.SetData(Map);
            //UnityMapN = new Texture2D(gfx, MapSize, MapSize, false, SurfaceFormat.Single);
            //UnityMapN.SetData(MapUnity);

        }

        #endregion

        #region face list

        public List<AAFace> GetFaceList(AAFace face)
        {
            List<AAFace> Faces = new List<AAFace>();

            switch (face)
            {
                case AAFace.XPlus:
                    Faces = new List<AAFace>() { AAFace.XPlus, AAFace.ZMinus, AAFace.ZPlus, AAFace.YPlus, AAFace.YMinus };
                    break;
                case AAFace.XMinus:
                    Faces = new List<AAFace>() { AAFace.XMinus, AAFace.ZPlus, AAFace.ZMinus, AAFace.YPlus, AAFace.YMinus };
                    break;
                case AAFace.ZPlus:
                    Faces = new List<AAFace>() { AAFace.ZPlus, AAFace.XPlus, AAFace.XMinus, AAFace.YPlus, AAFace.YMinus };
                    break;
                case AAFace.ZMinus:
                    Faces = new List<AAFace>() { AAFace.ZMinus, AAFace.XMinus, AAFace.XPlus, AAFace.YPlus, AAFace.YMinus };
                    break;
                case AAFace.YPlus:
                    Faces = new List<AAFace>() { AAFace.YPlus, AAFace.XPlus, AAFace.XMinus, AAFace.ZMinus, AAFace.ZPlus };
                    break;
                case AAFace.YMinus:
                    Faces = new List<AAFace>() { AAFace.YMinus, AAFace.XMinus, AAFace.XPlus, AAFace.ZMinus, AAFace.ZPlus };
                    break;
            }
            return Faces;
        }

        #endregion

        #region access

        public Texture2D TFront
        {
            get
            {
                return RTIndividual[0];
            }
        }

        public Texture2D TLeft
        {
            get
            {
                return RTIndividual[1];
            }
        }

        public Texture2D TRight
        {
            get
            {
                return RTIndividual[2];
            }
        }

        public Texture2D TUp
        {
            get
            {
                return RTIndividual[3];
            }
        }

        public Texture2D TDown
        {
            get
            {
                return RTIndividual[4];
            }
        }

        #endregion
    }
}
