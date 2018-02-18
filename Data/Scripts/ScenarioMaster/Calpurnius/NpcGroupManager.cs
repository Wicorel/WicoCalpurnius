using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	public class NpcGroupManager : ModSystemUpdatable
	{
        // TODO: Only allows one destination per group.  Should expand to allow multiple destinations for different types.  With different random lists for each 'type'.
        // TODO: Add space destinations
		private Vector3D airConvoyDestinationPosition;
		private Vector3D groundConvoyDestinationPosition;

		private readonly HeatSystem heatSystem;
		private readonly QueuedAudioSystem audioSystem;
		private readonly BaseManager baseManager;
		private readonly Random random = new Random();
		private readonly List<NpcGroup> npcGroups = new List<NpcGroup>();
		private readonly TimeSpan convoyExpiryTime = new TimeSpan(1, 10, 0);
		private readonly Queue<IMyCubeGrid> unitialisedNewGrids = new Queue<IMyCubeGrid>();

		private readonly Dictionary<long, NpcGroupSaveData> restoredNpcGroupData = new Dictionary<long, NpcGroupSaveData>();
		private readonly Dictionary<long, long> restoredEscortAssignments = new Dictionary<long, long>();
		readonly Dictionary<long, Convoy> restoredConvoys = new Dictionary<long, Convoy>();
		readonly List<IMyCubeGrid> possibleEscorts = new List<IMyCubeGrid>();

		internal NpcGroupManager(HeatSystem heatSystem, QueuedAudioSystem audioSystem, BaseManager baseManager)
		{
			this.heatSystem = heatSystem;
			this.audioSystem = audioSystem;
			this.baseManager = baseManager;
			MyAPIGateway.Entities.OnEntityAdd += NewEntityEvent;
		}

		// We have to listen for new entities because they are not returned by the spawning code.
		// The faction of the blocks also isn't set by this point, neither is the beacon name, so there's nothing much 
		// we can do other than queue it up to check next update
		internal void NewEntityEvent(IMyEntity ent)
		{
			if (!(ent is IMyCubeGrid)) return;
			var grid = (IMyCubeGrid) ent;
			unitialisedNewGrids.Enqueue(grid);
		}

		public override void GridInitialising(IMyCubeGrid grid)
		{
			if (!grid.IsControlledByNpcFaction())
			{
				return;
			}

			if (grid.IsStatic)
			{
				OfferPotentialDestination(grid);
			}
			else
			{
				OfferPotentialNpcShip(grid);
			}
		}

		public override void AllGridsInitialised()
		{
			// Loop through again to find escorts of the npcGroups we made above
			// We can't do this until the end when all group leaders have been found!
			foreach (var grid in possibleEscorts)
			{
				long deliveryShipId;
				if (restoredEscortAssignments.TryGetValue(grid.EntityId, out deliveryShipId))
				{
					Convoy convoy;
					NpcGroupSaveData npcGroupSaveData;
					if (restoredConvoys.TryGetValue(deliveryShipId, out convoy) &&
					    restoredNpcGroupData.TryGetValue(deliveryShipId, out npcGroupSaveData))
					{
						convoy.ReConnectEscort(grid, npcGroupSaveData);
					}
				}
			}

			// We don't need these afer finishing restores
			restoredNpcGroupData.Clear();
			restoredEscortAssignments.Clear();
		}

		private void OfferPotentialDestination(IMyCubeGrid grid)
		{
			foreach (var remoteControl in grid.GetTerminalBlocksOfType<IMyRemoteControl>())
			{
				// TODO: need a reference to the whole base grid to properly determine ownership
				if (!remoteControl.IsControlledByFaction("GCORP"))
				{
//					continue;
				}

                // TODO: add multiple source and desitinations. also should check CustomData
				if (remoteControl.CustomName.Contains("AIR_DELIVERY_DESTINATION"))
				{
					// If the destination has not yet been setup (ie restored from a save), do so now
					if (Vector3D.IsZero(airConvoyDestinationPosition))
					{
						airConvoyDestinationPosition = remoteControl.GetPosition() + remoteControl.GetNaturalGravity() *
						                               -23f;
					}
				}
				else if (remoteControl.CustomName.Contains("GROUND_DELIVERY_DESTINATION"))
				{
					//basesToInit.Add(remoteControl); // Is in the same place as the air destination, so disabled this
					// If the destination has not yet been setup (ie restored from a save), do so now
					if (Vector3D.IsZero(groundConvoyDestinationPosition))
					{
						groundConvoyDestinationPosition = remoteControl.GetPosition() + remoteControl.GetNaturalGravity() * -0.3f;
						// 3M above for Ground Destination
					}
				}
			}
		}
		
		private void OfferPotentialNpcShip(IMyCubeGrid grid)
		{
			NpcGroupSaveData npcGroupSaveData;
			if (restoredNpcGroupData.TryGetValue(grid.EntityId, out npcGroupSaveData))
			{
				if (npcGroupSaveData.NpcGroupType == NpcGroupType.Backup)
				{
					npcGroups.Add(new BackupGroup(npcGroupSaveData.State, npcGroupSaveData.GroupDestination,
						grid, heatSystem, audioSystem, DateTime.FromBinary(npcGroupSaveData.SpawnTime)));
				}
                // TODO: Allow other types such as "Police", "Gypsy", etc.
				else // Must be convoy
				{
					restoredConvoys.Add(grid.EntityId,
						RegisterConvoy(grid, npcGroupSaveData.State, npcGroupSaveData.LeaderUnitType,
							npcGroupSaveData.GroupDestination, DateTime.FromBinary(npcGroupSaveData.SpawnTime)));
				}
			}
			else
			{
				possibleEscorts.Add(grid);
			}
		}

		internal NpcGroup FindNearestJoinableNpcGroup(Vector3D position, UnitType unitType)
		{
			NpcGroup nearestGroup = null;
			var closestDistance = double.MaxValue;
			foreach (var group in npcGroups)
			{
				if (group.IsJoinable(unitType))
				{
					var distSquared = Vector3D.DistanceSquared(group.GetPosition(), position);
					if (distSquared < closestDistance)
					{
						closestDistance = distSquared;
						nearestGroup = group;
					}
				}
			}
			return nearestGroup;
		}

		internal List<NpcGroupSaveData> GetSaveData()
		{
			var npcGroupSaveDatas = new List<NpcGroupSaveData>();
			foreach (var group in npcGroups)
			{
				npcGroupSaveDatas.Add(group.GetSaveData());
			}
			return npcGroupSaveDatas;
		}

		internal void LoadSaveData(List<NpcGroupSaveData> saveDatas)
		{
			foreach (var convoySaveData in saveDatas) // Easier for later if these are in a map!
			{
				restoredNpcGroupData.Add(convoySaveData.GroupId, convoySaveData);
				foreach (var escortSaveData in convoySaveData.Escorts)
				{
					restoredEscortAssignments.Add(escortSaveData.EscortEntityId, convoySaveData.GroupId);
				}
			}
		}

		public override void Update60()
		{
			UpdateGroups(); // Disband or update existing, active groups
			GiveOrdersToUnassignedShips(); // Find ships that have been spawned but not set in a direction / mission
		}

		private void UpdateGroups()
		{
			foreach (var group in npcGroups.Reverse<NpcGroup>()) // Go backwards so we can remove part way through if needed
			{
				if (group.IsDisbanded())
				{
					npcGroups.Remove(group);
				}
				else
				{
					@group.Update();
				}
			}
		}

		private void GiveOrdersToUnassignedShips()
		{
			while (unitialisedNewGrids.Count > 0)
			{
				var grid = unitialisedNewGrids.Dequeue();
				if (!grid.IsControlledByNpcFaction())
				{
					continue;
				}

				var roleAndUnitType = PrefabGrid.GetRoleAndUnitType(grid);
				if (roleAndUnitType == null)
				{
					continue;
				}

				var unitType = roleAndUnitType.Value.UnitType;

				switch (roleAndUnitType.Value.UnitRole)
				{
					case UnitRole.Delivery:
						var cargoType = CargoType.GenerateRandomCargo(random);
						LoadCargo(grid, cargoType);
						grid.SetAllBeaconNames("T" + random.Next(10000, 99999) + " - " + cargoType.GetDisplayName() + " Shipment",
							20000f);
						var destination = unitType == UnitType.Air ? airConvoyDestinationPosition : groundConvoyDestinationPosition;
						SetDestination(grid, destination);
						RegisterConvoy(grid, NpcGroupState.Travelling, unitType, destination, MyAPIGateway.Session.GameDateTime);

						var planet = DuckUtils.FindPlanetInGravity(grid.GetPosition());
						if (planet != null)
						{
							CalFactions.Gcorp.Ships.SpawnConvoyEscorts(grid, unitType, planet);
						}
						break;
					case UnitRole.Escort:
						var group = FindNearestJoinableNpcGroup(grid.GetPosition(), unitType);
						if (group == null)
						{
							ModLog.Error("Escort ship spawned but can't find a group to join!");
							grid.CloseAll();
						}
						else
						{
							grid.SetAllBeaconNames("E" + random.Next(10000, 99999) + " - Convoy Escort", 20000f);
							var nearestPlanet = DuckUtils.FindPlanetInGravity(grid.GetPosition());
							if (nearestPlanet != null)
							{
								group.JoinAsEscort(grid, unitType, nearestPlanet);
							}
						}
						break;
					case UnitRole.Backup:
						var gCorpBase = baseManager.FindBaseWantingBackup();
						if (gCorpBase == null)
						{
							ModLog.Error("Backup ship spawned but can't find the base that asked for it!");
							grid.CloseAll();
							break;
						}
						var backupPosition = gCorpBase.GetBackupPosition();
						grid.SendToPosition(backupPosition);
						grid.SetAllBeaconNames("M" + random.Next(10000, 99999) + " Investigating Backup Call", 20000f);
						var backupGroup = new BackupGroup(NpcGroupState.Travelling, backupPosition, grid,
							heatSystem, audioSystem, MyAPIGateway.Session.GameDateTime);
						//damageSensor.RegisterDamageObserver(grid.EntityId, backupGroup);
						npcGroups.Add(backupGroup);
						break;
					default:
						continue;
				}
			}
		}

		private Convoy RegisterConvoy(IMyCubeGrid leaderGrid, NpcGroupState npcGroupState, UnitType unitType,
			Vector3D destination, DateTime spawnTime)
		{
			leaderGrid.StartTimerBlocks();

			Convoy convoy;
			if (unitType == UnitType.Air)
			{
				convoy = new ConvoyAir(destination, npcGroupState, heatSystem, audioSystem, leaderGrid, spawnTime);
			}
			else
			{
				convoy = new ConvoyGround(destination, npcGroupState, heatSystem, audioSystem, leaderGrid, spawnTime);
			}
			npcGroups.Add(convoy);
			return convoy;
		}

		private static void LoadCargo(IMyCubeGrid deliveryShip, CargoType cargo)
		{
			var slimBlocks = new List<IMySlimBlock>();
			deliveryShip.GetBlocks(slimBlocks, b => b.FatBlock is IMyCargoContainer);
			foreach (var slim in slimBlocks)
			{
				var cargoContainer = (IMyCargoContainer) slim.FatBlock;
				var entity = cargoContainer as MyEntity;
				if (entity.HasInventory)
				{
					var inventory = entity.GetInventoryBase() as MyInventory;
					inventory?.AddItems(cargo.AmountPerCargoContainer, cargo.GetObjectBuilder());
				}
			}
		}

		private static void SetDestination(IMyCubeGrid grid, Vector3D destination)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyGyro);
			foreach (var slim in slimBlocks)
			{
				var block = slim.FatBlock as IMyGyro;
				// using STARTED_DELIVERY as a way to find our grids! For autopilot it's a harmless comment.
				block.CustomName = "NAV: C STARTED_DELIVERY; S 10; D 80 ; W " + destination.X + ":" + destination.Y +
				                   ":" + destination.Z;
				break; // We only need to set up one gyro, it may have more
			}
		}

		public override void Update1200()
		{
			var currentTime = MyAPIGateway.Session.GameDateTime;
			foreach (var npcGroup in npcGroups)
			{
				if (npcGroup.GroupSpawnTime + convoyExpiryTime < currentTime)
				{
					ModLog.DebugError("NPC group expired! Ordering to disband for passing time limit of "
					                  + convoyExpiryTime, npcGroup.GetPosition());
					npcGroup.Expire();
				}
			}
		}

		public override void Close()
		{
			MyAPIGateway.Entities.OnEntityAdd -= NewEntityEvent;
		}
	}
}