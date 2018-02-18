using System;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	internal class ConvoyAir : Convoy
	{
		internal const int AdditionalHeightModifier = 0;

		public ConvoyAir(Vector3D destination, NpcGroupState initialState, NpcGroupArrivalObserver arrivalObserver,
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
			return unitType == UnitType.Air;
		}

		internal override UnitType GetLeaderUnitType()
		{
			return UnitType.Air;
		}
	}
}