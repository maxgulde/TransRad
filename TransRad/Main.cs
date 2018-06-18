/* Radiate Energy Transport Tests Project
 * 
 * Application control
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-18
 * 
 * Optimizations:
 * - Paraboloid-approach for comparison: http://cdn.imgtec.com/sdk-documentation/Dual+Paraboloid+Environment+Mapping.Whitepaper.pdf
 * - Optimization of transfer map for better matching with analytical results.
 * 
 */

#region using

using System;
using System.IO;
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

        // Current simulation
        List<AAFace> FaceList;
        int RadSourceIdx;
        int RadSourceFaceIdx;
        int RadTargetIdx;
        int RadTargetFaceIdx;
        bool f_IndicesInitalized = false;
        bool f_ComputationEnded = true;
        bool f_ComputeVFMatrix = false;

        // Performance
        Stopwatch StopWatchFPS;
        Stopwatch SW;
        int TimerFPS;
        int TimerMaxFPS;
        int MaxCalc;
        int CurrentCalc;

        // Verification
        bool f_StartVerification = false;
        float v_DistStep = 0.01f;
        int v_MaxStepNum = 490;
        int v_StepNum = 0;
        Dictionary<float, float> v_Results;

        // Results
        List<ViewFactorPerFace> VFPerFace, VFReciprocal;
        Dictionary<ViewFactorCoordinate, float> VFactors;

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
            VFPerFace = new List<ViewFactorPerFace>();
            VFReciprocal = new List<ViewFactorPerFace>();
            VFactors = new Dictionary<ViewFactorCoordinate, float>();

            // Start stopwatch
            StopWatchFPS = new Stopwatch();
            SW = new Stopwatch();

            base.Initialize();
        }

        private void InitIndices()
        {
            RadSourceIdx = 0;
            RadSourceFaceIdx = 0;
            RadTargetIdx = 1;
            RadTargetFaceIdx = 0;
            Settings.f_ComputeArea = true;
        }

        #endregion

        #region loading

        protected override void LoadContent()
        {
            // Loading assets
            Effect Effect = Content.Load<Effect>("Default");
            SEffect = Content.Load<Effect>("Default2D");
            Model = new SatModel(Content.Load<Model>("TwoDiscs"), Effect, GraphicsDevice);
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
            if (Settings.f_FindHCImageOrientation == true)
            {
                Settings.f_DrawCompleteModel = true;
                Settings.f_UseUniformMMap = true;
            }

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
                f_ComputationEnded = false;
                // Compute necessary number of calculations
                MaxCalc = (int)Math.Pow(6, Model.Components.Count);
                CurrentCalc = 0;
                Console.Write("Calculating ");
                SW.Restart();
            }
            if (!f_ComputationEnded)
            {
                Settings.f_ComputeArea = true;
                Settings.f_DrawCompleteModel = false;
                Settings.f_UseUniformMMap = false;
                CurrentCalc++;
                if (!f_IndicesInitalized)
                {
                    InitIndices();
                    VFactors = new Dictionary<ViewFactorCoordinate, float>();
                    VFPerFace = new List<ViewFactorPerFace>();
                    VFReciprocal = new List<ViewFactorPerFace>();
                    f_IndicesInitalized = true;
                    Settings.f_ComputationRunning = true;
                }
                else
                {
                    // Select next face on source
                    RadSourceFaceIdx++;
                    // If last source face, next face on target
                    if (RadSourceFaceIdx >= Tools.GetEnumValues<AAFace>().Count)
                    {
                        RadSourceFaceIdx = 0;
                        RadTargetFaceIdx++;
                    }
                    // If last target face, next source
                    if (RadTargetFaceIdx >= Tools.GetEnumValues<AAFace>().Count)
                    {
                        RadTargetFaceIdx = 0;
                        RadSourceIdx++;
                    }
                    // If source same as target, next source
                    if (RadSourceIdx == RadTargetIdx)
                    {
                        RadSourceIdx++;
                    }
                    // If last source, next target
                    if (RadSourceIdx >= Model.MeshNumber)
                    {
                        RadSourceIdx = 0;
                        RadTargetIdx++;
                    }
                    // If last target, end
                    if (RadTargetIdx >= Model.MeshNumber)
                    {
                        InitIndices();
                        f_ComputationEnded = true;
                        Settings.f_ComputationRunning = false;
                        Settings.f_ComputeArea = false;
                        f_ComputeVFMatrix = true;
                        Console.Write(" done (" + (SW.ElapsedMilliseconds / 1000.0f).ToString("F1", Settings.Format) + " s).");
                        SW.Stop();
                    }
                }
            }

            #endregion

            #region create view factor matrix

            if (f_ComputeVFMatrix)
            {
                Console.Write("Creating view factor matrix ...");
                CreateVFMatrix();
                Console.WriteLine(" done.");

                Console.Write("Writing view factor matrix to file ...");
                WriteMatrixToFile();
                Console.WriteLine(" done.");
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
                if (Input.ToggleCompleteModel)
                {
                    Settings.f_DrawCompleteModel = !Settings.f_DrawCompleteModel;
                }
                if (Input.ToggleUniformMap)
                {
                    Settings.f_UseUniformMMap = !Settings.f_UseUniformMMap;
                }
                if (Input.ComputeArea)
                {
                    Settings.f_ComputeArea = true;
                }
                if (Input.NextTargetFace)
                {
                    RadTargetFaceIdx++;
                    RadTargetFaceIdx = RadTargetFaceIdx >= FaceList.Count ? 0 : RadTargetFaceIdx;
                    Settings.f_ComputeArea = true;
                }
                if (Input.NextSourceFace)
                {
                    RadSourceFaceIdx++;
                    RadSourceFaceIdx = RadSourceFaceIdx >= FaceList.Count ? 0 : RadSourceFaceIdx;
                    Settings.f_ComputeArea = true;
                }
                if (Input.NextTarget)
                {
                    RadTargetIdx++;
                    RadTargetIdx = RadTargetIdx >= Model.MeshNumber ? 0 : RadTargetIdx;
                    RadTargetFaceIdx = 0;
                    Settings.f_ComputeArea = true;
                }
                if (Input.NextSource)
                {
                    RadSourceIdx++;
                    RadSourceIdx = RadSourceIdx >= Model.MeshNumber ? 0 : RadSourceIdx;
                    Settings.f_ComputeArea = true;
                }
                if (Input.SwapSourceAndTarget)
                {
                    int temp = RadSourceIdx;
                    RadSourceIdx = RadTargetIdx;
                    RadTargetIdx = temp;

                    temp = RadSourceFaceIdx;
                    RadSourceFaceIdx = RadTargetFaceIdx;
                    RadTargetFaceIdx = temp;

                    Settings.f_ComputeArea = true;
                }
                if (Input.StartVerificationRun)
                {
                    f_StartVerification = true;
                    v_Results = new Dictionary<float, float>();
                    v_StepNum = 0;
                }
                if (f_StartVerification)
                {
                    if (v_StepNum < v_MaxStepNum)
                    {
                        Settings.f_ComputeArea = true;
                        v_StepNum++;
                    }
                    else
                    {
                        f_StartVerification = false;
                        // Save to file
                        try
                        {
                            using (StreamWriter sw = new StreamWriter("vf_results.txt"))
                            {
                                sw.WriteLine("%Distance \tView factor");
                                foreach (KeyValuePair<float, float> pair in v_Results)
                                {
                                    sw.WriteLine(pair.Key.ToString("F5", Settings.Format) + "\t" + pair.Value.ToString("F5", Settings.Format));
                                }
                                sw.Close();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Could not write verification results file.");
                        }
                    }
                }
                //if (Settings.f_ComputeArea)
                //{
                //    Settings.f_DrawCompleteModel = false;
                //    Settings.f_UseUniformMMap = false;
                //    //Console.WriteLine("Current RadTarget is <" + Model.Components[RadTargetIdx].Name + ">.");
                //    //Console.WriteLine("Current RadSource is <" + Model.Components[RadSourceIdx].Name + ">.");
                //    //Console.WriteLine("Current TargetFace is <" + FaceList[RadTargetFaceIdx].ToString() + ">.");
                //}
            }

            #endregion

            MouseOldPosition = Input.MousePosition;

            base.Update(gameTime);
        }

        #endregion

        #region draw

        protected override void Draw(GameTime gameTime)
        {
            AAFace SourceFace = FaceList[RadSourceFaceIdx];
            AAFace TargetFace = FaceList[RadTargetFaceIdx];

            #region free view

            if (f_ComputationEnded)
            {
                GraphicsDevice.SetRenderTarget(RTFree);
                GraphicsDevice.Clear(Color.White);
                Model.DrawCompleteModel(CamFree, false, Color.Black, -1, true);
                Model.Components[RadSourceIdx].DrawBoundingBoxFace(CamFree, SourceFace, Color.Orange);
                Model.Components[RadTargetIdx].DrawBoundingBoxFace(CamFree, TargetFace, Color.Green);
                // Draw pointer
                if (Settings.f_DrawPointer)
                {
                    Vector3 CamPosition = Model.Components[RadSourceIdx].GetFaceCenter(SourceFace);
                    DrawModel(CamFree, Pointer, CamPosition);
                    foreach (AAFace face in HemiCube.GetFaceList(SourceFace))
                    {
                        DrawModel(CamFree, Pointer, Model.Components[RadSourceIdx].GetFaceDirection(CamPosition, face));
                    }
                }
            }

            #endregion

            #region verification

            if (f_StartVerification)
            {
                CamHemiCube.TranslateZ(v_StepNum * v_DistStep);
            }

            #endregion

            #region hemicube view

            // Generate HemiCube textures
            DrawHemiCubeTextures(SourceFace, Model.Components[RadSourceIdx], TargetFace);
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
            if (f_ComputationEnded)
            {
                SBatch.Draw(RTFree, RTFree.Bounds, Color.White);
            }

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
                // Components
                SatComponent Source = Model.Components[RadSourceIdx];
                SatComponent Target = Model.Components[RadTargetIdx];
                // Areas
                float SourceArea = Source.GetBBoxTotalArea();
                float SourceFaceArea = Source.GetBBoxFaceArea(SourceFace);
                float TargetArea = Target.GetBBoxTotalArea();
                float TargetFaceArea = Target.GetBBoxFaceArea(TargetFace);
                float AreaRatioSource = SourceFaceArea / SourceArea;
                float AreaRatioTarget = TargetFaceArea / TargetArea;
                // View factor F_s->t
                float Factor = GetPixelSum(RTViewFactor);
                float FactorR = Factor * SourceFaceArea / TargetFaceArea;
                // Area-weighted view factors
                float WeightedFactor = Factor * AreaRatioSource * AreaRatioTarget;
                float WeightedFactorR = FactorR * AreaRatioSource * AreaRatioTarget;
                // Save results
                string SourceName = Source.Name;
                string TargetName = Target.Name;
                VFPerFace.Add(new ViewFactorPerFace(SourceName, TargetName, SourceFace, TargetFace, WeightedFactor));
                VFReciprocal.Add(new ViewFactorPerFace(TargetName, SourceName, TargetFace, SourceFace, FactorR));
                // Text output
                if (f_ComputationEnded || Settings.f_Verbose)
                {
                    Console.Write("From (" + SourceName + "," + SourceFace + ")");
                    Console.WriteLine(" to (" + TargetName + "," + TargetFace + ")");
                    Console.Write("\t Weighted view factor = " + WeightedFactor.ToString("F6", Settings.Format));
                    Console.WriteLine(" (" + Factor.ToString("F3", Settings.Format) + ")");
                    Console.Write("\t Reciprocal view factor = " + WeightedFactorR.ToString("F6", Settings.Format));
                    Console.WriteLine(" (" + FactorR.ToString("F3", Settings.Format) + ")");
                    Console.WriteLine("\t Source area ratio = " + AreaRatioSource.ToString("F4", Settings.Format));
                    Console.WriteLine("\t Target area ratio = " + AreaRatioTarget.ToString("F4", Settings.Format));
                }
                Settings.f_ComputeArea = false;
                // Save values for verification
                if (f_StartVerification)
                {
                    v_Results.Add(v_StepNum * v_DistStep, Factor);
                }
                // Progress
                if (!f_ComputationEnded)
                {
                    if ((CurrentCalc % (int)Math.Ceiling(MaxCalc / 10.0f)) == 0)
                    {
                        Console.Write('.');
                    }
                }
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

        void DrawHemiCubeTextures(AAFace radSourceFace, SatComponent radSource, AAFace radTargetFace)
        {
            // Set camera position on radition source face
            Vector3 Position = radSource.GetFaceCenter(radSourceFace);
            // Get List of camera orientations
            List<AAFace> CameraDirections = HemiCube.GetFaceList(radSourceFace);

            for (int i = 0; i < CameraDirections.Count; i++)
            {
                // Orient camera
                Vector3 CameraPointingTarget = radSource.GetFaceDirection(Position, CameraDirections[i]);
                CamHemiCube.SetPositionTarget(Position, CameraPointingTarget, true);

                // Prepare rendertarget
                GraphicsDevice.SetRenderTarget(HemiCube.RTIndividual[i]);
                GraphicsDevice.Clear(Color.Black);

                // Render complete model without source
                if (Settings.f_DrawCompleteModel)
                {
                    Model.DrawCompleteModel(CamHemiCube, false, Color.White);
                }
                // Render only currently active radiation source (single face)
                else
                {
                    // Draw black bounding boxes for complete model
                    Model.DrawCompleteModel(CamHemiCube, true, Color.Black);
                    // Draw single bounding box face
                    Model.Components[RadTargetIdx].DrawBoundingBoxFace(CamHemiCube, radTargetFace, Color.White);
                }
            }
        }

        void MergeHemiCubeTextures()
        {
            // Prepare variables
            int TextureSize = Settings.D_HemiCubeResolution;
            AAFace CurrentSourceFace = FaceList[RadSourceFaceIdx];
            Vector2 Origin = Vector2.One * TextureSize * 0.5f;
            Vector2 Position;
            Rectangle Source;
            float Rotation;

            // Init drawing (around texture center)
            GraphicsDevice.SetRenderTarget(RTHemiCube);
            GraphicsDevice.Clear(Color.DarkRed);
            SBatch.Begin();

            #region front

            switch (CurrentSourceFace)
            {
                case AAFace.YPlus:
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

            switch (CurrentSourceFace)
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

            switch (CurrentSourceFace)
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

            switch (CurrentSourceFace)
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
                    Source = new Rectangle(0, 0, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 0) * TextureSize;
                    Rotation = MathHelper.Pi;
                    break;
                case AAFace.XMinus:
                    Source = new Rectangle(0, 0, TextureSize, TextureSize / 2);
                    Position = new Vector2(1.0f, 0.0f) * TextureSize;
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

            switch (CurrentSourceFace)
            {
                case AAFace.ZPlus:
                    Source = new Rectangle(TextureSize / 2, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2(1.0f, 1.5f) * TextureSize;
                    Rotation = -MathHelper.PiOver2;
                    break;
                case AAFace.ZMinus:
                    Source = new Rectangle(0, 0, TextureSize / 2, TextureSize);
                    Position = new Vector2(1.0f, 2.0f) * TextureSize;
                    Rotation = MathHelper.PiOver2;
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
            if (Settings.f_UseUniformMMap)
            {
                MMap = HemiCube.UnityMap;
            }
            SEffect.Parameters["TexMuMap"].SetValue(MMap);
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

        void CreateVFMatrix()
        {
            // Compute component-to-component view factors for regular and reciprocal data.
            //Dictionary<ViewFactorCoordinate, float> Regular = ComputeVFMatrix(VFPerFace);
            //Dictionary<ViewFactorCoordinate, float> Reciprocal = ComputeVFMatrix(VFReciprocal);
            //VFactors = FindInconsistencies(Regular, Reciprocal);
            VFactors = ComputeVFMatrix(VFPerFace);
            f_ComputeVFMatrix = false;
        }

        Dictionary<ViewFactorCoordinate, float> ComputeVFMatrix(List<ViewFactorPerFace> vfList)
        {
            Dictionary<ViewFactorCoordinate, float> Results = new Dictionary<ViewFactorCoordinate, float>();
            foreach (ViewFactorPerFace vf in vfList)
            {
                // Get coordinate, swap source and target
                string SourceName = vf.Coordinate.SourceName;
                string TargetName = vf.Coordinate.TargetName;
                ViewFactorCoordinate C = new ViewFactorCoordinate(SourceName, TargetName);
                // Check if already in dictionary
                if (Results.ContainsKey(C))
                {
                    Results[C] += vf.Factor;
                }
                // New entry
                else
                {
                    Results.Add(C, vf.Factor);
                }
            }
            return Results;
        }

        Dictionary<ViewFactorCoordinate, float> FindInconsistencies(Dictionary<ViewFactorCoordinate, float> regular, Dictionary<ViewFactorCoordinate, float> reciprocal)
        {
            Dictionary<ViewFactorCoordinate, float> Results = new Dictionary<ViewFactorCoordinate, float>();
            foreach (KeyValuePair<ViewFactorCoordinate, float> p in regular)
            {
                reciprocal.TryGetValue(p.Key, out float VFReciprocal);
                if (p.Value == VFReciprocal)
                {
                    Results.Add(p.Key, p.Value);
                }
                else
                {
                    float VFMax = Math.Max(p.Value, VFReciprocal);
                    if (VFReciprocal == VFMax)
                    {
                        Console.WriteLine("\t Found deviation for F(" + p.Key.SourceName + " -> " + p.Key.TargetName + "):");
                        Console.WriteLine("\t\t VF was " + p.Value.ToString("F3", Settings.Format) + ", corrected it to " + VFMax.ToString("F3", Settings.Format) + ".");
                    }
                    Results.Add(p.Key, VFMax);
                }
            }

            return Results;
        }

        void WriteMatrixToFile()
        {
            using (StreamWriter sw = new StreamWriter("matrix.txt"))
            {
                // Write header
                sw.WriteLine("% View factor matrix generated on " + DateTime.Now.ToLongDateString());
                sw.WriteLine("% One row per source, e.g. row n: n -> target");
                // Write list of components
                int i = 1;
                foreach (SatComponent sc in Model.Components)
                {
                    sw.WriteLine("% Row / column " + i++ + ": " + sc.Name);
                }
                // Write matrix
                foreach (SatComponent source in Model.Components)
                {
                    foreach (SatComponent target in Model.Components)
                    {
                        ViewFactorCoordinate C = new ViewFactorCoordinate(source.Name, target.Name);
                        if (source == target)
                        {
                            sw.Write("0");
                        }
                        else
                        {
                            if(VFactors.TryGetValue(C, out float Factor))
                            {
                                sw.Write(Factor.ToString("F3", Settings.Format));
                            }
                        }
                        sw.Write("\t");
                    }
                    sw.Write("\n");
                }
            }
        }
        
        #endregion
    }

    #region data structs

    public struct ViewFactorPerFace
    {
        public ViewFactorCoordinate Coordinate { get; private set; }
        public float Factor { get; private set; }

        public AAFace SourceFace { get; private set; }
        public AAFace TargetFace { get; private set; }

        public ViewFactorPerFace(string sourceName, string targetName, AAFace sourceFace, AAFace targetFace,  float factor)
        {
            Coordinate = new ViewFactorCoordinate(sourceName, targetName);
            Factor = factor;
            TargetFace = targetFace;
            SourceFace = sourceFace;
        }
    }

    public struct ViewFactorCoordinate
    {
        public string SourceName { get; private set; }
        public string TargetName { get; private set; }

        public ViewFactorCoordinate(string sourceName, string targetName)
        {
            SourceName = sourceName;
            TargetName = targetName;
        }
    }

    #endregion
}
