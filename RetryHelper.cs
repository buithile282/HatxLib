using HAtxLib.Utils;
using System;
using System.Threading;

namespace HAtxLib
{
    /// <summary>
    /// Helper class for retry logic with timeout and max attempts support
    /// </summary>
    public static class RetryHelper
    {
        private readonly static HLog Log = HLog.Get<HAtx>("RetryHelper");

        /// <summary>
        /// Executes an action with retry logic
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="delayMs">Delay between retries in milliseconds</param>
        /// <param name="operationName">Name of the operation for logging purposes</param>
        /// <returns>True if action succeeded, false otherwise</returns>
        public static bool ExecuteWithRetry(Action action, int maxAttempts = 3, int delayMs = 1000, string operationName = "Operation")
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Log.Debug($"{operationName}: Attempt {attempt}/{maxAttempts}");
                    action();
                    Log.Info($"{operationName}: Succeeded on attempt {attempt}/{maxAttempts}");
                    return true;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxAttempts)
                    {
                        Log.Error($"{operationName}: Failed after {maxAttempts} attempts. Last error: {ex.Message}");
                        Log.Error($"Stack trace: {ex.StackTrace}");
                        return false;
                    }
                    
                    Log.Warn($"{operationName}: Attempt {attempt}/{maxAttempts} failed: {ex.Message}. Retrying in {delayMs}ms...");
                    
                    if (delayMs > 0)
                    {
                        Thread.Sleep(delayMs);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Executes a function with retry logic that returns a result
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="delayMs">Delay between retries in milliseconds</param>
        /// <param name="operationName">Name of the operation for logging purposes</param>
        /// <param name="defaultValue">Default value to return on failure</param>
        /// <returns>Result of the function, or default value on failure</returns>
        public static T ExecuteWithRetry<T>(Func<T> func, int maxAttempts = 3, int delayMs = 1000, string operationName = "Operation", T defaultValue = default(T))
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Log.Debug($"{operationName}: Attempt {attempt}/{maxAttempts}");
                    T result = func();
                    Log.Info($"{operationName}: Succeeded on attempt {attempt}/{maxAttempts}");
                    return result;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxAttempts)
                    {
                        Log.Error($"{operationName}: Failed after {maxAttempts} attempts. Last error: {ex.Message}");
                        Log.Error($"Stack trace: {ex.StackTrace}");
                        return defaultValue;
                    }
                    
                    Log.Warn($"{operationName}: Attempt {attempt}/{maxAttempts} failed: {ex.Message}. Retrying in {delayMs}ms...");
                    
                    if (delayMs > 0)
                    {
                        Thread.Sleep(delayMs);
                    }
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Executes an action with retry until a condition is met or timeout occurs
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="condition">The condition to check after each attempt</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        /// <param name="delayMs">Delay between retries in milliseconds</param>
        /// <param name="operationName">Name of the operation for logging purposes</param>
        /// <returns>True if condition was met, false on timeout</returns>
        public static bool ExecuteUntilCondition(Action action, Func<bool> condition, int timeoutSeconds = 30, int delayMs = 1000, string operationName = "Operation")
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            DateTime startTime = DateTime.Now;
            int attempt = 0;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                attempt++;
                try
                {
                    Log.Debug($"{operationName}: Attempt {attempt}, elapsed: {(DateTime.Now - startTime).TotalSeconds:F1}s/{timeoutSeconds}s");
                    action();
                    
                    if (condition())
                    {
                        Log.Info($"{operationName}: Condition met after {attempt} attempts in {(DateTime.Now - startTime).TotalSeconds:F1}s");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"{operationName}: Attempt {attempt} failed: {ex.Message}");
                }

                if (delayMs > 0)
                {
                    Thread.Sleep(delayMs);
                }
            }

            Log.Error($"{operationName}: Timeout after {timeoutSeconds}s and {attempt} attempts");
            return false;
        }
    }
}
