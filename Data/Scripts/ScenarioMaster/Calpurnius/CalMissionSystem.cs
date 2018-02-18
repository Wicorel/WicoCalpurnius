using System;
using System.Collections.Generic;
using VRageMath;

namespace Scenario.Calpurnius
{
	public class CalMissionSystem : AbstractMissionSystem
	{
		// TODO: Need correct waypoints for Calpurnius, below is an example
		private readonly Vector3D droneWreck = new Vector3D(1854936.75, -2006193.25, 1325297.5);

		private readonly ResearchControl researchControl;

		internal CalMissionSystem(long missionStartTimeBinary, HashSet<int> alreadyExecutedPrompts,
			QueuedAudioSystem audioSystem, ResearchControl researchControl) : base(missionStartTimeBinary, alreadyExecutedPrompts, audioSystem)
		{
			this.researchControl = researchControl;
		}

		protected override void GeneratePrompts()
		{
			// TODO: Add desired tech unlocks for Scenario
			AddTimePrompt(1000, new TimeSpan(0, 0, 10),
				UnlockAllTech(),
				PlayAudioClip(CalAudioClip.AllTechUnlocked));

			AddProximityPrompt(1000, droneWreck, 3000,
				PlayAudioClip(CalAudioClip.AllTechUnlocked));
		}

		internal Action UnlockAllTech()
		{
			return () =>
			{
				foreach (TechGroup techGroup in Enum.GetValues(typeof(TechGroup)))
				{
					researchControl.UnlockTechGroupForAllPlayers(techGroup);
				}
			};
		}
	}
}