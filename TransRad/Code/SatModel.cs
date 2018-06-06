/* Radiate Energy Transport Tests Project
 * 
 * Data struct for complete satellite model.
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-05
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
    public class SatModel
    {
        // List with satellite components
        public List<SatComponent> Components { get; private set; }

        #region ctr

        public SatModel(Model model, Effect effect, GraphicsDevice gfx)
        {
            // Disseminate the model to component level.
            DisseminateModel(model, effect, gfx);

            // Analysing model
            Console.WriteLine("Loaded model with " + MeshNumber + " meshes.");
            if (MeshNumber < 2)
            {
                Console.WriteLine("### Error ### Not enough meshed found in model.");
            }
        }

        #endregion

        #region model

        public void DisseminateModel(Model model, Effect effect, GraphicsDevice gfx)
        {
            Components = new List<SatComponent>();
            for (int i = 0; i < model.Meshes.Count; i++)
            {
                Components.Add(new SatComponent(model.Meshes[i], effect, gfx));
            }
        }

        public int MeshNumber
        {
            get
            {
                return Components != null ? Components.Count : -1;
            }
        }

        #endregion

        #region draw

        // Draw the complete model / scene
        public void DrawCompleteModel(Camera cam, bool drawBoundingBoxes, Vector3 color, int excludeComponent = -1, bool useIndividualColors = false)
        {
            for (int i = 0; i < MeshNumber; i++)
            {
                if (i != excludeComponent)
                {
                    Vector3 C = useIndividualColors ? Tools.GetColorFromIndex(i, MeshNumber).ToVector3() : color;
                    if (drawBoundingBoxes)
                    {
                        Components[i].DrawBoundingBox(cam);
                    }
                    else
                    {
                        Components[i].DrawMesh(cam, C);
                    }
                }
            }
        }

        public void DrawComponent(Camera cam, int targetIdx)
        {
            Components[targetIdx].DrawMesh(cam, Color.White.ToVector3());
        }

        #endregion
    }
}
