using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario
{
	public interface IFaction
	{			
		IMyFaction MyFaction { get; }
		long FactionId { get; }
		string SubtitlesSpeaker { get; }
		MyFontEnum SubtitlesFont { get; }
		Color LightsColor { get; }
		bool IsNpc { get; }
		IFactionShips Ships { get; }
	}
}