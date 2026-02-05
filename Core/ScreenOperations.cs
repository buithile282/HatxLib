using HAtxLib.Catch;
using System;
using System.Drawing;
using System.Threading;

namespace HAtxLib.Core
{
    public class ScreenOperations
    {
        private readonly HAtx _hatx;

        public ScreenOperations(HAtx hatx)
        {
            _hatx = hatx;
        }

        #region Screen Click
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        /// <exception cref="ATXNodeException"></exception>
        public bool Click(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            var result = _hatx.JsonRpc("click", new int[] { pos.X, pos.Y });
            if (result == null)
            {
                return false;
            }
            if (result.Error != null)
            {
                throw new ATXNodeException("Click fail", result.Error);
            }
            return result.Data is bool s && s;
        }

        /// <summary>
        /// Screen Double Click
        /// </summary>
        /// <param name="x">Coordinate X</param>
        /// <param name="y">Coordinate Y</param>
        /// <param name="wait">Interval</param>
        /// <returns></returns>
        public bool DoubleClick(float x, float y, int wait = 60)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(_hatx, pos.X, pos.Y).Up(pos.X, pos.Y);
            Thread.Sleep(wait);
            Click(x, y);
            return false;
        }

        /// <summary>
        /// Long Press Screen
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="time"></param>
        public void LongClick(float x, float y, int time = 500)
        {
            Thread.Sleep(_hatx.UINodeClickExistDelay);
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(_hatx, pos.X, pos.Y).Wait(time).Up(pos.X, pos.Y);
        }
        #endregion

        #region Screen Swipe
        /// <summary>
        /// Swipe Screen
        /// </summary>
        /// <param name="fx">Start X</param>
        /// <param name="fy">Start Y</param>
        /// <param name="lx">End X</param>
        /// <param name="ly">End Y</param>
        /// <param name="duration">Duration</param>
        /// <returns></returns>
        public bool Swipe(float fx, float fy, float lx, float ly, int duration = 55)
        {
            if (duration < 2)
            {
                duration = 2;
            }
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = _hatx.JsonRpc("swipe", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null)
            {
                return false;
            }
            return result.Data is bool s && s;
        }

        #endregion

        #region Screen Operations

        public void TouchDown(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(_hatx, pos.X, pos.Y);
        }

        public void TouchMove(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Move(_hatx, pos.X, pos.Y);
        }

        public void TouchUp(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Up(_hatx, pos.X, pos.Y);
        }
        #endregion

        #region Drag
        /// <summary>
        /// Drag
        /// </summary>
        /// <param name="fx"></param>
        /// <param name="fy"></param>
        /// <param name="lx"></param>
        /// <param name="ly"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public bool Drag(float fx, float fy, float lx, float ly, int duration = 55)
        {
            if (duration < 2)
            {
                duration = 2;
            }
            duration *= 200;
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = _hatx.JsonRpc("drag", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null)
            {
                return false;
            }
            return result.Data is bool s && s;
        }
        #endregion

        #region Rel2Abs

        public Point Rel2Abs(float x, float y)
        {
            Point pos = new Point();
            Size size = _hatx.GetWindowSize();
            if (x > 1)
            {
                pos.X = (int)x;
            }
            else
            {
                pos.X = (int)(x * size.Width);
            }
            if (y > 1)
            {
                pos.Y = (int)y;
            }
            else
            {
                pos.Y = (int)(y * size.Height);
            }
            return pos;
        }

        #endregion

        #region AtxTouch

        internal class AtxTouch
        {
            private readonly HAtx _atx;
            internal AtxTouch(HAtx atx)
            {
                _atx = atx;
            }

            public AtxTouch Down(int x, int y)
            {
                Event(0, x, y);
                return this;
            }

            public AtxTouch Up(int x, int y)
            {
                Event(1, x, y);
                return this;
            }

            public AtxTouch Move(int x, int y)
            {
                Event(2, x, y);
                return this;
            }

            public AtxTouch Wait(int wait)
            {
                Thread.Sleep(wait);
                return this;
            }

            private void Event(int @event, int x, int y)
            {
                _ = _atx.JsonRpc("injectInputEvent", @event, x, y, 0) ?? throw new ATXException("AtxTouch.Move fail");
            }

            public static AtxTouch Down(HAtx atx, int x, int y)
            {
                return new AtxTouch(atx).Down(x, y);
            }

            public static AtxTouch Up(HAtx atx, int x, int y)
            {
                return new AtxTouch(atx).Up(x, y);
            }

            public static AtxTouch Move(HAtx atx, int x, int y)
            {
                return new AtxTouch(atx).Move(x, y);
            }
        }

        #endregion
    }
}
