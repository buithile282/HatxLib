using System.Threading.Tasks;

namespace HAtxLib
{
    /// <summary>
    /// Async operations for HAtx
    /// </summary>
    public partial class HAtx
    {
        #region Async Touch Operations

        /// <summary>
        /// Asynchronously clicks on the screen at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="y">Y coordinate (0-1 for relative, >1 for absolute)</param>
        /// <returns>Task that returns true if click was successful</returns>
        public Task<bool> ClickAsync(float x, float y)
        {
            return Task.Run(() => Click(x, y));
        }

        /// <summary>
        /// Asynchronously swipes from one point to another
        /// </summary>
        /// <param name="fx">Start X coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="fy">Start Y coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="lx">End X coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="ly">End Y coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="duration">Duration in milliseconds (minimum 2ms)</param>
        /// <returns>Task that returns true if swipe was successful</returns>
        public Task<bool> SwipeAsync(float fx, float fy, float lx, float ly, int duration = Configuration.AtxConstants.DEFAULT_SWIPE_DURATION)
        {
            return Task.Run(() => Swipe(fx, fy, lx, ly, duration));
        }

        /// <summary>
        /// Asynchronously drags from one point to another
        /// </summary>
        /// <param name="fx">Start X coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="fy">Start Y coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="lx">End X coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="ly">End Y coordinate (0-1 for relative, >1 for absolute)</param>
        /// <param name="duration">Duration in milliseconds (minimum 2ms, multiplied by 200 internally)</param>
        /// <returns>Task that returns true if drag was successful</returns>
        public Task<bool> DragAsync(float fx, float fy, float lx, float ly, int duration = Configuration.AtxConstants.DEFAULT_SWIPE_DURATION)
        {
            return Task.Run(() => Drag(fx, fy, lx, ly, duration));
        }

        #endregion

        #region Async Screen Operations

        /// <summary>
        /// Asynchronously dumps the current screen hierarchy
        /// </summary>
        /// <returns>Task that returns XML string representation of the screen hierarchy</returns>
        public Task<string> DumpHierarchyAsync()
        {
            return Task.Run(() => DumpHierarchy());
        }

        #endregion

        #region Async Device Operations

        /// <summary>
        /// Asynchronously checks if the device is alive and responsive
        /// </summary>
        /// <returns>Task that returns true if device is alive</returns>
        public Task<bool> IsAliveAsync()
        {
            return Task.Run(() => IsAlive());
        }

        #endregion

        #region Async App Operations

        /// <summary>
        /// Asynchronously starts an application
        /// </summary>
        /// <param name="package">Package name of the application</param>
        /// <param name="monkey">If true, uses monkey command to start the app</param>
        /// <param name="stop">If true, stops the app before starting</param>
        /// <param name="wait">If true, waits for the app to start</param>
        /// <param name="activity">Activity name to start (optional, uses main activity if not specified)</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task AppStartAsync(string package, bool monkey = false, bool stop = false, bool wait = false, string activity = null)
        {
            return Task.Run(() => AppStart(package, monkey, stop, wait, activity));
        }

        /// <summary>
        /// Asynchronously waits for an application to start
        /// </summary>
        /// <param name="package">Package name of the application</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="activity">Activity name to wait for (optional)</param>
        /// <param name="front">If true, checks if app is in foreground</param>
        /// <returns>Task that returns true if app started within timeout</returns>
        public Task<bool> AppWaitAsync(string package, int timeout = Configuration.AtxConstants.DEFAULT_TIMEOUT, string activity = null, bool front = false)
        {
            return Task.Run(() => AppWait(package, timeout, activity, front));
        }

        #endregion
    }
}
