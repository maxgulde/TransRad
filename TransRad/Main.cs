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
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        int NumberOfMeshes;
        int IdxSource = -1;
        int IdxTarget = -1;
        float ViewportSize = -1;
        bool MeasureArea = true;
        bool StopMeasurement = false;
        float[,] Areas;
        List<string> ObjNames;

        Stopwatch SW_FPS;
        int t_FPS;
        int t_MaxFPS;

        #endregion

        #region init

        public Main()
        {
            GFX = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef
            };

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            // Set resolution
            GFX.PreferredBackBufferWidth = Settings.D_ScreenWidth;
            GFX.PreferredBackBufferHeight = Settings.D_ScreenHeight;
            GFX.SynchronizeWithVerticalRetrace = false;
            GFX.ApplyChanges();

            // Create viewports
            VP_Complete = new Viewport(0, 0, Settings.D_ScreenWidth, Settings.D_ScreenHeight);
            VP_Free = new Viewport(0, 0, Settings.D_ViewportSize, Settings.D_ScreenHeight);
            VP_Obj = new Viewport(Settings.D_ViewportSize, 0, Settings.D_ViewportSize, Settings.D_ScreenHeight);

            // Create cameras
            C_Free = new Camera(Settings.D_DefaultImageSize);
            C_Obj = new Camera(Settings.D_DefaultImageSize);

            // Create sprite batch for font display
            SBatch = new SpriteBatch(GraphicsDevice);

            // Hide mouse, disable update sync
            IsMouseVisible = true;
            IsFixedTimeStep = false;

            // Init input
            Input.InitInput();
            MouseOldPosition = Input.MousePosition;

            // Start stopwatch
            SW_FPS = new Stopwatch();

            base.Initialize();
        }

        #endregion

        #region loading

        protected override void LoadContent()
        {
            // Loading assets
            Model = Content.Load<Model>("TestObj1");
            Effect = Content.Load<Effect>("Default");
            Font = Content.Load<SpriteFont>("Info");

            // Analysing model
            NumberOfMeshes = Model.Meshes.Count;
            Console.WriteLine("Loading model with " + NumberOfMeshes + " meshes.");
            if (NumberOfMeshes < 2)
            {
                Console.WriteLine("### Error ### Not enough meshed found in model.");
            }
            ObjNames = new List<string>();
            int i = 0;
            foreach (ModelMesh m in Model.Meshes)
            {
                Console.WriteLine("Mesh " + i + " <" + m.Name + ">");
                // Remember model name
                ObjNames.Add(m.Name);
                // Apply custom effect to each mesh
                foreach (ModelMeshPart p in m.MeshParts)
                {
                    p.Effect = Effect;
                }
                i++;
            }

            // Creating area matrix
            Areas = new float[NumberOfMeshes, NumberOfMeshes];
            Console.WriteLine("Created " + NumberOfMeshes + " x " + NumberOfMeshes + " view factor matrix.");

            // Start performance counters
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

            // Camera control for left viewport)
            if (Input.ClickL && MouseOldPosition != Input.MousePosition)
            {
                Point dRot = MouseOldPosition - Input.MousePosition;
                if (VP_Free.Bounds.Contains(Input.MousePosition))
                {
                    C_Free.Rotate(-dRot.X * Settings.C_RotSpeedAz, -dRot.Y * Settings.C_RotSpeedEl);
                }
            }

            // Fix view for area evaluation in right viewport
            if (!StopMeasurement)
            {
                // Advance Indices
                if (IdxSource < 0 && IdxTarget < 0)
                {
                    IdxSource = 0;
                    IdxTarget = 0;
                }
                else
                {
                    IdxTarget++;
                    if (IdxTarget >= NumberOfMeshes)
                    {
                        IdxSource++;
                        IdxTarget = 0;  // Full matrix to check symmetry
                    }
                    if (IdxSource >= NumberOfMeshes)
                    {
                        MeasureArea = false;
                    }
                }

                // Print out matrix
                if (!MeasureArea && !StopMeasurement)
                {
                    Tools.PrintMatrix(Areas);
                    StopMeasurement = true;
                    IdxSource = -1;
                    IdxTarget = -1;
                }

                // Fix camera view
                if (MeasureArea)
                {
                    Vector3 CameraPosition = Tools.GetMeshCenter(Model.Meshes[IdxSource]);
                    Vector3 CameraTarget = Tools.GetMeshCenter(Model.Meshes[IdxTarget]);
                    ViewportSize = Tools.GetMeshDiameter(Model.Meshes[IdxTarget]);
                    C_Obj.SetPositionTarget(CameraPosition, CameraTarget, ViewportSize);
                    Console.WriteLine("Viewing " + ObjNames[IdxSource] + " (" + IdxSource + ") -> " + ObjNames[IdxTarget] + " (" + IdxTarget + ")");
                }
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

            // Draw measurement view
            if (MeasureArea)
            {
                // Draw obj view
                GraphicsDevice.Viewport = VP_Obj;
                // Whole scene, without source
                DrawCompleteModel(C_Obj, !StopMeasurement);
                // Redraw target
                DrawMeshWithOcclusion(Model.Meshes[IdxTarget], C_Obj, IdxTarget);
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
        void DrawCompleteModel(Camera cam, bool excludeSource = false)
        {
            int Idx = 0;
            foreach (ModelMesh mesh in Model.Meshes)
            {
                if (!excludeSource || Idx != IdxSource)
                {
                    DrawMesh(mesh, cam, Idx);
                }
                Idx++;
            }
        }

        // Draw a single model part
        void DrawMeshWithOcclusion(ModelMesh mesh, Camera cam, int partIdx)
        {
            // Init occlusion query
            OcclusionQuery occQuery = new OcclusionQuery(GraphicsDevice);
            occQuery.Begin();

            // Draw mesh
            DrawMesh(mesh, cam, partIdx);

            // End occlusion query
            occQuery.End();

            // Do nothing until query is complete.
            while (!occQuery.IsComplete) { }

            float PixelArea = ViewportSize * ViewportSize / Settings.D_PixelPerViewport;
            float ObjArea = occQuery.PixelCount * PixelArea;
            Areas[IdxSource, IdxTarget] = ObjArea;

            // Dispose occlusion query
            occQuery.Dispose();
        }

        void DrawMesh(ModelMesh mesh, Camera cam, int partIdx)
        {
            foreach (Effect eff in mesh.Effects)
            {
                eff.CurrentTechnique = eff.Techniques["BasicColorDrawing"];
                eff.Parameters["WorldViewProjection"].SetValue(cam.World * cam.View * cam.Projection);
                eff.Parameters["MeshNumber"].SetValue(partIdx);
                eff.Parameters["SourceIdx"].SetValue(IdxSource);
                eff.Parameters["TargetIdx"].SetValue(IdxTarget);
            }
            mesh.Draw();
        }

        #endregion
    }
}
