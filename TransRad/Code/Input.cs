/* Radiate Energy Transport Tests Project
 * 
 * Mouse and keyboard input
 * 
 * Author: Max Gulde
 * Last Update: 2018-06-06
 * 
 */

#region using

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

#endregion

namespace TransRad
{
    public static class Input
    {
        #region fields

        static Stopwatch sw_KeyDelay;

        static int MouseWheelPosOld;

        #endregion

        #region init

        public static void InitInput()
        {
            sw_KeyDelay = new Stopwatch();
            sw_KeyDelay.Start();

            MouseWheelPosOld = Mouse.GetState().ScrollWheelValue;
        }

        #endregion

        #region general

        public static bool Exit
        {
            get
            {
                return Keyboard.GetState().IsKeyDown(Keys.Escape);
            }
        }

        public static bool ClickL
        {
            get
            {
                return Mouse.GetState().LeftButton == ButtonState.Pressed;
            }
        }

        public static Point MousePosition
        {
            get
            {
                return Mouse.GetState().Position;
            }
        }

        public static bool NextFace
        {
            get
            {
                return Keyboard.GetState().IsKeyDown(Keys.Space) && KeyDelay;
            }
        }

        public static bool NextObject
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.LeftAlt) || Keyboard.GetState().IsKeyDown(Keys.RightAlt)) && KeyDelay;
            }
        }

        public static bool StartComputation
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.Enter) && KeyDelay);
            }
        }


        #endregion

        #region internal

        static bool KeyDelay
        {
            get
            {
                if (sw_KeyDelay.ElapsedMilliseconds >= Settings.I_KeyDelay)
                {
                    sw_KeyDelay.Restart();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        static bool Shift
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift));
            }
        }

        static bool Control
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl));
            }
        }

        static bool Right
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.Right) || Keyboard.GetState().IsKeyDown(Keys.D) || Keyboard.GetState().IsKeyDown(Keys.NumPad6));
            }
        }

        static bool Left
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.Left) || Keyboard.GetState().IsKeyDown(Keys.A) || Keyboard.GetState().IsKeyDown(Keys.NumPad4));
            }
        }

        #endregion

        #region debug

        public static bool ToggleBBox
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.F1) && KeyDelay);
            }
        }

        public static bool TogglePointer
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.F2) && KeyDelay);
            }
        }

        public static bool ToggleMultiplierMap
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.F3) && KeyDelay);
            }
        }

        public static bool ComputeArea
        {
            get
            {
                return (Keyboard.GetState().IsKeyDown(Keys.F4) && KeyDelay);
            }
        }

        #endregion
    }
}
