using System;

namespace Scenario.Calpurnius
{
	internal class CalPrefabFactory //TODO make internal again once extending interface 
	{

        // TODO: Make prefabs per-NPC-faction
		private static readonly PrefabGrid LightAirTransport = new PrefabGrid("Envoy", UnitType.Air, UnitRole.Delivery);

		private static readonly PrefabGrid MediumAirTransport = new PrefabGrid("Medium GCorp Transport", UnitType.Air,
			UnitRole.Delivery);

		private static readonly PrefabGrid LargeAirTransport = new PrefabGrid("Large GCorp Transport", UnitType.Air,
			UnitRole.Delivery);

		private static readonly PrefabGrid LightAirEscort = new PrefabGrid("Combat Drone", UnitType.Air, UnitRole.Escort);

		private static readonly PrefabGrid MediumAirEscort = new PrefabGrid("Medium GCorp Combat Flyer", UnitType.Air, UnitRole.Escort);

		private static readonly PrefabGrid RocketAirEscort = new PrefabGrid("Medium GCorp Missile Flyer", UnitType.Air, UnitRole.Escort);

		private static readonly PrefabGrid LightGroundTransport = new PrefabGrid("Small Hover Transport", UnitType.Ground,
			UnitRole.Delivery);

		private static readonly PrefabGrid MediumGroundTransport = new PrefabGrid("Medium Hover Transport", UnitType.Ground,
			UnitRole.Delivery);

		private static readonly PrefabGrid LargeGroundTransport = new PrefabGrid("Large Hover Transport", UnitType.Ground,
				UnitRole.Delivery);

		private static readonly PrefabGrid LightGroundEscort = new PrefabGrid("Light Hover Drone", UnitType.Ground,
			UnitRole.Escort);

		private static readonly PrefabGrid MediumGroundEscort = new PrefabGrid("Medium Hover Drone", UnitType.Ground,
			UnitRole.Escort);

		private static readonly PrefabGrid RocketGroundEscort = new PrefabGrid("Medium Rocket Hover Drone", UnitType.Ground,
			UnitRole.Escort);

		private static readonly PrefabGrid LightAirBackup = new PrefabGrid("Combat Drone", UnitType.Air, UnitRole.Backup);

		private static readonly PrefabGrid MediumAirBackup = new PrefabGrid("Medium GCorp Combat Flyer", UnitType.Air, UnitRole.Backup);

		private static readonly PrefabGrid RocketAirBackup = new PrefabGrid("Medium GCorp Missile Flyer", UnitType.Air, UnitRole.Backup);
		// Define more prefabs here...

		internal static PrefabGrid GetBackup(ShipSize size)
		{
			switch (size)
			{
				case ShipSize.Large:
					return RocketAirBackup;
				case ShipSize.Medium:
					return MediumAirBackup;
				case ShipSize.Small:
					return LightAirBackup;
				default:
					throw new ArgumentException(size + " air backup not found!");
			}
		}

		internal static PrefabGrid GetAirTransport(ShipSize size)
		{
			switch (size)
			{
				case ShipSize.Large:
					return LargeAirTransport;
				case ShipSize.Medium:
					return MediumAirTransport;
				case ShipSize.Small:
					return LightAirTransport;
				default:
					throw new ArgumentException(size + " air transport not found!");
			}
		}

		internal static PrefabGrid GetGroundTransport(ShipSize size)
		{
			switch (size)
			{
				case ShipSize.Large:
					return LargeGroundTransport;
				case ShipSize.Medium:
					return MediumGroundTransport;
				case ShipSize.Small:
					return LightGroundTransport;
				default:
					throw new ArgumentException(size + " ground transport not found!");
			}
		}

		internal static PrefabGrid GetEscort(UnitType unitType, ShipSize shipSize)
		{
			return unitType == UnitType.Air ? GetAirEscort(shipSize) : GetGroundEscort(shipSize);
		}

		private static PrefabGrid GetAirEscort(ShipSize size)
		{
			switch (size)
			{
				case ShipSize.Large:
					return RocketAirEscort;
				case ShipSize.Medium:
					return MediumAirEscort;
				case ShipSize.Small:
					return LightAirEscort;
				default:
					throw new ArgumentException(size + " air escort not found!");
			}
		}

		private static PrefabGrid GetGroundEscort(ShipSize size)
		{
			switch (size)
			{
				case ShipSize.Large:
					return RocketGroundEscort;
				case ShipSize.Medium:
					return MediumGroundEscort;
				case ShipSize.Small:
					return LightGroundEscort;
				default:
					throw new ArgumentException(size + " ground escort not found!");
			}
		}
    }
}