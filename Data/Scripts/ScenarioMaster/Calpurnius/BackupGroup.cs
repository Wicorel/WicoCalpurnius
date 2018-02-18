using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	internal class BackupGroup : NpcGroup
	{
		private IMyCubeGrid leader;
		private readonly QueuedAudioSystem audioSystem;
		private const string InterceptingBeaconSuffix = " *PURSUING TARGET*";
		private const string ReturningToBase = " *RETURNING TO BASE*";

		internal BackupGroup(NpcGroupState initialState, Vector3D destination, IMyCubeGrid leader,
			NpcGroupArrivalObserver npcGroupArrivalObserver, QueuedAudioSystem audioSystem, DateTime groupSpawnTime)
			: base(leader.EntityId, initialState, destination, NpcGroupType.Backup, groupSpawnTime, npcGroupArrivalObserver)
		{
			this.leader = leader;
			this.audioSystem = audioSystem;
		}

		internal override bool IsJoinable(UnitType unitType)
		{
			return false; // For now only 1 ship
		}

		internal override void JoinAsEscort(IMyCubeGrid escort, UnitType unitType, MyPlanet marsPlanet)
		{
			throw new ArgumentException("Not allowed!");
		}

		internal override void Update()
		{
			if (!leader.IsControlledByNpcFaction())
			{
				leader = null;
				GroupState = NpcGroupState.Disbanded;
				return;
			}
            
			if (GroupState == NpcGroupState.Travelling && Vector3D.Distance(Destination, leader.GetPosition()) < 40.0)
			{
				GroupState = NpcGroupState.Disbanding;
			}

			if (GroupState == NpcGroupState.Disbanding)
			{
				var isArmed = leader.HasUsableGun();
				if (AttemptDespawn(leader))
				{
					leader = null;
					GroupState = NpcGroupState.Disbanded;
					if (isArmed)
					{
						ArrivalObserver.GroupArrivedIntact();
					}
				}
				return;
			}

			if (DuckUtils.IsAnyPlayerNearPosition(leader.GetPosition(), 1000) &&
			    (GroupState == NpcGroupState.Travelling || GroupState == NpcGroupState.Disbanding))
			{
				GroupState = NpcGroupState.InCombat;
				leader.SetLightingColors(Color.Red);
				leader.RemoveFromFirstBeaconName(ReturningToBase);
				leader.AppendToFirstBeaconName(InterceptingBeaconSuffix);
				audioSystem.PlayAudio(CalAudioClip.TargetFoundDronesAttack, CalAudioClip.TargetIdentifiedUnitsConverge);
			}

			if (GroupState == NpcGroupState.InCombat)
			{
				if (!leader.HasUsableGun())
				{
					GroupState = NpcGroupState.Disbanding;
					leader.SendToPosition(Destination);
					audioSystem.PlayAudio(CalAudioClip.DroneDisarmed);
				}
				else
				{
					var player = DuckUtils.GetNearestPlayerToPosition(leader.GetPosition(), 1250);
					if (player == null)
					{
						GroupState = NpcGroupState.Disbanding; // Return to normal, cowardly players have run off or died
                        // TODO: get lighting per-faction
						leader.SetLightingColors(GcorpBlue);
						leader.RemoveFromFirstBeaconName(InterceptingBeaconSuffix);
						leader.AppendToFirstBeaconName(ReturningToBase);
						leader.SendToPosition(Destination);
						audioSystem.PlayAudio(CalAudioClip.HostileDisappeared, CalAudioClip.TargetFleeingPursuit);
					}
					else
					{
						leader.SendToPosition(player.GetPosition(), 2);
					}
				}
			}
		}

		internal override List<EscortSaveData> GetEscortSaveData()
		{
			return new List<EscortSaveData>();
		}

		internal override UnitType GetLeaderUnitType()
		{
			return UnitType.Air;
		}

		internal override Vector3D GetPosition()
		{
			return leader.GetPosition();
		}
	}
}