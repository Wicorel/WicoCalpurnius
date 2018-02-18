using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario
{
	public abstract class AbstractFaction : IFaction
	{
		public IMyFaction MyFaction { get; }
		public long FactionId { get; }
		public string SubtitlesSpeaker { get; }
		public MyFontEnum SubtitlesFont { get; }
		public Color LightsColor { get; }
		public bool IsNpc { get; }
		public IFactionShips Ships { get; }

		internal AbstractFaction(string tag, string subtitlesSpeaker, MyFontEnum subtitlesFont, 
			Color lightsColor, bool isNpc, IFactionShips ships)
		{
			if (tag.Length < 4)
			{
				throw new Exception("Invalid faction tag, must be four characters or more so as not to clash with" +
				                    " player factions which are limited to three: " + tag);
			}
			MyFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
			if (MyFaction == null)
			{
				throw new Exception("Failed to create faction, not found: " + tag);
			}
			SubtitlesSpeaker = subtitlesSpeaker;
			SubtitlesFont = subtitlesFont;
			LightsColor = lightsColor;
			IsNpc = isNpc;
			Ships = ships;
			FactionId = MyFaction.FactionId;
		}
	}
}