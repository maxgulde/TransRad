/* Radiate Energy Transport Tests Project
 * 
 * Application control
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
using Microsoft.Xna.Framework.Input;

#endregion

namespace TransRad
{
    public class Main : Game
    {

        #region fields

        GraphicsDeviceManager GFX;
        Effect Effect;
        Viewport VP_Free, VP_Obj;
        Camera C_Free, C_Obj;

        Model Model;

        Point MouseOldPosition;

        int SourceIdx = 0;
        int TargetIdx = 1;
        int TargetIdxOld = -1;
        int NumberOfMeshes;

        #endregion

        #region init

        public Main()
        {
            GFX = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            GFX.PreferredBackBufferWidth = Settings.D_ScreenWidth;
            GFX.PreferredBackBufferHeight = Settings.D_ScreenHeight;
            GFX.ApplyChanges();

            VP_Free = new Viewport(0, 0, Settings.D_ViewportSize, Settings.D_ScreenHeight);
            VP_Obj = new Viewport(Settings.D_ViewportSize, 0, Settings.D_ViewportSize, Settings.D_ScreenHeight);

            C_Free = new Camera(1);
            C_Obj = new Camera(1);

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            Input.InitInput();
            MouseOldPosition = Input.MousePosition;

            base.Initialize();
        }

        #endregion

        #region loading

        protected override void LoadContent()
        {
            Model = Content.Load<Model>("TestObj1");
            Effect = Content.Load<Effect>("Default");

            NumberOfMeshes = Model.Meshes.Count;
            Console.WriteLine("Loading model with " + NumberOfMeshes + " meshes.");

            foreach (ModelMesh m in Model.Meshes)
            {
                Console.WriteLine("Mesh: " + m.Name);
                foreach (ModelMeshPart p in m.MeshParts)
                {
                    p.Effect = Effect;
                }
            }
        }

        #endregion

        #region update

        protected override void Update(GameTime gameTime)
        {
            if (Input.Exit)
            {
                Exit();
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
            if (TargetIdx != TargetIdxOld)
            {
                Vector3 CameraPosition = Tools.GetMeshCenter(Model.Meshes[SourceIdx]);
                Vector3 CameraTarget = Tools.GetMeshCenter(Model.Meshes[TargetIdx]);
                float ViewportSize = Tools.GetMeshDiameter(Model.Meshes[TargetIdx]);
                C_Obj.SetPositionTarget(CameraPosition, CameraTarget, ViewportSize);
                Console.WriteLine("New target: " + TargetIdx);
                Console.WriteLine("\tNew viewport size: " + ViewportSize);
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
            DrawModel(C_Free);

            // Draw obj view
            GraphicsDevice.Viewport = VP_Obj;
            DrawModel(C_Obj, SourceIdx);

            base.Draw(gameTime);
        }

        void DrawModel(Camera cam, int sourceIdx = -1)
        {
            int i = 0;
            foreach (ModelMesh mesh in Model.Meshes)
            {
                if (i != sourceIdx)
                {
                    foreach (Effect eff in mesh.Effects)
                    {
                        eff.CurrentTechnique = eff.Techniques["BasicColorDrawing"];
                        eff.Parameters["WorldViewProjection"].SetValue(cam.World * cam.View * cam.Projection);
                        eff.Parameters["MeshNumber"].SetValue(i);
                        eff.Parameters["SourceIdx"].SetValue(SourceIdx);
                        eff.Parameters["TargetIdx"].SetValue(TargetIdx);
                    }
                    mesh.Draw();
                }
                i++;
            }
        }

        #endregion
    }
}
