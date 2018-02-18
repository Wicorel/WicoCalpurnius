using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;

namespace Scenario.Calpurnius
{
	internal class ConvoySpawner : ModSystemUpdatable
	{
		public static readonly bool DebugConvoys = false;
		private readonly QueuedAudioSystem audioSystem;
		private readonly List<IMyRemoteControl> spawningBases = new List<IMyRemoteControl>();
		private readonly Random random = new Random();
		private DateTime nextSpawnTime;
		private readonly List<IFaction> factions = new List<IFaction>();

		internal ConvoySpawner(QueuedAudioSystem audioSystem)
		{
			this.audioSystem = audioSystem;
		}

		/// <summary>
		/// Adds a faction to the list of those that are eligible for spawning convoys
		/// </summary>
		/// <param name="faction"></param>
		internal void AddFaction(IFaction faction)
		{
			factions.Add(faction);
		}

		internal long GetNextSpawnTimeForSaving()
		{
			return nextSpawnTime.ToBinary();
		}

		private const string DeliverySpawnerPrefix = "DELIVERY_SPAWNER";

		public override void GridInitialising(IMyCubeGrid grid)
		{
			if (!grid.IsStatic)
			{
				return;
			}
			
			foreach (var remoteControl in grid.GetTerminalBlocksOfType<IMyRemoteControl>())
			{
                // TODO: need another method to determine ownership (or just ignore it)
				if ( /*remoteControl.IsControlledByFaction("GCORP") && */
				    remoteControl.CustomName.Contains(DeliverySpawnerPrefix)) // Finds both ground and air spawners
				{
					spawningBases.Add(remoteControl);
				}
			}
		}

		internal void RestoreSpawnTimeFromSave(long savedTime)
		{
			nextSpawnTime = DebugConvoys ?  MyAPIGateway.Session.GameDateTime 
				: DateTime.FromBinary(savedTime);
		}

		public override void Update300()
		{
			if (nextSpawnTime.Equals(DateTime.MinValue)) // New game before any save
			{
				var delayUntilFirstConvoy = DebugConvoys ? new TimeSpan(0, 0, 10) : new TimeSpan(0, 45, 0);
				nextSpawnTime = MyAPIGateway.Session.GameDateTime + delayUntilFirstConvoy;
			}
			else
			{
				if (MyAPIGateway.Session.GameDateTime >= nextSpawnTime)
				{
					SpawnConvoy();
					ResetTimeUntilNextConvoy();
				}
			}
		}

		private void ResetTimeUntilNextConvoy()
		{
			var delayUntilNextConvoy = DebugConvoys ? new TimeSpan(0, 0, 30) : new TimeSpan(0, random.Next(12, 18), 0);
			nextSpawnTime = MyAPIGateway.Session.GameDateTime + delayUntilNextConvoy;
		}

		internal void SpawnConvoy()
		{
			var baseToSpawnAt = ChooseSpawningBase();
			if (baseToSpawnAt != null)
			{
				CalFactions.Gcorp.Ships.SpawnConvoyTransport(baseToSpawnAt);//TODO make this work for any faction
				audioSystem.PlayAudioRandomChance(0.1, CalAudioClip.ConvoyDispatched1, CalAudioClip.ConvoyDispatched2,
					CalAudioClip.ConvoyDispatched3);
			}
		}

		private IMyRemoteControl ChooseSpawningBase()
		{
			if (spawningBases.Count == 0)
			{
				return null;
			}

			var players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);

			var playerPositions = new List<Vector3D>();
			foreach (var player in players)
			{
				var controlled = player.Controller.ControlledEntity;
				if (controlled == null) continue;
				playerPositions.Add(controlled.Entity.GetPosition());
			}

			if (playerPositions.Count == 0)
			{
				return null;
			}

			var baseDistances = new Dictionary<double, IMyRemoteControl>();
			var positionToSpawnNearTo = playerPositions.GetRandomItemFromList();
			foreach (var spawningBase in spawningBases)
			{
                // TODO: need a reference to the whole base grid to properly determine ownership
				if (!spawningBase.IsControlledByFaction("GCORP"))
				{
//					continue;
				}

				var distSq = Vector3D.DistanceSquared(spawningBase.PositionComp.GetPosition(), positionToSpawnNearTo);
				baseDistances.Add(distSq, spawningBase);
			}

			var sortedDistances = baseDistances.Keys.ToList();
			sortedDistances.Sort();

			foreach (var distance in sortedDistances)
			{
				var d4 = random.Next(0, 4);
				// 50% of spawning at closest, then next closest and so on. But don't spawn within 1km of a player, as they will see it
				if (d4 > 1 && NoPlayerNearby(baseDistances[distance]))
				{
					return baseDistances[distance];
				}
			}
			return null; // Small chance of nothing spawning at all
		}

		private static bool NoPlayerNearby(VRage.Game.ModAPI.Ingame.IMyEntity baseRc)
		{
			return DebugConvoys || !DuckUtils.IsAnyPlayerNearPosition(baseRc.GetPosition(), 1000);
		}
	}
}