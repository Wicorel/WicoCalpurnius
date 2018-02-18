using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario
{
	public static class DuckExtensions
	{
		///  Gets gravity vector at the given point
		public static Vector3D GetGravityAtPoint(this MyPlanet planet, Vector3D vector3D)
		{
			if (planet == null) // Added in due to an NRE in this method somewhere, if it goes off again we'll know why
			{
				throw new InvalidOperationException("Planet passed in is null, can't determine gravity at point");
			}

			var gravityProvider = planet.Components.Get<MyGravityProviderComponent>();

			if (gravityProvider == null) // Added in due to an NRE in this method somewhere, if it goes off again we'll know why
			{
				throw new InvalidOperationException("GravityProvider of planet is null, can't determine gravity at point");
			}

			return new Vector3D(gravityProvider.GetWorldGravity(vector3D));
		}

		///  Closes this grid and all it's subgrids (may include things docked on connectors!)
		public static void CloseAll(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyMotorBase);
			foreach (var slim in slimBlocks)
			{
				var fat = slim.FatBlock as IMyMotorBase;
				var subGrid = fat.TopGrid;
				if (subGrid != null)
				{
					subGrid.Close();
				}
			}
			grid.Close();
		}

		public static void StartTimerBlocks(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyTimerBlock);
			foreach (var slim in slimBlocks)
			{
				var block = slim.FatBlock as IMyTimerBlock;
				block.GetActionWithName("Start").Apply(block);
			}
		}

		/// <summary>
		/// Allahu Akbar!
		/// </summary>
		/// <param name="grid"></param>
		public static void AttemptSelfDestruct(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyWarhead);
			foreach (var slim in slimBlocks)
			{
				var block = slim.FatBlock as IMyWarhead;
				block.GetActionWithName("Detonate").Apply(block);
			}
		}

		public static void SetAllBeaconNames(this IMyCubeGrid grid, string name, float range)
		{
			var slimBlocks = new List<IMySlimBlock>(8);
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyBeacon);
			foreach (var slim in slimBlocks)
			{
				var beacon = (IMyBeacon) slim.FatBlock;
				beacon.CustomName = name;
				beacon.SetValue("Radius", range);
			}
		}

		public static void SetInvulnerable(this IMyCubeGrid grid, bool invulnerable)
		{
			var cubeGrid = grid as MyCubeGrid;
			if (cubeGrid != null)
			{
				cubeGrid.DestructibleBlocks = !invulnerable;
				cubeGrid.Editable = !invulnerable;
			}
		}

		public static void SetAllSubgridsInvulnerable(this IMyCubeGrid grid, bool invulnerable)
		{
			var related = GetRelatedGrids(grid);
			foreach (var myCubeGrid in related)
			{
				SetInvulnerable(myCubeGrid, invulnerable);
			}
		}

		public static bool IsIndestructableOrUneditable(this IMyCubeGrid grid)
		{
			var cubeGrid = grid as MyCubeGrid;
			if (cubeGrid != null)
			{
				if (!cubeGrid.DestructibleBlocks || !cubeGrid.Editable)
				{
					return true;
				}
			}

			return false;
		}

		public static void PlaySoundBlocks(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>(8);
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMySoundBlock);
			foreach (var slim in slimBlocks)
			{
				var soundBlock = (IMySoundBlock) slim.FatBlock;
				soundBlock.ApplyAction("PlaySound");
			}
		}

		public static void AppendToFirstBeaconName(this IMyCubeGrid grid, string toAppend)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyBeacon);
			foreach (var slim in slimBlocks)
			{
				var beacon = slim.FatBlock as IMyBeacon;
				var beaconName = beacon.CustomName;
				if (beaconName.Contains(toAppend))
				{
					return;
				}
				beacon.CustomName = beaconName + toAppend;
			}
		}

		/// <summary>
		/// Removes the string if it is on the first beacon found on the grid.
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="toRemove"></param>
		public static void RemoveFromFirstBeaconName(this IMyCubeGrid grid, string toRemove)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyBeacon);
			foreach (var slim in slimBlocks)
			{
				var beacon = slim.FatBlock as IMyBeacon;
				var beaconName = beacon.CustomName;
				if (beaconName.Contains(toRemove))
				{
					beacon.CustomName = beaconName.Replace(toRemove, "");
				}
				return;
			}
		}

		public static void StopTimerBlocks(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyTimerBlock);
			foreach (var slim in slimBlocks)
			{
				var block = slim.FatBlock as IMyTimerBlock;
				block.GetActionWithName("Stop").Apply(block);
			}
		}

		public static bool HasUsableGun(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyUserControllableGun);
			foreach (var slim in slimBlocks)
			{
				var block = slim.FatBlock as IMyUserControllableGun;
				if (!block.IsFunctional) continue;
				if (block.HasInventory && !block.GetInventory(0).IsItemAt(0)) continue;
				return true;
			}
			return false;
		}

		public static List<IMyUserControllableGun> FindNonTurretWeapons(this IMyCubeGrid grid)
		{
			var weapons = new List<IMyUserControllableGun>();
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyUserControllableGun && !(b.FatBlock is IMyLargeTurretBase));
			foreach (var slim in slimBlocks)
			{
				weapons.Add(slim.FatBlock as IMyUserControllableGun);
			}
			return weapons;
		}

		// Stops all Remote Control blocks on the grid
		public static void StopAutopilot(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyRemoteControl);
			foreach (var slim in slimBlocks)
			{
				var remoteControl = slim.FatBlock as IMyRemoteControl;
				remoteControl.ClearWaypoints();
				remoteControl.SetAutoPilotEnabled(false);
			}
		}

		public static long GetGridControllerFaction(this IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyShipController);
			foreach (var slim in slimBlocks)
			{
				var block = slim.FatBlock as IMyShipController;
				return block.OwnerId;
			}
			return -1;
		}

		public static bool IsOwnedByFaction(this IMyCubeBlock block, string factionTag)
		{
			var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionTag);
			if (faction == null)
			{
				//TODO log this again
				return false;
			}
			return block.OwnerId == faction.FounderId;
		}

		// Changes the color of all lights on the ship
		public static void SetLightingColors(this IMyCubeGrid grid, Color color, float blinkInterval = 0f)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyLightingBlock);
			foreach (var slim in slimBlocks)
			{
				var light = slim.FatBlock as IMyLightingBlock;
				light.SetValueColor("Color", color);
				light.SetValue("Blink Interval", blinkInterval);
			}
		}

		/// <summary>
		///  Sends grid to position if it has a remote control module.
		/// </summary>
		/// <param name="grid"></param> this grid
		/// <param name="targetPosition"></param> target position to go to
		/// <param name="heightModifier"></param> where 1f is 10m above target
		public static void SendToPosition(this IMyCubeGrid grid, Vector3D targetPosition, float heightModifier = 0f)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyRemoteControl);
			foreach (var slim in slimBlocks)
			{
				var remoteControl = slim.FatBlock as IMyRemoteControl;
				remoteControl.ClearWaypoints();
				if (heightModifier > 0)
				{
					targetPosition = targetPosition + remoteControl.GetNaturalGravity() * -heightModifier;
				}
				remoteControl.AddWaypoint(targetPosition, "Target");
				remoteControl.SetAutoPilotEnabled(true);
				break; // We have to break because of the if statement, otherwise height will keep increasing!
			}
		}

		public static MyInventory GetContainerInventoryOfName(this IMyCubeGrid grid, string name)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyCargoContainer);
			foreach (var slim in slimBlocks)
			{
				var cargoContainer = (IMyCargoContainer) slim.FatBlock;
				var entity = cargoContainer as MyEntity;
				if (entity != null && entity.HasInventory && cargoContainer.CustomName.Contains(name))
				{
					var inventory = entity.GetInventoryBase() as MyInventory;
					if (inventory != null)
					{
						return inventory;
					}
				}
			}

			throw new InvalidOperationException("Can't find container matching name: " + name);
		}

		public static List<T> GetTerminalBlocksOfTypeInSubgrids<T>(this IMyCubeGrid grid) where T : IMyTerminalBlock
		{
			var list = new List<T>();
			foreach (var relatedGrid in GetRelatedGrids(grid))
			{
				CollectTerminalBlocksOfType(relatedGrid, null, list);
			}
			return list;
		}

		/// <summary>
		/// Method by Equinox to find all related (sub-grids) of this grid.
		/// </summary>
		/// <param name="grid"></param>
		/// <returns>The Set of all subgrids of this grid, including itself</returns>
		public static IEnumerable<IMyCubeGrid> GetRelatedGrids(this IMyCubeGrid grid)
		{
			var relatedGrids = new HashSet<IMyCubeGrid>();
			GetRelatedGrids(grid, relatedGrids);
			return relatedGrids;
		}

		public static IEnumerable<IMyCubeGrid> GetRelatedGrids(this IMyCubeGrid grid, HashSet<IMyCubeGrid> relatedGrids)
		{
			relatedGrids.Add(grid);
			var scanRelated = new Queue<IMyCubeGrid>();
			scanRelated.Enqueue(grid);
			while (scanRelated.Count > 0)
				scanRelated.Dequeue()
					.GetBlocks(null, x =>
						{
							var child = (x?.FatBlock as IMyMechanicalConnectionBlock)?.TopGrid
							            ?? (x?.FatBlock as IMyAttachableTopBlock)?.Base?.CubeGrid;
							if (child != null && relatedGrids.Add(child))
								scanRelated.Enqueue(child);
							return false;
						}
					);

			return relatedGrids;
		}


		public static T GetTerminalBlockMatchingName<T>(this IMyCubeGrid grid, string name) where T : IMyTerminalBlock
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is T);
			foreach (var slim in slimBlocks)
			{
				var terminalBlock = (T) slim.FatBlock;
				if (terminalBlock.CustomName.Contains(name))
				{
					return terminalBlock;
				}
			}

			throw new InvalidOperationException("Can't find terminal block matching name: " + name);
		}

		public static List<T> GetTerminalBlocksOfType<T>(this IMyCubeGrid grid) where T : IMyTerminalBlock
		{
			return GetTerminalBlocksOfType<T>(grid, null);
		}

		public static List<T> GetTerminalBlocksOfType<T>(this IMyCubeGrid grid, string name) where T : IMyTerminalBlock
		{
			var list = new List<T>();
			CollectTerminalBlocksOfType(grid, name, list);
			return list;
		}

		private static void CollectTerminalBlocksOfType<T>(IMyCubeGrid grid, string name, ICollection<T> list)
			where T : IMyTerminalBlock
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is T);
			foreach (var slim in slimBlocks)
			{
				var terminalBlock = (T) slim.FatBlock;
				if (name == null || terminalBlock.CustomName.Contains(name))
				{
					list.Add(terminalBlock);
				}
			}
		}

		public static Dictionary<MyDefinitionId, MyFixedPoint> CalculateIngotCost(this IMyCubeGrid grid)
		{
			var ingots = new Dictionary<MyDefinitionId, MyFixedPoint>(MyDefinitionId.Comparer);
			var components = CalculateNewComponentCost(grid); //TODO swap for current cost when that method is working
			foreach (var entry in components)
			{
				var blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(entry.Key);
				foreach (var ingot in blueprint.Prerequisites)
				{
					var amountToAdd = ingot.Amount * entry.Value;
					MyFixedPoint currentTotal;
					if (ingots.TryGetValue(ingot.Id, out currentTotal))
						ingots[ingot.Id] = currentTotal + amountToAdd;
					else
						ingots.Add(ingot.Id, amountToAdd);
				}
			}
			return ingots;
		}

		/// <summary>
		/// Finds the cost of blocks as if they were new
		/// </summary>
		/// <param name="grid"></param>
		/// <returns></returns>
		public static Dictionary<MyDefinitionId, int> CalculateNewComponentCost(this IMyCubeGrid grid)
		{
			var res = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
			grid.GetBlocks(new List<IMySlimBlock>(), b =>
			{
				foreach (var comp in (b.BlockDefinition as MyCubeBlockDefinition).Components)
				{
					var id = comp.Definition.Id;
					int count;
					if (res.TryGetValue(id, out count))
						res[id] = count + comp.Count;
					else
						res[id] = comp.Count;
				}
				return false;
			});
			return res;
		}

		/// <summary>
		/// Finds the cost of blocks as they are in their current state
		/// </summary>
		/// <param name="grid"></param>
		/// <returns></returns>
		public static Dictionary<MyDefinitionId, int> CalculateComponentCost(this IMyCubeGrid grid)
		{
			var res = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
			grid.GetBlocks(new List<IMySlimBlock>(), block =>
			{
				var myCubeBlockDefinition = block.BlockDefinition as MyCubeBlockDefinition;
				if (myCubeBlockDefinition == null) return false;
				foreach (var comp in myCubeBlockDefinition.Components.Select(s => s.Definition.Id)
					.Distinct(MyDefinitionId.Comparer))
				{
					var cnt = block.GetConstructionStockpileItemAmount(comp);
//TODO could try ClearConstructionStockpile as it takes things into an inventory or FullyDismount which seems better
					int count;
					if (res.TryGetValue(comp, out count))
						res[comp] = count + cnt;
					else
						res[comp] = cnt;
				}
				return false;
			});
			return res;
		}

		public static void MoveAllContentsTo(this MyInventory fromInventory, MyInventoryBase toInventory)
		{
			foreach (var item in fromInventory.GetItems())
			{
				toInventory.AddItems(item.Amount, item.Content);
			}
			fromInventory.Clear();
		}

		public static bool IsControlledByFaction(this IMyCubeGrid grid, string factionTag)
		{
			return grid != null && !grid.Closed && IsControlledBy(grid, factionTag);
		}

		public static bool IsControlledByNpcFaction(this IMyCubeGrid grid)
		{
			return true;
			//TODO determine the majority holder (if any) using	Faction.IsKnownNpcFaction()
		}

		private static bool IsControlledBy(IMyCubeGrid grid, string factionTag)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyShipController);
			foreach (var slim in slimBlocks)
			{
				var block = slim.FatBlock as IMyShipController;
				if (IsControlledByFaction(block, factionTag))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsControlledByFaction(this IMyCubeBlock block, string factionTag)
		{
			return block != null && !block.Closed && block.IsWorking && IsControlledBy(block, factionTag);
		}

		private static bool IsControlledBy(this IMyCubeBlock block, string factionTag)
		{
			var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionTag);
			if (faction == null)
			{
				return false;
			}
			return block.OwnerId == faction.FounderId;
		}

		public static void MakePeaceWithFaction(this IFaction faction1, IFaction faction2)
		{
			MyAPIGateway.Session.Factions.SendPeaceRequest(faction1.FactionId, faction2.FactionId);
			MyAPIGateway.Session.Factions.AcceptPeace(faction2.FactionId, faction1.FactionId);
		}
		
		public static void JoinCurrentPlayerToFaction(this IFaction faction)
		{
			var player = MyAPIGateway.Session.Player;
			if (player == null)
			{
				return;
			}

			MyAPIGateway.Session.Factions.SendJoinRequest(faction.FactionId, player.IdentityId);
			MyAPIGateway.Session.Factions.AcceptJoin(faction.FactionId, player.IdentityId);
		}

		public static bool ControlsBlock(this IFaction faction, IMyCubeBlock block)
		{
			return true;
			//TODO reactivate this code
			//return block != null && !block.Closed && block.IsWorking && block.OwnerId == faction.MyFaction.FounderId;
		}
	}
}