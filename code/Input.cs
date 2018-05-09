/*
 * Author: Max Gulde
 * Last Update: 2018-02-06
 */

#region using

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

#endregion

namespace ObjectViewer
{
    public static class Input
    {

        #region settings

        public static int tKeyDelay = 200;

        #endregion

        #region fields

        static Stopwatch sw_KeyDelay;
        static Stopwatch sw_SimDelay;

        static int MouseWheelPosOld;

        #endregion

        public static void Initialize()
        {
            sw_KeyDelay = new Stopwatch();
            sw_KeyDelay.Start();
            sw_SimDelay = new Stopwatch();
            sw_SimDelay.Start();

            MouseWheelPosOld = Mouse.GetState().ScrollWheelValue;
        }

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

        static bool KeyDelay
        {
            get
            {
                if (sw_KeyDelay.ElapsedMilliseconds >= tKeyDelay)
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

        public static int Zoom
        {
            get
            {
                int MouseWheelPos = Mouse.GetState().ScrollWheelValue;
                int diff = MouseWheelPos - MouseWheelPosOld;
                MouseWheelPosOld = Mouse.GetState().ScrollWheelValue;
                return diff;
            }
        }

        public static bool SwitchView
        {
            get
            {
                return Keyboard.GetState().IsKeyDown(Keys.Space) && KeyDelay;
            }
        }

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
