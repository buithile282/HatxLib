using System;
using System.Diagnostics;

namespace HAtxLib.Utils {
	public class HRuntime {
		private readonly static HLog Log = HLog.Get<HRuntime>("Timer");

		public static void Run(Action action) {
			Run("HRuntime", action);
		}

		public static void Run(string name, Action action) {
			var watch = Stopwatch.StartNew();
			action.Invoke();
			watch.Stop();
			Log.Warn($"{name} execution time: {watch.Elapsed.TotalMilliseconds}ms");
		}

		public static T Run<T>(Func<T> action) {
			return Run("HRuntime", action);
		}

		public static T Run<T>(string name, Func<T> action) {
			var watch = Stopwatch.StartNew();
			T value = action.Invoke();
			watch.Stop();
			Log.Warn($"{name} execution time: {watch.Elapsed.TotalMilliseconds}ms");
			return value;
		}

		public static T Time<T>(Func<T> action, out int usetime) {
			var watch = Stopwatch.StartNew();
			T value = action.Invoke();
			watch.Stop();
			usetime = (int)watch.Elapsed.TotalMilliseconds;
			return value;
		}

	}
}