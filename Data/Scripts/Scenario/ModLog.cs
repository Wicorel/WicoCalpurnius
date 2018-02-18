using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Scenario
{
	/// Log class written by Digi and adapted for EFM
	public static class ModLog
	{
		public static bool DebugMode = false;

		public const string ModName = "Calpurnius";
		public const int WorkshopId = 873852597;//TODO CHANGE
		public const string LogFile = "CAL.log";

		private static System.IO.TextWriter _writer;
		private static IMyHudNotification _notify;
		private static readonly StringBuilder Cache = new StringBuilder(64);
		private static List<string> _preInitMessages = new List<string>(0);
		private static int _indent;

		public static void IncreaseIndent()
		{
			_indent++;
		}

		public static void DecreaseIndent()
		{
			if (_indent > 0)
				_indent--;
		}

		public static void ResetIndent()
		{
			_indent = 0;
		}

		public static void DebugInfo(string msg)
		{
			if (DebugMode)
			{
				MyAPIGateway.Utilities.ShowNotification("Debug: " + msg, 10000, MyFontEnum.Blue);
			}
		}

		public static void DebugError(string msg)
		{
			if (DebugMode)
			{
				Error(msg);
			}
		}

		public static void DebugError(string msg, Vector3D atPosition)
		{
			if (DebugMode)
			{
				Error(msg);
				DuckUtils.AddGpsToAllPlayers("ERROR: " + msg, "An error happened here!", atPosition);
			}
		}

		public static void Error(Exception e)
		{
			Error(e.ToString());
		}

		public static void Error(string msg)
		{
			if (DebugMode)
			{
				MyAPIGateway.Utilities.ShowNotification("ERROR: " + msg, 10000, MyFontEnum.Red);
			}
			else
			{
				MyAPIGateway.Utilities.ShowNotification(
					@"Calpurinus Mod has encountered an error. Please post your log on steam workshop.
It can be found at C:\Users\[YOUR USER]\AppData\Roaming\SpaceEngineers\SpaceEngineers.log", 10000, MyFontEnum.Red);
			}
			PrintError(msg);
		}

		private static void PrintError(string msg)
		{
			Info("ERROR: " + msg);

			try
			{
				MyLog.Default.WriteLineAndConsole(ModName + " error: " + msg);

				if (MyAPIGateway.Session != null)
				{
					var text = ModName + " error - open %AppData%/SpaceEngineers/Storage/" + WorkshopId + ".sbm_" + ModName + "/" +
					           LogFile +
					           " for details";

					if (_notify == null)
					{
						_notify = MyAPIGateway.Utilities.CreateNotification(text, 10000, MyFontEnum.Red);
					}
					else
					{
						_notify.Text = text;
						_notify.ResetAliveTime();
					}

					_notify.Show();
				}
			}
			catch (Exception e)
			{
				Info("ERROR: Could not send notification to local client: " + e);
			}
		}

		public static void Info(string msg)
		{
			Write(msg);
		}

		private static void Write(string msg)
		{
			try
			{
				Cache.Clear();
				Cache.Append(DateTime.Now.ToString("[HH:mm:ss] "));

				if (_writer == null)
					Cache.Append("(PRE-INIT) ");

				for (var i = 0; i < _indent; i++)
				{
					Cache.Append("\t");
				}

				Cache.Append(msg);

				if (_writer == null)
				{
					_preInitMessages.Add(Cache.ToString());
				}
				else
				{
					_writer.WriteLine(Cache);
					_writer.Flush();
				}

				Cache.Clear();
			}
			catch (Exception e)
			{
				MyLog.Default.WriteLineAndConsole(ModName + " had an error while logging message='" + msg + "'\nLogger error: " +
				                                  e.Message + "\n" + e.StackTrace);
			}
		}

		public static void Init()
		{
			if (MyAPIGateway.Utilities == null)
			{
				MyLog.Default.WriteLineAndConsole(ModName + " Log.Init() called before API was ready!");
				return;
			}

			if (_writer != null)
			{
				Close();
			}

			_writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(LogFile, typeof(ModLog));

			if (_preInitMessages.Count > 0)
			{
				foreach (var msg in _preInitMessages)
				{
					Error(msg);
				}

				_preInitMessages = new List<string>(0);
			}
		}

		public static void Close()
		{
			if (_writer != null)
			{
				_writer.Flush();
				_writer.Close();
				_writer = null;
			}

			_indent = 0;
			Cache.Clear();
		}
	}
}