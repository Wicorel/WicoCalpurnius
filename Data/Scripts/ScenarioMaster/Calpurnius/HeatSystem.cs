using System;
using System.Collections.Generic;

namespace Scenario.Calpurnius
{
	internal class HeatSystem : NpcGroupArrivalObserver
	{
		private static readonly IList<ShipSize> EmptyList = new List<ShipSize>().AsReadOnly();
		private readonly Random random = new Random();
		public int HeatLevel { get; set; }

		internal HeatSystem(int initialHeat)
		{
			HeatLevel = initialHeat;
		}

		internal ShipSize GenerateCargoShipSize()
		{
			NewShipGroupLaunched();

			if (HeatLevel < 10) // Very early game, make them all small
			{
				return ShipSize.Small;
			}

			var d100 = random.Next(0, 100); // Random between 0 and 99

			if (HeatLevel < 50) // Mediums start to appear after 10
			{
				if (HeatLevel > 97) // Tiny 2% chance of giant delivery!
				{
					return ShipSize.Large;
				}

				return d100 < HeatLevel ? ShipSize.Medium : ShipSize.Small;
			}

			if (HeatLevel < 100)  // Larges start to appear after 50
			{
				if (d100 < 10)
				{
					return ShipSize.Small;
				}
				return d100 < 70 ? ShipSize.Medium : ShipSize.Large;
			}

			// End game - player killed 100+ convoys!
			if (d100 < 10)
			{
				return ShipSize.Small;
			}
			return d100 < 40 ? ShipSize.Medium : ShipSize.Large;
		}

		// Contract: Should return a list of SMALLEST SIZE FIRST. The list should be mutable if it is not the empty list.
		internal IList<ShipSize> GenerateEscortSpecs()
		{
			var d100 = random.Next(0, 100); // Random between 0 and 99
			if (HeatLevel < 10)
			{
				return d100 > 90 ? new List<ShipSize> {ShipSize.Small} : EmptyList;
			}

			if (HeatLevel < 20)
			{
				if (d100 > 90)
				{
					return new List<ShipSize> {ShipSize.Small, ShipSize.Small};
				}
				return d100 > 70 ? new List<ShipSize> {ShipSize.Small} : EmptyList;
			}

			if (HeatLevel < 40)
			{
				if (d100 > 90)
				{
					return new List<ShipSize> {ShipSize.Small, ShipSize.Small, ShipSize.Medium};
				}
				if (d100 > 80)
				{
					return new List<ShipSize> {ShipSize.Small, ShipSize.Small};
				}
				return d100 > 30 ? new List<ShipSize> {ShipSize.Small} : EmptyList;
			}

			if (HeatLevel < 60)
			{
				if (d100 > 90)
				{
					return new List<ShipSize> {ShipSize.Large};
				}
				if (d100 > 80)
				{
					return new List<ShipSize> {ShipSize.Small, ShipSize.Small, ShipSize.Medium};
				}
				if (d100 > 70)
				{
					return new List<ShipSize> {ShipSize.Small, ShipSize.Small, ShipSize.Small, ShipSize.Small};
				}
				return d100 > 20 ? new List<ShipSize> {ShipSize.Small} : EmptyList;
			}

			if (HeatLevel < 80)
			{
				if (d100 > 90)
				{
					return new List<ShipSize> {ShipSize.Medium, ShipSize.Medium, ShipSize.Large};
				}
				if (d100 > 80)
				{
					return new List<ShipSize> {ShipSize.Small, ShipSize.Small, ShipSize.Small, ShipSize.Small, ShipSize.Medium};
				}
				return d100 > 60 ? new List<ShipSize> {ShipSize.Medium, ShipSize.Medium} : CreateSmallShips(d100 / 10);
			}

			// End game values
			if (d100 > 90)
			{
				return new List<ShipSize> {ShipSize.Large, ShipSize.Large, ShipSize.Large};
			}
			if (d100 > 80)
			{
				return new List<ShipSize> {ShipSize.Medium, ShipSize.Medium, ShipSize.Medium, ShipSize.Large};
			}
			return d100 > 60 ? new List<ShipSize> {ShipSize.Medium, ShipSize.Medium, ShipSize.Large} : CreateSmallShips(d100 / 10);
		}

		private static IList<ShipSize> CreateSmallShips(int smallShipsToSpawn)
		{
			var shipsToSpawn = new List<ShipSize>();
			for (var i = 0; i < smallShipsToSpawn; i++)
			{
				shipsToSpawn.Add(ShipSize.Small);
			}
			return shipsToSpawn;
		}

		internal ShipSize GenerateBackupShipSize()
		{
			NewShipGroupLaunched();
			if (HeatLevel < 20)
			{
				return ShipSize.Small;
			}
			return HeatLevel < 50 ? ShipSize.Medium : ShipSize.Large;
		}

		private void NewShipGroupLaunched()
		{
			HeatLevel++;
			ModLog.DebugInfo("Heat Level: " + HeatLevel);
		}

		public void GroupArrivedIntact()
		{
			HeatLevel--;
			ModLog.DebugInfo("Heat Level: " + HeatLevel);
		}
	}
}