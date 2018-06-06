/* Radiate Energy Transport Tests Project
 * 
 * Application control
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-05
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
        Camera CamFree, CamHemiCube;
        SpriteFont Font;
        SpriteBatch SBatch;
        Effect SEffect;
        Point MouseOldPosition;
        RenderTarget2D RTFree, RTHemiCube, RTViewFactor;

        Model Pointer;
        SatModel Model;
        HemiCube HemiCube;

        Stopwatch StopWatchFPS;
        Stopwatch SW;
        int TimerFPS;
        int TimerMaxFPS;

        List<AAFace> FaceList;
        int SourceIdx;
        int TargetIdx;
        int TargetFaceIdx;
        bool IndicesInitalized = false;
        bool ComputationEnded = true;

        List<ViewFactor> Results;

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

            // Create cameras
            CamFree = new Camera(Settings.D_DefaultImageSize);
            CamHemiCube = new Camera(MathHelper.PiOver2, true);

            // Create Hemicube data structure
            HemiCube = new HemiCube(GraphicsDevice);    

            // Create the rendertargets
            RTFree = new RenderTarget2D(GraphicsDevice, Settings.D_ViewportSize, Settings.D_ViewportSize, false, SurfaceFormat.Color, DepthFormat.Depth16);
            RTHemiCube = new RenderTarget2D(GraphicsDevice, Settings.D_HemiCubeResolution * 2, Settings.D_HemiCubeResolution * 2, false, SurfaceFormat.Color, DepthFormat.Depth16);
            RTViewFactor = new RenderTarget2D(GraphicsDevice, Settings.D_HemiCubeResolution * 2, Settings.D_HemiCubeResolution * 2, true, SurfaceFormat.Single, DepthFormat.None);

            // Create sprite batch for font display
            SBatch = new SpriteBatch(GraphicsDevice);

            // Hide mouse, disable update sync
            IsMouseVisible = true;
            IsFixedTimeStep = false;

            // Init input
            Input.InitInput();
            MouseOldPosition = Input.MousePosition;

            // Results
            InitIndices();
            Results = new List<ViewFactor>();

            // Start stopwatch
            StopWatchFPS = new Stopwatch();
            SW = new Stopwatch();

            base.Initialize();
        }

        private void InitIndices()
        {
            SourceIdx = 0;
            TargetIdx = 1;
            TargetFaceIdx = 0;
        }

        #endregion

        #region loading

        protected override void LoadContent()
        {
            // Loading assets
            Effect Effect = Content.Load<Effect>("Default");
            SEffect = Content.Load<Effect>("Default2D");
            Model = new SatModel(Content.Load<Model>("TestObj2"), Effect, GraphicsDevice);
            Font = Content.Load<SpriteFont>("Info");
            Pointer = Content.Load<Model>("Pointer");
            Pointer = Tools.ApplyCustomEffect(Pointer, Effect);

            // Fill face list
            FaceList = Tools.GetEnumValues<AAFace>();

            // Start performance counters
            StopWatchFPS.Start();
            TimerFPS = 0;
            TimerMaxFPS = 0;
        }

        #endregion

        #region update

        protected override void Update(GameTime gameTime)
        {
            if (Input.Exit)
            {
                Exit();
            }

            #region performance

            TimerFPS++;
            if (StopWatchFPS.ElapsedMilliseconds > 1000)
            {
                TimerMaxFPS = TimerFPS;
                TimerFPS = 0;
                StopWatchFPS.Restart();
            }

            #endregion

            #region camera control

            if (Input.ClickL && MouseOldPosition != Input.MousePosition)
            {
                Point dRot = Input.MousePosition - MouseOldPosition;
                if (RTFree.Bounds.Contains(Input.MousePosition))
                {
                    CamFree.Rotate(dRot.X * Settings.C_RotSpeedAz, dRot.Y * Settings.C_RotSpeedEl);
                }
            }

            #endregion

            #region simulation

            if (Input.StartComputation)
            {
                ComputationEnded = false;
            }
            if (!ComputationEnded)
            {
                Settings.f_ComputeArea = true;
                if (!IndicesInitalized)
                {
                    InitIndices();
                    IndicesInitalized = true;
                    Settings.f_ComputationRunning = true;
                }
                else
                {
                    // Select next face
                    TargetFaceIdx++;
                    // If was last face, next target
                    if (TargetFaceIdx >= Tools.GetEnumValues<AAFace>().Count)
                    {
                        TargetFaceIdx = 0;
                        TargetIdx++;
                    }
                    // If same as source, next target
                    if (TargetIdx == SourceIdx)
                    {
                        TargetIdx++;
                    }
                    // If last target, next source
                    if (TargetIdx >= Model.MeshNumber)
                    {
                        TargetIdx = 0;
                        SourceIdx++;
                    }
                    // If last source, end
                    if (SourceIdx >= Model.MeshNumber)
                    {
                        InitIndices();
                        ComputationEnded = true;
                        Settings.f_ComputationRunning = false;
                        Settings.f_ComputeArea = false;
                    }
                }
                Console.WriteLine("Computing view factor");
                Console.WriteLine("\t Source index = " + SourceIdx);
                Console.WriteLine("\t Target index = " + TargetIdx);
                Console.WriteLine("\t Target face index = " + TargetFaceIdx);
            }

            #endregion

            #region debug

            else
            {
                if (Input.ToggleBBox)
                {
                    Settings.f_DrawBoundingBoxes = !Settings.f_DrawBoundingBoxes;
                }
                if (Input.TogglePointer)
                {
                    Settings.f_DrawPointer = !Settings.f_DrawPointer;
                }
                if (Input.ToggleMultiplierMap)
                {
                    Settings.f_DrawMultiplierMap = !Settings.f_DrawMultiplierMap;
                }
                if (Input.ComputeArea)
                {
                    Settings.f_ComputeArea = true;
                }
                if (Input.NextFace)
                {
                    TargetFaceIdx++;
                    TargetFaceIdx = TargetFaceIdx >= FaceList.Count ? 0 : TargetFaceIdx;
                    Console.WriteLine("Current face is <" + FaceList[TargetFaceIdx].ToString() + ">.");
                }
                if (Input.NextObject)
                {
                    SourceIdx++;
                    SourceIdx = SourceIdx >= Model.MeshNumber ? 0 : SourceIdx;
                    TargetFaceIdx = 0;
                    Console.WriteLine("Current object is <" + Model.Components[SourceIdx].Name + ">.");
                }
            }

            #endregion

            MouseOldPosition = Input.MousePosition;

            base.Update(gameTime);
        }

        #endregion

        #region draw

        protected override void Draw(GameTime gameTime)
        {
            AAFace CurrentFace = FaceList[TargetFaceIdx];

            #region free view

            // Draw top view (free camera)
            GraphicsDevice.SetRenderTarget(RTFree);
            GraphicsDevice.Clear(Color.White);
            Model.DrawCompleteModel(CamFree, Settings.f_DrawBoundingBoxes, Vector3.Zero, -1, true);
            // Draw pointer
            if (Settings.f_DrawPointer)
            {
                Vector3 CamPosition = Model.Components[SourceIdx].GetFaceCenter(CurrentFace);
                DrawModel(CamFree, Pointer, CamPosition);
                foreach (AAFace face in HemiCube.GetFaceList(CurrentFace))
                {
                    DrawModel(CamFree, Pointer, Model.Components[SourceIdx].GetFaceDirection(CamPosition, face));
                }
            }

            #endregion

            #region radiosity

            // Generate HemiCube textures
            DrawHemiCubeToTextures(Model.Components[SourceIdx], CurrentFace, HemiCube.GetFaceList(CurrentFace));
            MergeHemiCubeTextures();

            // Apply transfer map
            GraphicsDevice.SetRenderTarget(RTViewFactor);
            GraphicsDevice.Clear(Color.White);
            ApplyMultiplierMap();

            #endregion

            #region rendertargets

            // Draw rendertargets
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.White);
            SBatch.Begin();

            // Free view
            SBatch.Draw(RTFree, RTFree.Bounds, Color.White);

            // View factor map
            Vector2 Position = new Vector2(RTFree.Bounds.Width, 0);
            SBatch.Draw(RTViewFactor, Position, Color.White);

            // Multiplier texture
            if (Settings.f_DrawMultiplierMap)
            {
                SBatch.Draw(HemiCube.MultiplierMap, Position, Color.White);
            }

            // FPS counter
            string Text = TimerMaxFPS + " fps";
            Position = Settings.D_FontPadding * Vector2.One;
            SBatch.DrawString(Font, Text, Position, Color.Black);

            SBatch.End();

            #endregion

            #region area computation

            if (Settings.f_ComputeArea)
            {
                float Area = GetPixelSum(RTViewFactor);
                Console.WriteLine("\t View factor = " + Area.ToString());
                // Safe results
                float SourceArea = Model.Components[SourceIdx].GetBBoxTotalArea();
                float TargetArea = Model.Components[TargetIdx].GetBBoxFaceArea(FaceList[TargetFaceIdx]);
                Results.Add(new ViewFactor(SourceIdx, TargetIdx, SourceArea, TargetArea, FaceList[TargetFaceIdx]));
                Settings.f_ComputeArea = false;
            }

            #endregion

            base.Draw(gameTime);
        }

        #region secondary methods

        void DrawModel(Camera cam, Model model, Vector3 position)
        {
            // Draw pointer
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect eff in mesh.Effects)
                {
                    Matrix World = Matrix.CreateTranslation(position);
                    Matrix Scale = Matrix.CreateScale(Settings.D_PointerScale);
                    eff.CurrentTechnique = eff.Techniques["BasicColorDrawing"];
                    eff.Parameters["WorldViewProjection"].SetValue(Scale * World * cam.View * cam.Projection);
                    eff.Parameters["ComponentColor"].SetValue(Color.Red.ToVector3());
                    eff.Parameters["Alpha"].SetValue(1.0f);
                }
                mesh.Draw();
            }
        }

        void DrawHemiCubeToTextures(SatComponent source, AAFace CurrentFace, List<AAFace> faces)
        {
            // Set camera position
            Vector3 Position = source.GetFaceCenter(CurrentFace);

            for (int i = 0; i < faces.Count; i++)
            {
                // Orient camera
                Vector3 Target = source.GetFaceDirection(Position, faces[i]);
                CamHemiCube.SetPositionTarget(Position, Target, true);

                // Prepare rendertarget
                GraphicsDevice.SetRenderTarget(HemiCube.RTIndividual[i]);
                GraphicsDevice.Clear(Color.Black);

                // Render model
                if (Settings.f_ComputationRunning)
                {
                    Model.DrawComponent(CamHemiCube, TargetIdx);
                }
                else
                {
                    Model.DrawCompleteModel(CamHemiCube, false, Vector3.One, SourceIdx);
                }
            }
        }

        void MergeHemiCubeTextures()
        {
            // Prepare variables
            int TextureSize = Settings.D_HemiCubeResolution;
            AAFace CurrentFace = FaceList[TargetFaceIdx];
            Vector2 Origin = Vector2.One * TextureSize * 0.5f;
            Vector2 Position;
            Rectangle Source;
            float Rotation;

            // Init drawing (around texture center)
            GraphicsDevice.SetRenderTarget(RTHemiCube);
            GraphicsDevice.Clear(Color.DarkRed);
            SBatch.Begin();

            #region front

            switch (CurrentFace)
            {
                case AAFace.YPlus:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize);
                    Position = Vector2.One * TextureSize;
                    Rotation = MathHelper.PiOver2;
                    break;
                case AAFace.YMinus:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize);
                    Position = Vector2.One * TextureSize;
                    Rotation = MathHelper.PiOver2;
                    break;
                default:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize);
                    Position = Vector2.One * TextureSize;
                    Rotation = 0;
                    break;
            }
            SBatch.Draw(HemiCube.TFront, Position, Source, Color.White, Rotation, Origin, 1.0f, SpriteEffects.None, 0.0f);

            #endregion

            #region left

            switch (CurrentFace)
            {
                case AAFace.YPlus:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize / 2);
                    Position = new Vector2(0, 1.0f) * TextureSize;
                    Rotation = MathHelper.PiOver2;
                    break;
                case AAFace.YMinus:
                    Source = new Rectangle(0, TextureSize / 2, TextureSize, TextureSize / 2);
                    Position = new Vector2(0.5f, 1.0f) * TextureSize;
                    Rotation = -MathHelper.PiOver2;
                    break;
                default:
                    Source = new Rectangle(TextureSize / 2, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2(0.5f, 1.0f) * TextureSize;
                    Rotation = 0;
                    break;
            }
            SBatch.Draw(HemiCube.TLeft, Position, Source, Color.White, Rotation, Origin, 1.0f, SpriteEffects.None, 0.0f);

            #endregion

            #region right

            switch (CurrentFace)
            {
                case AAFace.YPlus:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize / 2);
                    Position = new Vector2(2.0f, 1.0f) * TextureSize;
                    Rotation = -MathHelper.PiOver2;
                    break;
                case AAFace.YMinus:
                    Source = new Rectangle(0, TextureSize / 2, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.5f, 1.0f) * TextureSize;
                    Rotation = MathHelper.PiOver2;
                    break;
                default:
                    Source = new Rectangle(0, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2(2.0f, 1.0f) * TextureSize;
                    Rotation = 0;
                    break;
            }

            SBatch.Draw(HemiCube.TRight, Position, Source, Color.White, Rotation, Origin, 1.0f, SpriteEffects.None, 0.0f);

            #endregion

            #region up

            switch (CurrentFace)
            {
                case AAFace.ZPlus:
                    Source = new Rectangle(TextureSize / 2, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2(1.0f, 0.5f) * TextureSize;
                    Rotation = MathHelper.PiOver2;
                    break;
                case AAFace.ZMinus:
                    Source = new Rectangle(0, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2(1.0f, 0) * TextureSize;
                    Rotation = -MathHelper.PiOver2;
                    break;
                case AAFace.YPlus:
                    Source = new Rectangle(0, TextureSize / 2, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 0) * TextureSize;
                    Rotation = MathHelper.Pi;
                    break;
                case AAFace.YMinus:
                    Source = new Rectangle(0, TextureSize / 2, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 0.5f) * TextureSize;
                    Rotation = 0;
                    break;
                case AAFace.XMinus:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 0) * TextureSize;
                    Rotation = MathHelper.Pi;
                    break;
                default:
                    Source = new Rectangle(0, TextureSize / 2, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 0.5f) * TextureSize;
                    Rotation = 0;
                    break;
            }
            SBatch.Draw(HemiCube.TUp, Position, Source, Color.White, Rotation, Origin, 1.0f, SpriteEffects.None, 0.0f);

            #endregion

            #region down

            switch (CurrentFace)
            {
                case AAFace.ZPlus:
                    Source = new Rectangle(TextureSize / 2, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2( 1.0f, 1.5f) * TextureSize;
                    Rotation = -MathHelper.PiOver2;
                    break;
                case AAFace.ZMinus:
                    Source = new Rectangle(0, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2(1.0f, 2.0f) * TextureSize;
                    Rotation = MathHelper.PiOver2;
                    break;
                case AAFace.YPlus:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 2.0f) * TextureSize;
                    Rotation = 0;
                    break;
                case AAFace.YMinus:
                    Source = new Rectangle(0, TextureSize / 2, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 1.5f) * TextureSize;
                    Rotation = MathHelper.Pi;
                    break;
                case AAFace.XMinus:
                    Source = new Rectangle(0, TextureSize / 2, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 1.5f) * TextureSize;
                    Rotation = MathHelper.Pi;
                    break;
                default:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 2.0f) * TextureSize;
                    Rotation = 0;
                    break;
            }
            SBatch.Draw(HemiCube.TDown, Position, Source, Color.White, Rotation, Origin, 1.0f, SpriteEffects.None, 0.0f);

            #endregion

            SBatch.End();
        }

        void ApplyMultiplierMap()
        {
            Texture2D MMap = Settings.f_ComputeArea ? HemiCube.MultiplierMapN : HemiCube.MultiplierMap;
            SEffect.Parameters["TexMuMap"].SetValue(MMap);
            //SEffect.Parameters["TexMuMap"].SetValue(HemiCube.UnityMap);
            SBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, SEffect);
            SBatch.Draw(RTHemiCube, Vector2.Zero, Color.White);
            SBatch.End();
        }

        #endregion

        #endregion

        #region view factor computation

        float GetPixelSum(Texture2D tex)
        {
            int N = tex.Width * tex.Height;
            float[] PixelValue = new float[N];
            tex.GetData(PixelValue);

            float Sum = 0;
            for (int i = 0; i < N; i++)
            {
                Sum += PixelValue[i];
            }

            return Sum;
        }
        
        #endregion
    }

    public struct ViewFactor
    {
        public int SourceIdx { get; private set; }
        public int TargetIdx { get; private set; }

        public float SourceArea { get; private set; }
        public float TargetFaceArea { get; private set; }

        public AAFace TargetFace { get; private set; }

        public ViewFactor(int sourceIdx, int targetIdx, float sourceArea, float targetArea, AAFace targetFace)
        {
            SourceIdx = sourceIdx;
            TargetIdx = targetIdx;
            SourceArea = sourceArea;
            TargetFaceArea = targetArea;
            TargetFace = targetFace;
        }
    }
}
