using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	internal abstract class Convoy : NpcGroup
	{
		private const string InterceptingBeaconSuffix = " *INTERCEPTING*";
		private const string FleeingBeaconSuffix = " *FLEEING*";

		protected static readonly IList<EscortPosition> AllEscortPositions =
			new List<EscortPosition>(DuckUtils.GetEnumValues<EscortPosition>()).AsReadOnly();

		internal static readonly IList<EscortPosition> AirEscortPositions = new List<EscortPosition>
		{
			EscortPosition.AboveLeft,
			EscortPosition.AboveRight,
			EscortPosition.AboveLeftFar,
			EscortPosition.AboveRightFar,
			EscortPosition.AboveLeftSuperFar,
			EscortPosition.AboveRightSuperFar
		}.AsReadOnly();

		internal static readonly IList<EscortPosition> GroundEscortPositions = new List<EscortPosition>
		{
			EscortPosition.GroundLeft,
			EscortPosition.GroundRight
		}.AsReadOnly();

		private IMyCubeGrid leader;
		private readonly Dictionary<EscortPosition, IMyCubeGrid> escortDic = new Dictionary<EscortPosition, IMyCubeGrid>();
		private readonly QueuedAudioSystem audioSystem;

		internal Convoy(Vector3D destination, NpcGroupState initialState, NpcGroupArrivalObserver arrivalObserver,
			QueuedAudioSystem audioSystem, IMyCubeGrid leader, DateTime groupSpawnTime)
			: base(leader.EntityId, initialState, destination, NpcGroupType.Convoy, groupSpawnTime, arrivalObserver)
		{
			this.audioSystem = audioSystem;
			this.leader = leader;
		}

		internal override void JoinAsEscort(IMyCubeGrid escortApplicant, UnitType unitType, MyPlanet marsPlanet)
		{
			var applicantPosition = escortApplicant.GetPosition();
			var gravity = marsPlanet.GetGravityAtPoint(applicantPosition);
			var closestDistSq = double.MaxValue;
			EscortPosition? closestPosition = null;

			foreach (var escortPosition in GetPotentialEscortPositions(unitType))
			{
				if (!escortDic.ContainsKey(escortPosition))
				{
					var suggestedPosition = GetEscortPositionVector(leader, gravity, escortPosition, GetAdditionalHeightModifier());
					var distSq = Vector3D.DistanceSquared(applicantPosition, suggestedPosition);
					if (distSq < closestDistSq)
					{
						closestDistSq = distSq;
						closestPosition = escortPosition;
					}
				}
			}

			if (closestPosition.HasValue)
			{
				escortDic.Add(closestPosition.Value, escortApplicant);
				//TODO: Update the new recruit on our current state and have them act appropriately?
			}
		}

		internal void ReConnectEscort(IMyCubeGrid escort, NpcGroupSaveData npcGroupSaveData)
		{
			var escortSaveData = GetEscortSaveData(escort, npcGroupSaveData);
			escortDic.Add(escortSaveData.EscortPosition, escort);
		}

		private static EscortSaveData GetEscortSaveData(IMyEntity escort, NpcGroupSaveData npcGroupSaveData)
		{
			foreach (var escortSaveData in npcGroupSaveData.Escorts)
			{
				if (escortSaveData.EscortEntityId == escort.EntityId)
				{
					return escortSaveData;
				}
			}
			throw new ArgumentException("Can't find escort save data: " + escort.EntityId);
		}

		internal override void Update()
		{
			CheckEscortsAlive();
            
			if (!leader.IsControlledByNpcFaction())
			{
				GroupState = NpcGroupState.Disbanding;
				InitiateDisbandProtocols();
			}
			else if ((GroupState == NpcGroupState.Travelling || GroupState == NpcGroupState.InCombat)
                     && Vector3D.DistanceSquared(Destination, leader.GetPosition()) < 200.0 * 200) // increase to 200 to allow for variations in height.
  //                     && Vector3D.Distance(Destination, leader.GetPosition()) < 100.0)
            {
                ArrivalObserver.GroupArrivedIntact();
				audioSystem.PlayAudioRandomChance(0.1, CalAudioClip.ConvoyArrivedSafely);
				GroupState = NpcGroupState.Disbanding;
				InitiateDisbandProtocols();
			    ResetBeaconNames();
			}

			if (GroupState == NpcGroupState.Disbanding)
			{
				AttemptDespawning();
				return;
			}

			if (DuckUtils.IsAnyPlayerNearPosition(leader.GetPosition(),1000) && GroupState == NpcGroupState.Travelling)
			{
				GroupState = NpcGroupState.InCombat;
				InitiateAttackProtocols();
			}

			if (GroupState == NpcGroupState.InCombat)
			{
				var player = DuckUtils.GetNearestPlayerToPosition(leader.GetPosition(), 4000);
				if (player == null)
				{
					GroupState = NpcGroupState.Travelling; // Return to normal, cowardly players have run off or died
					ResetBeaconNames();
					if (escortDic.Count > 0) //todo maybe check if the escorts are actually alive? Dunno if doing this already
					{
						audioSystem.PlayAudio(CalAudioClip.DisengagingFromHostile, CalAudioClip.TargetLost);
					}
					else
					{
						audioSystem.PlayAudio(CalAudioClip.PursuitEvaded, CalAudioClip.SensorsLostTrack);
					}
				}
				else
				{
					SendArmedEscortsNearPosition(player.GetPosition()); // Use same position as when escorting, to avoid collisions
				}
			}

			if (GroupState == NpcGroupState.Travelling)
			{
				foreach (var entry in escortDic)
				{
					SendEscortToGrid(entry.Key, entry.Value, leader);
				}
			}
		}

	    private void ResetBeaconNames()
	    {
	        foreach (var escort in escortDic.Values)
	        {
                //TODO: Add per-faction color

	            escort.SetLightingColors(GcorpBlue);
	            escort.RemoveFromFirstBeaconName(InterceptingBeaconSuffix);
	        }

	        if (leader != null && !leader.Closed)
	        {
                //TODO: Add per-faction color
	            leader.SetLightingColors(GcorpBlue);
	            leader.RemoveFromFirstBeaconName(FleeingBeaconSuffix);
	        }
	    }

	    private void CheckEscortsAlive()
		{
			List<EscortPosition> removedEscorts = null; // Don't initialise unless needed, this is called a lot
			foreach (var entry in escortDic)
			{
                /* TODO: Need another method to determine loss of control/etc
				if (!entry.Value.IsControlledByFaction("GCORP"))
				{
					if (removedEscorts == null)
					{
						removedEscorts = new List<EscortPosition>();
					}
					removedEscorts.Add(entry.Key);
				}
                */
			}

			if (removedEscorts != null)
			{
				foreach (var escort in removedEscorts)
				{
					escortDic.Remove(escort);
				}
			}
		}

		private void AttemptDespawning()
		{
			if (leader != null)
			{
				if (AttemptDespawn(leader))
				{
					leader = null;
				}
			}

			var removedEscorts = new List<EscortPosition>();
			foreach (var entry in escortDic)
			{
				if (AttemptDespawn(entry.Value))
				{
					removedEscorts.Add(entry.Key);
				}
			}

			foreach (var escort in removedEscorts)
			{
				escortDic.Remove(escort);
			}

			if (leader == null && escortDic.Count == 0)
			{
				GroupState = NpcGroupState.Disbanded;
			}
		}

		private void InitiateDisbandProtocols()
		{
			foreach (var entry in escortDic)
			{
				SendEscortToPosition(entry.Key, entry.Value, Destination);
			}
		}

		//TODO adjust height depending on friendly or hostile?
		private void SendArmedEscortsNearPosition(Vector3D targetPosition)
		{
			foreach (var entry in escortDic)
			{
				var targetPos = entry.Value.HasUsableGun() ? targetPosition : Destination;
				SendEscortToPosition(entry.Key, entry.Value, targetPos);
			}
		}

		private void SendEscortToPosition(EscortPosition position, IMyCubeGrid escort, Vector3D targetPosition)
		{
			var slimBlocks = new List<IMySlimBlock>();
			escort.GetBlocks(slimBlocks, b => b.FatBlock is IMyRemoteControl);
			foreach (var slim in slimBlocks)
			{
				var remoteControl = slim.FatBlock as IMyRemoteControl;
				remoteControl.ClearWaypoints();
				var escortPosition = GetEscortPositionVector(targetPosition, remoteControl.GetNaturalGravity(), position,
					GetAdditionalHeightModifier());
				remoteControl.AddWaypoint(escortPosition, "Target");
				remoteControl.SetAutoPilotEnabled(true);
			}
		}

		protected abstract int GetAdditionalHeightModifier();

		private void InitiateAttackProtocols()
		{
			var haveEscorts = false;
			foreach (var escort in escortDic.Values)
			{
				escort.SetLightingColors(Color.Red);
				escort.AppendToFirstBeaconName(InterceptingBeaconSuffix);
				haveEscorts = true;
			}

			if (leader != null)
			{
				leader.SetLightingColors(Color.Orange, 0.8f);
				leader.AppendToFirstBeaconName(FleeingBeaconSuffix);
			}

			if (haveEscorts)
			{
				MyVisualScriptLogicProvider.MusicPlayMusicCategory("HeavyFight");
				audioSystem.PlayAudio(CalAudioClip.EnemyDetectedMovingToIntercept, CalAudioClip.UnknownHostileOnScanners);
			}
			else
			{
				audioSystem.PlayAudio(CalAudioClip.ConvoyUnderThreat);
			}
		}

		private void SendEscortToGrid(EscortPosition position, IMyCubeGrid escort, IMyEntity convoyLeaderGrid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			escort.GetBlocks(slimBlocks, b => b.FatBlock is IMyRemoteControl);
			foreach (var slim in slimBlocks)
			{
                // TODO: Make intelligent escorts
				var remoteControl = slim.FatBlock as IMyRemoteControl;
				remoteControl.ClearWaypoints();
				var escortPosition = GetEscortPositionVector(convoyLeaderGrid, remoteControl.GetTotalGravity(), position,
					GetAdditionalHeightModifier());
				remoteControl.AddWaypoint(escortPosition, "Target");
				remoteControl.SetAutoPilotEnabled(true);
			}
		}

		internal static Vector3D GetEscortPositionVector(IMyEntity convoyLeaderGrid, Vector3D gravity,
			EscortPosition escortPosition, int additionalHeightModifier)
		{
			var deliveryShipPosition = convoyLeaderGrid.GetPosition();
			switch (escortPosition)
			{
				case EscortPosition.GroundLeft:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Left * 20;
				case EscortPosition.GroundRight:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Right * 20;
				case EscortPosition.AboveLeft:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Left * 10-
					       gravity * (2 + additionalHeightModifier);
				case EscortPosition.AboveRight:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Right * 10 -
					       gravity * (2 + additionalHeightModifier);
				case EscortPosition.AboveLeftFar:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Left * 20 -
					       gravity * (3 + additionalHeightModifier);
				case EscortPosition.AboveRightFar:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Right * 20 -
					       gravity * (3 + additionalHeightModifier);
				case EscortPosition.AboveLeftSuperFar:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Left * 30 -
					       gravity * (4 + additionalHeightModifier);
				case EscortPosition.AboveRightSuperFar:
					return deliveryShipPosition + convoyLeaderGrid.WorldMatrix.Right * 30 -
					       gravity * (4 + additionalHeightModifier);
				default:
					throw new ArgumentException("Uncoped for escort position: " + escortPosition);
			}
		}

		//TODO reconcile these two methods? Or stop using formations when attacking/moving to base without the escort.
		//TODO could follow a randomly decided leader, if more than one ship is performing the action.

		private const double Degrees90 = 0.5 * MathHelper.Pi;
		private const double Degrees270 = 1.5 * MathHelper.Pi;

		internal static Vector3D GetEscortPositionVector(Vector3D deliveryShipPosition, Vector3D gravity,
			EscortPosition escortPosition, int additionalHeightModifier)
		{
			switch (escortPosition)
			{
				case EscortPosition.GroundLeft:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees90) * 2;
				case EscortPosition.GroundRight:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees270) * 2;
				case EscortPosition.AboveLeft:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees90) -
					       gravity * (2 + additionalHeightModifier);
				case EscortPosition.AboveRight:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees270) -
					       gravity * (2 + additionalHeightModifier);
				case EscortPosition.AboveLeftFar:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees90) * 2 -
					       gravity * (3 + additionalHeightModifier);
				case EscortPosition.AboveRightFar:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees270) * 2 -
					       gravity * (3 + additionalHeightModifier);
				case EscortPosition.AboveLeftSuperFar:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees90) * 3 -
					       gravity * (4 + additionalHeightModifier);
				case EscortPosition.AboveRightSuperFar:
					return deliveryShipPosition + GetPerpendicularVector(ref gravity, Degrees270) * 3 -
					       gravity * (4 + additionalHeightModifier);
				default:
					throw new ArgumentException("Uncoped for escort position: " + escortPosition);
			}
		}

		internal List<IMyCubeGrid> GetEscorts()
		{
			var escorts = new List<IMyCubeGrid>();
			escorts.AddRange(escortDic.Values);
			return escorts;
		}

		internal override bool IsJoinable(UnitType unitType)
		{
            //TODO: Need another method to determine if not joinable owner
			if ( /*!leader.IsControlledByFaction("GCORP") ||*/ !IsUnitTypeAllowedToJoin(unitType))
			{
				return false;
			}

			foreach (var escortPosition in GetPotentialEscortPositions(unitType))
			{
				if (!escortDic.ContainsKey(escortPosition))
				{
					return true;
				}
			}
			return false;
		}

		private static IEnumerable<EscortPosition> GetPotentialEscortPositions(UnitType unitType)
		{
			IEnumerable<EscortPosition> potentialEscortPositions = unitType == UnitType.Air
				? AirEscortPositions
				: GroundEscortPositions;
			return potentialEscortPositions;
		}

		protected abstract bool IsUnitTypeAllowedToJoin(UnitType unitType);

		internal override Vector3D GetPosition()
		{
			return leader?.GetPosition() ?? Vector3D.Zero;
		}

		private static Vector3D GetPerpendicularVector(ref Vector3D axis, double angle)
		{
			var tangent = Vector3D.CalculatePerpendicularVector(axis);
			Vector3D bitangent;
			Vector3D.Cross(ref axis, ref tangent, out bitangent);
			return Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent;
		}

		internal override List<EscortSaveData> GetEscortSaveData()
		{
			var escortSaveDatas = new List<EscortSaveData>();
			foreach (var entry in escortDic)
			{
				var escortSaveData = new EscortSaveData
				{
					EscortPosition = entry.Key,
					EscortEntityId = entry.Value.EntityId
				};
				escortSaveDatas.Add(escortSaveData);
			}
			return escortSaveDatas;
		}
	}

	public enum EscortPosition
	{
		GroundLeft,
		GroundRight,
		AboveLeft,
		AboveRight,
		AboveLeftFar,
		AboveRightFar,
		AboveLeftSuperFar,
		AboveRightSuperFar
	}

	public class EscortSaveData
	{
		public long EscortEntityId { get; set; }
		public EscortPosition EscortPosition { get; set; }
	}
}