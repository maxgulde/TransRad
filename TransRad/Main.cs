/* Radiate Energy Transport Tests Project
 * 
 * Application control
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-15
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

        Stopwatch StopWatchFPS;
        Stopwatch SW;
        int TimerFPS;
        int TimerMaxFPS;

        List<AAFace> FaceList;
        int RadTargetIdx;
        int RadTargetFaceIdx;
        int RadSourceIdx;
        int RadSourceFaceIdx;
        bool f_IndicesInitalized = false;
        bool f_ComputationEnded = true;
        bool f_ComputeVFMatrix = false;

        // Verification
        bool f_StartVerification = false;
        float v_DistStep = 0.01f;
        int v_MaxStepNum = 490;
        int v_StepNum = 0;
        Dictionary<float, float> v_Results;

        // Results
        List<ViewFactorPerFace> VFPerFace;
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
            VFactors = new Dictionary<ViewFactorCoordinate, float>();

            // Start stopwatch
            StopWatchFPS = new Stopwatch();
            SW = new Stopwatch();

            base.Initialize();
        }

        private void InitIndices()
        {
            RadTargetIdx = 0;
            RadTargetFaceIdx = 0;
            RadSourceIdx = 1;
            RadSourceFaceIdx = 0;
        }

        #endregion

        #region loading

        protected override void LoadContent()
        {
            // Loading assets
            Effect Effect = Content.Load<Effect>("Default");
            SEffect = Content.Load<Effect>("Default2D");
            Model = new SatModel(Content.Load<Model>("TwoPlates"), Effect, GraphicsDevice);
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
                //{
                //    f_ComputationEnded = false;
                //}
                //if (!f_ComputationEnded)
            {
                Settings.f_ComputeArea = true;
                Settings.f_DrawCompleteModel = false;
                Settings.f_UseUniformMMap = false;
                if (!f_IndicesInitalized)
                {
                    InitIndices();
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
                    }
                }
                Console.WriteLine("Computing view factor");
                Console.Write("\t from (" + Model.Components[RadSourceIdx].Name + "," + FaceList[RadSourceFaceIdx] + ")");
                Console.WriteLine(" to (" + Model.Components[RadTargetIdx].Name + "," + FaceList[RadTargetFaceIdx] + ")");
            }

            #endregion

            #region create view factor matrix

            if (f_ComputeVFMatrix)
            {
                Console.WriteLine("Creating view factor matrix:");
                ComputeVFMatrix();
                f_ComputeVFMatrix = false;
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
                //if (Input.NextTarget)
                //{
                //    RadTargetIdx++;
                //    RadTargetIdx = RadTargetIdx >= Model.MeshNumber ? 0 : RadTargetIdx;
                //    RadTargetFaceIdx = 0;
                //    Settings.f_ComputeArea = true;
                //}
                //if (Input.NextSource)
                //{
                //    RadSourceIdx++;
                //    RadSourceIdx = RadSourceIdx >= Model.MeshNumber ? 0 : RadSourceIdx;
                //    Settings.f_ComputeArea = true;
                //}
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
            AAFace TargetFace = FaceList[RadTargetFaceIdx];
            AAFace SourceFace = FaceList[RadSourceFaceIdx];

            #region free view

            GraphicsDevice.SetRenderTarget(RTFree);
            GraphicsDevice.Clear(Color.White);
            Model.DrawCompleteModel(CamFree, false, Color.Black, -1, true);
            Model.Components[RadSourceIdx].DrawBoundingBoxFace(CamFree, SourceFace, Color.Red); 
            Model.Components[RadTargetIdx].DrawBoundingBoxFace(CamFree, TargetFace, Color.Green);
            // Draw pointer
            if (Settings.f_DrawPointer)
            {
                Vector3 CamPosition = Model.Components[RadTargetIdx].GetFaceCenter(TargetFace);
                DrawModel(CamFree, Pointer, CamPosition);
                foreach (AAFace face in HemiCube.GetFaceList(TargetFace))
                {
                    DrawModel(CamFree, Pointer, Model.Components[RadTargetIdx].GetFaceDirection(CamPosition, face));
                }
            }

            #endregion

            #region verification

            if (f_StartVerification)
            {
                CamHemiCube.TranslateZ(v_StepNum * v_DistStep);
            }

            #endregion

            #region radiosity

            // Generate HemiCube textures
            DrawHemiCubeTextures(SourceFace, Model.Components[RadTargetIdx], TargetFace);
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
                // Areas
                float SourceArea = Model.Components[RadSourceIdx].GetBBoxTotalArea();
                float SourceFaceArea = Model.Components[RadSourceIdx].GetBBoxFaceArea(SourceFace);
                float TargetArea = Model.Components[RadTargetIdx].GetBBoxTotalArea();
                float TargetFaceArea = Model.Components[RadTargetIdx].GetBBoxFaceArea(TargetFace);
                float AreaRatioSource = SourceFaceArea / SourceArea;
                float AreaRatioTarget = TargetFaceArea / TargetArea;
                // View factor
                float Factor = GetPixelSum(RTViewFactor);
                float WeightedFactor = Factor * AreaRatioSource * AreaRatioTarget;
                // Save results
                string SourceName = Model.Components[RadSourceIdx].Name;
                string TargetName = Model.Components[RadTargetIdx].Name;
                VFPerFace.Add(new ViewFactorPerFace(SourceName, TargetName, SourceFace, TargetFace, WeightedFactor));
                // Text output
                Console.Write("From (" + SourceName + "," + SourceFace + ")");
                Console.WriteLine(" to (" + TargetName + "," + TargetFace + ")");
                Console.Write("\t Weighted view factor = " + WeightedFactor.ToString("F6", Settings.Format));
                Console.WriteLine(" (" + Factor.ToString("F3", Settings.Format) + ")");
                Console.WriteLine("\t Source area ratio = " + AreaRatioSource.ToString("F4", Settings.Format));
                Console.WriteLine("\t Target area ratio = " + AreaRatioTarget.ToString("F4", Settings.Format));
                
                Settings.f_ComputeArea = false;
                // Save values for verification
                if (f_StartVerification)
                {
                    v_Results.Add(v_StepNum * v_DistStep, Factor);
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

        void DrawHemiCubeTextures(AAFace radSourceFace, SatComponent radTarget, AAFace radTargetFace)
        {
            // Set camera position on radition target face
            Vector3 Position = radTarget.GetFaceCenter(radTargetFace);

            // Get List of camera orientations
            List<AAFace> CameraDirections = HemiCube.GetFaceList(radTargetFace);

            for (int i = 0; i < CameraDirections.Count; i++)
            {
                // Orient camera
                Vector3 CameraPointingTarget = radTarget.GetFaceDirection(Position, CameraDirections[i]);
                CamHemiCube.SetPositionTarget(Position, CameraPointingTarget, true);

                // Prepare rendertarget
                GraphicsDevice.SetRenderTarget(HemiCube.RTIndividual[i]);
                GraphicsDevice.Clear(Color.Black);

                // Render complete model
                if (Settings.f_DrawCompleteModel)
                {
                    Model.DrawCompleteModel(CamHemiCube, false, Color.White, RadTargetIdx);
                }
                // Render only currently active radiation source (single face)
                else
                {
                    // Draw black bounding boxes for complete model
                    Model.DrawCompleteModel(CamHemiCube, true, Color.Black);
                    // Draw single bounding box face
                    Model.Components[RadSourceIdx].DrawBoundingBoxFace(CamHemiCube, radSourceFace, Color.White);
                }
            }
        }

        void MergeHemiCubeTextures()
        {
            // Prepare variables
            int TextureSize = Settings.D_HemiCubeResolution;
            AAFace CurrentTargetFace = FaceList[RadTargetFaceIdx];
            Vector2 Origin = Vector2.One * TextureSize * 0.5f;
            Vector2 Position;
            Rectangle Source;
            float Rotation;

            // Init drawing (around texture center)
            GraphicsDevice.SetRenderTarget(RTHemiCube);
            GraphicsDevice.Clear(Color.DarkRed);
            SBatch.Begin();

            #region front

            switch (CurrentTargetFace)
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

            switch (CurrentTargetFace)
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

            switch (CurrentTargetFace)
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

            switch (CurrentTargetFace)
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

            switch (CurrentTargetFace)
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

        void ComputeVFMatrix()
        {
            foreach (ViewFactorPerFace factorFace in VFPerFace)
            {
                // Get coordinate, swap source and target
                string TargetName = factorFace.Coordinate.TargetName;
                string SourceName = factorFace.Coordinate.SourceName;
                ViewFactorCoordinate C = new ViewFactorCoordinate(SourceName, TargetName);
                // Check if already in dictionary
                if (VFactors.ContainsKey(C))
                {
                    VFactors[C] += factorFace.Factor;
                }
                // New entry
                else
                {
                    VFactors.Add(C, factorFace.Factor);
                }
            }
        }

        void WriteMatrixToFile()
        {
            using (StreamWriter sw = new StreamWriter("matrix.txt"))
            {
                // Write header
                sw.WriteLine("% View factor matrix generated on " + DateTime.Now.ToLongDateString());
                // Write list of components
                int i = 1;
                foreach (SatComponent sc in Model.Components)
                {
                    sw.WriteLine("% Row / column " + i++ + ": " + sc.Name);
                }
                // Write matrix
                foreach (SatComponent sc_r in Model.Components)
                {
                    foreach (SatComponent sc_c in Model.Components)
                    {
                        ViewFactorCoordinate C = new ViewFactorCoordinate(sc_r.Name, sc_c.Name);
                        if (sc_r == sc_c)
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
}
