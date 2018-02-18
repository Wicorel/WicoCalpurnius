using VRage.Game;

namespace Scenario
{
	public interface IAudioClip
	{
		string Filename { get; }
		string Subtitle { get; }
		string Speaker { get; }
		MyFontEnum Font { get; }
		int DisappearTimeMs { get; }
		int Id { get; }
	}
}