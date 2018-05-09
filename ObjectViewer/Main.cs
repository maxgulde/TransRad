/* Object Viewer
 * Author: Max Gulde
 * Last Update: 2018-05-09
 */

#region using

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace ObjectViewer
{
    public class Main : Game
    {
        #region fields

        GraphicsDeviceManager _GraphicsDeviceManager;
        Model _Model;
        Texture2D _Texture;
        Rectangle _WindowArea;

        Point MouseOldPosition;
        Camera _Cam;

        #endregion

        #region init

        public Main()
        {
            _GraphicsDeviceManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            _GraphicsDeviceManager.PreferredBackBufferWidth = Camera.ScreenSize;
            _GraphicsDeviceManager.PreferredBackBufferHeight = Camera.ScreenSize;
            _GraphicsDeviceManager.ApplyChanges();
            _WindowArea = new Rectangle(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            _Cam = new Camera();
            Input.Initialize();
            base.Initialize();
        }

        #endregion

        #region load

        protected override void LoadContent()
        {
            _Model = Content.Load<Model>("model/Hemisphere");
            _Texture = Content.Load<Texture2D>("texture/texture1");
        }

        #endregion

        #region update

        protected override void Update(GameTime gameTime)
        {
            if (Input.Exit)
            {
                Exit();
            }

            if (Input.SwitchView)
            {
                _Cam.SwitchViewMode();
            }

            #region camera mouse control

            // camera control
            if (Input.ClickL && MouseOldPosition != Input.MousePosition)
            {
                Point dRot = MouseOldPosition - Input.MousePosition;
                MouseOldPosition = Input.MousePosition;
                _Cam.Rotate(-dRot.X, -dRot.Y);
            }

            #endregion

            base.Update(gameTime);
        }

        #endregion

        #region draw

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            RasterizerState rs = new RasterizerState()
            {
                CullMode = CullMode.None
            };
            GraphicsDevice.RasterizerState = rs;

            DrawModel(_Model);

            base.Draw(gameTime);
        }

        private void DrawModel(Model model)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.View = _Cam.View;
                    effect.Projection = _Cam.Projection;
                    effect.World = _Cam.World;
                    effect.TextureEnabled = true;
                    effect.LightingEnabled = true;
                    effect.Texture = _Texture;
                    effect.DiffuseColor = Color.White.ToVector3();
                    effect.AmbientLightColor = Color.White.ToVector3();

                    mesh.Draw();
                }
            }
        }

        #endregion
    }
}
