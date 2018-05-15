/* Radiate Energy Transport Tests Project
 * 
 * Application control
 * 
 * Author: Max Gulde
 * Last Update: 2018-05-15
 * 
 */

#region using

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

#endregion

namespace TransRad
{
    public class Main : Game
    {

        #region fields

        GraphicsDeviceManager GFX;
        Effect Effect;
        Viewport VP_Free, VP_Obj, VP_Complete;
        Camera C_Free, C_Obj;
        SpriteFont Font;
        SpriteBatch SBatch;

        Model Model;

        Point MouseOldPosition;

        int SourceIdx = 0;
        int TargetIdx = 1;
        int TargetIdxOld = -1;
        int NumberOfMeshes;
        float ViewportSize = -1;
        bool MeasureArea = false;

        Stopwatch SW_FPS;
        int t_FPS;
        int t_MaxFPS;

        #endregion

        #region init

        public Main()
        {
            GFX = new GraphicsDeviceManager(this);
            GFX.GraphicsProfile = GraphicsProfile.HiDef;

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            GFX.PreferredBackBufferWidth = Settings.D_ScreenWidth;
            GFX.PreferredBackBufferHeight = Settings.D_ScreenHeight;
            GFX.SynchronizeWithVerticalRetrace = false;
            GFX.ApplyChanges();

            VP_Complete = new Viewport(0, 0, Settings.D_ScreenWidth, Settings.D_ScreenHeight);
            VP_Free = new Viewport(0, 0, Settings.D_ViewportSize, Settings.D_ScreenHeight);
            VP_Obj = new Viewport(Settings.D_ViewportSize, 0, Settings.D_ViewportSize, Settings.D_ScreenHeight);

            C_Free = new Camera(1);
            C_Obj = new Camera(1);

            SBatch = new SpriteBatch(GraphicsDevice);

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            Input.InitInput();
            MouseOldPosition = Input.MousePosition;

            SW_FPS = new Stopwatch();

            base.Initialize();
        }

        #endregion

        #region loading

        protected override void LoadContent()
        {
            Model = Content.Load<Model>("TestObj1");
            Effect = Content.Load<Effect>("Default");
            Font = Content.Load<SpriteFont>("Info");

            NumberOfMeshes = Model.Meshes.Count;
            Console.WriteLine("Loading model with " + NumberOfMeshes + " meshes.");

            int i = 0;
            foreach (ModelMesh m in Model.Meshes)
            {
                Console.WriteLine("Mesh " + i + " <" + m.Name + ">");
                if (string.Compare(m.Name, "Source", true) == 0)
                {
                    SourceIdx = i;
                }
                foreach (ModelMeshPart p in m.MeshParts)
                {
                    p.Effect = Effect;
                }
                i++;
            }

            SW_FPS.Start();
            t_FPS = 0;
            t_MaxFPS = 0;
        }

        #endregion

        #region update

        protected override void Update(GameTime gameTime)
        {
            if (Input.Exit)
            {
                Exit();
            }

            // Performance
            t_FPS++;
            if (SW_FPS.ElapsedMilliseconds > 1000)
            {
                t_MaxFPS = t_FPS;
                t_FPS = 0;
                SW_FPS.Restart();
            }

            // Camera control
            if (Input.ClickL && MouseOldPosition != Input.MousePosition)
            {
                Point dRot = MouseOldPosition - Input.MousePosition;
                if (VP_Free.Bounds.Contains(Input.MousePosition))
                {
                    C_Free.Rotate(-dRot.X * Settings.C_RotSpeedAz, -dRot.Y * Settings.C_RotSpeedEl);
                }
            }

            // Fix view for area evaluation
            MeasureArea = TargetIdx != TargetIdxOld;
            if (MeasureArea)
            {
                Vector3 CameraPosition = Tools.GetMeshCenter(Model.Meshes[SourceIdx]);
                Vector3 CameraTarget = Tools.GetMeshCenter(Model.Meshes[TargetIdx]);
                ViewportSize = Tools.GetMeshDiameter(Model.Meshes[TargetIdx]);
                C_Obj.SetPositionTarget(CameraPosition, CameraTarget, ViewportSize);
                //Console.WriteLine("New target: " + TargetIdx);
                //Console.WriteLine("\tNew viewport size: " + ViewportSize);
                TargetIdxOld = TargetIdx;
            }

            // Next target
            if (Input.NextTarget)
            {
                TargetIdx++;
                TargetIdx = TargetIdx >= NumberOfMeshes ? 0 : TargetIdx;
                TargetIdx += TargetIdx == SourceIdx ? 1 : 0;
            }

            MouseOldPosition = Input.MousePosition;
            base.Update(gameTime);
        }

        #endregion

        #region draw

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            // Draw top view
            GraphicsDevice.Viewport = VP_Free;
            DrawCompleteModel(C_Free);

            // Draw obj view
            GraphicsDevice.Viewport = VP_Obj;
            // Whole scene, without source
            DrawCompleteModel(C_Obj, SourceIdx);
            // Only target
            if (MeasureArea)
            {
                DrawMeshWithOcclusion(Model.Meshes[TargetIdx], C_Obj, TargetIdx);
            }

            // Draw FPS counter
            GraphicsDevice.Viewport = VP_Complete;
            SBatch.Begin();
            string Text = t_MaxFPS + " fps";
            Vector2 Position = Settings.D_FontPadding * Vector2.One;
            SBatch.DrawString(Font, Text, Position, Color.Black);
            SBatch.End();

            base.Draw(gameTime);
        }


        // Draw the complete model / scene
        void DrawCompleteModel(Camera cam, int excludePartIdx = -1)
        {
            int Idx = 0;
            foreach (ModelMesh mesh in Model.Meshes)
            {
                if (Idx != excludePartIdx)
                {
                    DrawMesh(mesh, cam, Idx);
                }
                Idx++;
            }
        }

        // Draw a single model part
        void DrawMeshWithOcclusion(ModelMesh mesh, Camera cam, int partIdx)
        {
            OcclusionQuery occQuery = new OcclusionQuery(GraphicsDevice);
            occQuery.Begin();
            DrawMesh(mesh, cam, partIdx);
            occQuery.End();

            while (!occQuery.IsComplete)
            {
                // Do nothing until query is complete.
            }

            float ViewportArea = ViewportSize * ViewportSize;
            Console.WriteLine("Seeing " + (occQuery.PixelCount / (float)Settings.D_PixelPerViewport * ViewportArea * 10000).ToString("F1") + " cm^2 of target " + partIdx + ".");
            occQuery.Dispose();
        }

        void DrawMesh(ModelMesh mesh, Camera cam, int partIdx)
        {
            foreach (Effect eff in mesh.Effects)
            {
                eff.CurrentTechnique = eff.Techniques["BasicColorDrawing"];
                eff.Parameters["WorldViewProjection"].SetValue(cam.World * cam.View * cam.Projection);
                eff.Parameters["MeshNumber"].SetValue(partIdx);
                eff.Parameters["SourceIdx"].SetValue(SourceIdx);
                eff.Parameters["TargetIdx"].SetValue(TargetIdx);
            }
            mesh.Draw();
        }

        #endregion
    }
}
