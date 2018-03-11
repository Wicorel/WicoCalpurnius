using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	public class CalFactions
	{
		private static readonly Dictionary<long, IFaction> AllFactions = new Dictionary<long, IFaction>();

		public static readonly IFaction Player = new PlayerFaction();
		public static readonly IFaction Gcorp = new GCorpFaction();
		public static readonly IFaction Miki = new MikiFaction(); //TODO this could be a generic merchant faction?
		public static readonly IFaction None = new NoneFaction();

		internal static void Init()
		{
            // Make scenario changes here.
			// Register all factions here so we can look them up 
			AllFactions.Add(Player.FactionId, Player);
			AllFactions.Add(Gcorp.FactionId, Gcorp);
			AllFactions.Add(Miki.FactionId, Miki);
			
			// Add faction relationships here, e.g.
			Miki.MakePeaceWithFaction(Player);
			Miki.MakePeaceWithFaction(Gcorp);
		}
		
		/// <summary>
		/// Does not detect built in npc factions such as space pirates and spiders, only those that are unique 
		/// to the scenario and assumed to be managed by it.
		/// </summary>
		/// <param name="factionId"></param>
		/// <returns></returns>
		public static bool IsKnownNpcFaction(long factionId)
		{
			IFaction faction;
			return AllFactions.TryGetValue(factionId, out faction) && faction.IsNpc;
		}
	}


    // TODO: Move these into factions-specific files..

	internal class PlayerFaction : AbstractFaction
	{
		public PlayerFaction() : base("PLAYER", "Mabel", MyFontEnum.Green, Color.White, false, new NoFactionShips()) //TODO change the tag of this faction
		{
		} 		
	}

	internal class GCorpFaction : AbstractFaction
	{

		public GCorpFaction() : base("GCORP", "GCorp Transmission", MyFontEnum.Red, new Color(55, 69, 255), true, new GCorpShips())
		{
		}
	}

	internal class GCorpShips : IFactionShips
	{		
		private readonly HeatSystem heatSystem = new HeatSystem(-7);
		
		public bool HasBackupShips { get; } = true;
		
		public PrefabGrid GetBackupShip()
		{
			return CalPrefabFactory.GetBackup(heatSystem.GenerateBackupShipSize());
		}

		public bool HasConvoys { get; } = true;
		
		public void SpawnConvoyTransport(IMyShipController baseToSpawnAt)
		{
			var factionId = baseToSpawnAt.OwnerId;
			var spawnerPosition = baseToSpawnAt.GetPosition();
			var gravity = baseToSpawnAt.GetNaturalGravity();
			var unitType = baseToSpawnAt.CustomName.Contains("GROUND") ? UnitType.Ground : UnitType.Air;
			var cargoSize = heatSystem.GenerateCargoShipSize();

			if (unitType == UnitType.Air)
			{
				var positionToSpawn = spawnerPosition + gravity * -5f + baseToSpawnAt.WorldMatrix.Forward * 30;
				var transportPrefab = CalPrefabFactory.GetAirTransport(cargoSize);
				DuckUtils.SpawnInGravity(positionToSpawn, gravity, factionId, transportPrefab.PrefabName,
					transportPrefab.InitialBeaconName,
					Vector3D.Normalize(baseToSpawnAt.WorldMatrix.Forward));
			}
			else
			{
				var positionToSpawn = spawnerPosition + gravity * -1f + baseToSpawnAt.WorldMatrix.Forward * 35;
				var transportPrefab = CalPrefabFactory.GetGroundTransport(cargoSize);
				DuckUtils.SpawnInGravity(positionToSpawn, gravity, factionId, transportPrefab.PrefabName,
					transportPrefab.InitialBeaconName,
					Vector3D.Normalize(baseToSpawnAt.WorldMatrix.Forward));
			}
		}

		public void SpawnConvoyEscorts(IMyCubeGrid convoyLeaderGrid, UnitType unitType, MyPlanet planet)
		{
			var gravity = planet.GetGravityAtPoint(convoyLeaderGrid.GetPosition());
			var escortsNeededToSpawn = heatSystem.GenerateEscortSpecs();
			if (unitType == UnitType.Air)
			{
				SpawnAirEscorts(escortsNeededToSpawn, gravity, convoyLeaderGrid);
			}
			else
			{
				SpawnLandEscorts(escortsNeededToSpawn, gravity, convoyLeaderGrid);
				SpawnAirEscorts(escortsNeededToSpawn, gravity, convoyLeaderGrid);
			}
		}
			
		private void SpawnLandEscorts(ICollection<ShipSize> escortsNeededToSpawn, Vector3D gravity,
			IMyCubeGrid convoyLeaderGrid)
		{
			SpawnEscorts(escortsNeededToSpawn, gravity, Convoy.GroundEscortPositions, UnitType.Ground, convoyLeaderGrid);
		}

		private void SpawnAirEscorts(ICollection<ShipSize> escortsNeededToSpawn, Vector3D gravity,
			IMyCubeGrid convoyLeaderGrid)
		{
			SpawnEscorts(escortsNeededToSpawn, gravity, Convoy.AirEscortPositions, UnitType.Air, convoyLeaderGrid);
		}

		private void SpawnEscorts(ICollection<ShipSize> escortsNeededToSpawn, Vector3D gravity,
			IList<EscortPosition> allowedPositions, UnitType unitType, IMyCubeGrid convoyLeaderGrid)
		{
			var positionIndex = 0;
			foreach (var escort in escortsNeededToSpawn.Reverse())
			{
				SpawnEscortGrid(CalPrefabFactory.GetEscort(unitType, escort), gravity,
					allowedPositions[positionIndex], unitType, convoyLeaderGrid);
				positionIndex++;
				escortsNeededToSpawn.Remove(escort);
				if (positionIndex == allowedPositions.Count)
				{
					break; // Can't spawn any more of this type, all positions full
				}
			}
		}
		
		private void SpawnEscortGrid(PrefabGrid prefabGrid, Vector3D naturalGravity, EscortPosition escortPosition,
			UnitType convoyUnitType, IMyCubeGrid convoyLeaderGrid)
		{
			var positionToSpawn = Convoy.GetEscortPositionVector(convoyLeaderGrid, naturalGravity, escortPosition,
				GetAdditionalHeightModifier(convoyUnitType));
			var factionId = convoyLeaderGrid.GetGridControllerFaction();
			var forwards = convoyLeaderGrid.WorldMatrix.Forward;
			DuckUtils.SpawnInGravity(positionToSpawn, naturalGravity, factionId, prefabGrid.PrefabName,
				prefabGrid.InitialBeaconName, forwards);
		}
		
		private int GetAdditionalHeightModifier(UnitType convoyUnitType)
		{
			return convoyUnitType == UnitType.Air ? ConvoyAir.AdditionalHeightModifier : ConvoyGround.AdditionalHeightModifier;
		}
	}

	internal class MikiFaction : AbstractFaction
	{
		public MikiFaction() : base("MIKI", "Miki", MyFontEnum.White, Color.White, true, new NoFactionShips())
		{
		}
	}

	internal class NoneFaction : IFaction
	{
		public IMyFaction MyFaction
		{
			get { throw new Exception("None Faction Does not support being used in this way"); }
		}

		public long FactionId => -666; // Just something that will never match checks
		public string SubtitlesSpeaker => "Voice";
		public MyFontEnum SubtitlesFont => MyFontEnum.BuildInfo;
		public Color LightsColor => Color.White;
		public bool IsNpc => false;
		public IFactionShips Ships { get;  } = new NoFactionShips();
	}
}