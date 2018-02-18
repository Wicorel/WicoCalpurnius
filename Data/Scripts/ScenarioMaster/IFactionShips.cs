using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Scenario
{
	public interface IFactionShips
	{
		bool HasBackupShips { get; }
		
		PrefabGrid GetBackupShip();

		bool HasConvoys { get; }
		
		void SpawnConvoyTransport(IMyShipController baseToSpawnAt);
		
		void SpawnConvoyEscorts(IMyCubeGrid convoyLeaderGrid, UnitType unitType, MyPlanet planet);
	}
}