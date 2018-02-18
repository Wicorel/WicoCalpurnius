using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Scenario.Calpurnius
{
	public class TurretManager : ModSystemRapidUpdatable
	{
		private readonly List<ITurret> turrets = new List<ITurret>();

		public override void GridInitialising(IMyCubeGrid grid)
		{
			if (grid.IsStatic)
			{
				return;
			}
			
			//if (grid.IsAliveAndGCorpControlled()) ?? do we need this for turrets?

			var azimuthRotors = FindRotorsWithSubgrid(grid, "TURRET_ROTOR");
			if (azimuthRotors.Count > 0)
			{
				var azimuthRotor = azimuthRotors[0];
				var subGrid = azimuthRotor.TopGrid;
				var remoteControl = FindFirstRemoteControl(subGrid);

                // TODO: Need another method to determine loss of control
				if (remoteControl != null /* && remoteControl.IsControlledByFaction("GCORP") */)
				{
					var elevationRotors = FindRotorsWithSubgrid(azimuthRotor.TopGrid, "TURRET_ROTOR_ELEVATION");
					var weapons = new List<IMyUserControllableGun>();
					foreach (var elevationRotor in elevationRotors)
					{
						weapons.AddList(elevationRotor.TopGrid.FindNonTurretWeapons());
					}

					if (elevationRotors.Count == 0 || weapons.Count == 0)
					{
						turrets.Add(new TurretSingleAxis(remoteControl, subGrid, azimuthRotor));
					}
					else
					{
						turrets.Add(new TurretDualAxis(remoteControl, subGrid, azimuthRotor, elevationRotors, weapons));
					}
				}
			}
		}

		internal static List<IMyMotorStator> FindRotorsWithSubgrid(IMyCubeGrid parentGrid, string nameMatch)
		{
			var rotors = new List<IMyMotorStator>();
			var slimBlocks = new List<IMySlimBlock>();
			parentGrid.GetBlocks(slimBlocks, b => b.FatBlock is IMyMotorStator);
			foreach (var slim in slimBlocks)
			{
				var rotorFound = slim.FatBlock as IMyMotorStator;
				var subGrid = rotorFound.TopGrid;
				if (subGrid != null && rotorFound.CustomName.Contains(nameMatch))
				{
					rotors.Add(rotorFound);
				}
			}

			return rotors;
		}

		private static IMyRemoteControl FindFirstRemoteControl(IMyCubeGrid grid)
		{
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyRemoteControl);
			foreach (var slim in slimBlocks)
			{
				var remoteControl = slim.FatBlock as IMyRemoteControl;
				return remoteControl;
			}
			return null;
		}

		public override void Update1()
		{
			foreach (var turret in turrets)
			{
				turret.Update1();
			}
		}

		public override void Update60()
		{
			// Lets turrets check around for enemies, but they mostly sleep until needed
			foreach (var turret in turrets)
			{
				turret.Update60();
			}
		}
	}
}