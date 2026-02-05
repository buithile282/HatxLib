using HAtxLib.Catch;
using System;
using System.Diagnostics;
using System.Threading;

namespace HAtxLib.Utils
{
    /// <summary>
    /// Helper class for retry logic and timeout operations
    /// </summary>
    public static class RetryHelper
    {
        private readonly static HLog Log = HLog.Get("RetryHelper");

        /// <summary>
        /// Executes an operation with retry logic
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="successCondition">Optional condition to check if result is successful</param>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="delayMs">Delay between retries in milliseconds</param>
        /// <param name="onRetry">Optional callback on each retry</param>
        /// <returns>Result of the operation</returns>
        public static T ExecuteWithRetry<T>(
            Func<T> operation,
            Func<T, bool> successCondition = null,
            int maxRetries = 10,
            int delayMs = 500,
            Action<int, Exception> onRetry = null)
        {
            Exception lastException = null;
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    T result = operation();
                    
                    // If no success condition provided, assume success
                    if (successCondition == null || successCondition(result))
                    {
                        return result;
                    }
                    
                    // Result didn't meet success condition, retry
                    if (i < maxRetries - 1)
                    {
                        Thread.Sleep(delayMs);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    onRetry?.Invoke(i + 1, ex);
                    
                    if (i < maxRetries - 1)
                    {
                        Log.Debug($"Retry attempt {i + 1}/{maxRetries} failed: {ex.Message}");
                        Thread.Sleep(delayMs);
                    }
                }
            }
            
            // All retries exhausted
            if (lastException != null)
            {
                throw new ATXException($"Operation failed after {maxRetries} attempts", lastException);
            }
            
            throw new ATXException($"Operation did not meet success condition after {maxRetries} attempts");
        }

        /// <summary>
        /// Executes a void operation with retry logic
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="delayMs">Delay between retries in milliseconds</param>
        /// <param name="onRetry">Optional callback on each retry</param>
        public static void ExecuteWithRetry(
            Action operation,
            int maxRetries = 10,
            int delayMs = 500,
            Action<int, Exception> onRetry = null)
        {
            ExecuteWithRetry<object>(() => 
            {
                operation();
                return null;
            }, null, maxRetries, delayMs, onRetry);
        }

        /// <summary>
        /// Executes a condition with timeout
        /// </summary>
        /// <param name="condition">Condition to check</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="checkIntervalMs">Interval between checks in milliseconds</param>
        /// <returns>True if condition became true before timeout, false otherwise</returns>
        public static bool ExecuteWithTimeout(
            Func<bool> condition,
            int timeoutMs,
            int checkIntervalMs = 100)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    if (condition())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"Condition check failed: {ex.Message}");
                }
                
                Thread.Sleep(checkIntervalMs);
            }
            
            return false;
        }

        /// <summary>
        /// Waits until a condition becomes true or timeout occurs
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Operation that returns a value</param>
        /// <param name="successCondition">Condition to check the result</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="checkIntervalMs">Interval between checks in milliseconds</param>
        /// <returns>The result if condition met, default(T) otherwise</returns>
        public static T WaitForCondition<T>(
            Func<T> operation,
            Func<T, bool> successCondition,
            int timeoutMs,
            int checkIntervalMs = 100)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            T lastResult = default(T);
            
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    lastResult = operation();
                    if (successCondition(lastResult))
                    {
                        return lastResult;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"Operation failed during wait: {ex.Message}");
                }
                
                Thread.Sleep(checkIntervalMs);
            }
            
            return lastResult;
        }
    }
}
