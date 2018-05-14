/*
 * 
 * Class to manage keyboard input
 * 
 */

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace TransRad
{
    public static class Input
    {
        #region fields

        static Stopwatch sw_KeyDelay;

        static int MouseWheelPosOld;

        #endregion

        public static void InitInput()
        {
            sw_KeyDelay = new Stopwatch();
            sw_KeyDelay.Start();

            MouseWheelPosOld = Mouse.GetState().ScrollWheelValue;
        }

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

        public static bool LoadSettings
        {
            get
            {
                return Keyboard.GetState().IsKeyDown(Keys.F9) && KeyDelay;
            }
        }

        public static bool SaveSettings
        {
            get
            {
                return Keyboard.GetState().IsKeyDown(Keys.F5) && KeyDelay;
            }
        }

        static bool KeyDelay
        {
            get
            {
                if (sw_KeyDelay.ElapsedMilliseconds >= Settings.KeyDelay)
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

        #endregion

        #region internal

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

    }
}
