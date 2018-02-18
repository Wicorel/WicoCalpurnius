using System;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Scenario
{
	public class NoFactionShips : IFactionShips
	{
		public bool HasBackupShips { get; } = false;
		public PrefabGrid GetBackupShip()
		{
			throw new Exception("Not supported");
		}

		public bool HasConvoys { get; } = false;
		public void SpawnConvoyTransport(IMyShipController baseToSpawnAt)
		{
			throw new Exception("Not supported");
		}

		public void SpawnConvoyEscorts(IMyCubeGrid convoyLeaderGrid, UnitType unitType, MyPlanet planet)
		{
			throw new Exception("Not supported");
		}
	}
}