using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Scenario
{
	public class PrefabGrid
	{
		// TODO: Add _SPACE to the types.
		// TODO: Maybe add police role
		private const string Unitialised = "UNITIALISED";

		private const string EscortRole = "_ESCORT";
		private const string CargoRole = "_CARGO";
		private const string PoliceRole = "_POLICE";
		private const string BackupRole = "_BACKUP";

		private const string UnitTypeAir = "_AIR";
		private const string UnitTypeGround = "_GROUND";
		private const string UnitTypeSpace = "_SPACE";
		
		//       private const string EscortAir = EscortRole + "_AIR";
//		private const string EscortGround = EscortRole + "_GROUND";

//        private const string CargoAir = CargoRole + "_AIR";
//		private const string CargoGround = CargoRole + "_GROUND";
//		private const string PoliceAir = PoliceRole + "_AIR";
//		private const string BackupAir = Unitialised + "_BACKUP_AIR";

//        private const string CargoSpace = CargoRole + "_SPACE";
//		private const string PoliceSpace = PoliceRole + "_SPACE";
//		private const string BackupSpace = Unitialised + "_BACKUP_SPACE";
		
		
		public string PrefabName { get; }
		public UnitType UnitType { get; }
		public UnitRole UnitRole { get; }
		public string InitialBeaconName { get; }

		public PrefabGrid(string prefabName, UnitType unitType, UnitRole unitRole)
		{
			PrefabName = prefabName;
			UnitType = unitType;
			UnitRole = unitRole;
			InitialBeaconName = GetBeaconName(unitRole, unitType);
		}

		private static string GetBeaconName(UnitRole unitRole, UnitType unitType)
		{
			string s = Unitialised;

			switch (unitRole)
			{
				case UnitRole.Delivery:
					s += CargoRole;
					break;
				case UnitRole.Escort:
					s += EscortRole;
					break;
				case UnitRole.Backup:
					s += BackupRole;
					break;
				case UnitRole.Police:
					s += PoliceRole;
					break;
				default:
					throw new ArgumentException(unitRole + " not recognised!");
			}
			switch(unitType)
			{
				case UnitType.Air:
					s += UnitTypeAir;
					break;
				case UnitType.Ground:
					s += UnitTypeGround;
					break;
				case UnitType.Space:
					s += UnitTypeSpace;
					break;
				default:
					throw new ArgumentException(unitType + " not recognised!");
			}
//		ModLog.Error("Spawning with beacon Name:" + s);
			return s;
		}
		
		internal static RoleAndUnitType? GetRoleAndUnitType(IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyBeacon);
			foreach (var slim in slimBlocks)
			{
				var beacon = slim.FatBlock as IMyBeacon;
				var beaconName = beacon.CustomName;
//		ModLog.Error("Checking spawn of beacon Name:" + beaconName);

				if (beaconName.Contains(Unitialised))
				{
					UnitRole ur = UnitRole.Backup; // default
					if (beaconName.Contains(EscortRole))
						ur = UnitRole.Escort;
					else if (beaconName.Contains(CargoRole))
						ur = UnitRole.Delivery;
					else if (beaconName.Contains(PoliceRole))
						ur = UnitRole.Police;
					else if (beaconName.Contains(BackupRole))
						ur = UnitRole.Backup;

					UnitType ut = UnitType.Air; // default;
					if (beaconName.Contains(UnitTypeSpace))
						ut = UnitType.Space;
					else if (beaconName.Contains(UnitTypeAir))
						ut = UnitType.Air;
					else if (beaconName.Contains(UnitTypeGround))
						ut = UnitType.Ground;

					return new RoleAndUnitType { UnitRole = ur, UnitType = ut };

				}
			}
			return null; // Not one of ours perhaps?
		}
	}
}