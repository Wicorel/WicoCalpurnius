using System;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	internal class ConvoyGround : Convoy
	{
		internal const int AdditionalHeightModifier = 5;

		public ConvoyGround(Vector3D destination, NpcGroupState initialState, NpcGroupArrivalObserver arrivalObserver,
			QueuedAudioSystem audioSystem, IMyCubeGrid leader, DateTime groupSpawnTime)
			: base(destination, initialState, arrivalObserver, audioSystem, leader, groupSpawnTime)
		{
		}

		protected override int GetAdditionalHeightModifier()
		{
			return AdditionalHeightModifier;
		}

		protected override bool IsUnitTypeAllowedToJoin(UnitType unitType)
		{
			return true; // Anything is okay!
		}

		internal override UnitType GetLeaderUnitType()
		{
			return UnitType.Ground;
		}
	}
}