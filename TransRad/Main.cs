/* Radiate Energy Transport Tests Project
 * 
 * Author: Max Gulde
 * Last Update: 2018-05-14
 * 
 */

#region using

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
        Viewport VP_Top, VP_Obj;
        Camera C_Top, C_Obj;

        Model Model;

        Point MouseOldPosition;

        #endregion

        #region init

        public Main()
        {
            GFX = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            GFX.PreferredBackBufferWidth = Settings.ScreenWidth;
            GFX.PreferredBackBufferHeight = Settings.ScreenHeight;
            GFX.ApplyChanges();

            VP_Top = new Viewport(0, 0, Settings.ViewportSize, Settings.ScreenHeight);
            VP_Obj = new Viewport(Settings.ViewportSize, 0, Settings.ViewportSize, Settings.ScreenHeight);

            C_Top = new Camera(Settings.ViewportSize);
            C_Obj = new Camera(Settings.ViewportSize);

            IsMouseVisible = true;

            base.Initialize();
        }

        #endregion

        #region loading

        protected override void LoadContent()
        {
            Model = Content.Load<Model>("TestObj1");
            Effect = Content.Load<Effect>("Default");
        }

        #endregion

        #region update

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // Camera control
            if (Input.ClickL && MouseOldPosition != Input.MousePosition)
            {
                Point dRot = MouseOldPosition - Input.MousePosition;
                if (VP_Top.Bounds.Contains(Input.MousePosition))
                {
                    C_Top.Rotate(-dRot.X * Settings.RotSpeedAz, -dRot.Y * Settings.RotSpeedEl);
                }
                if (VP_Obj.Bounds.Contains(Input.MousePosition))
                {
                    C_Obj.Rotate(-dRot.X * Settings.RotSpeedAz, -dRot.Y * Settings.RotSpeedEl);
                }
            }
            base.Update(gameTime);
        }

        #endregion

        #region draw

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            // Draw top view
            GraphicsDevice.Viewport = VP_Top;
            Effect.CurrentTechnique = Effect.Techniques["BasicColorDrawing"];
            Effect.Parameters["WorldViewProjection"].SetValue(C_Top.World * C_Top.View * C_Top.Projection);
            DrawModel(Effect, C_Top);

            // Draw object view
            GraphicsDevice.Viewport = VP_Obj;
            Effect.CurrentTechnique = Effect.Techniques["BasicColorDrawing"];
            Effect.Parameters["WorldViewProjection"].SetValue(C_Obj.World * C_Obj.View * C_Obj.Projection);
            DrawModel(Effect, C_Obj);


            base.Draw(gameTime);
        }

        void DrawModel(Effect eff, Camera cam)
        {

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (EffectPass pass in eff.CurrentTechnique.Passes)
                {
                    pass.Apply();
                }

                mesh.Draw();
            }
        }

        #endregion
    }
}
