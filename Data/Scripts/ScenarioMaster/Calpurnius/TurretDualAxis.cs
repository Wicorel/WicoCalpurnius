using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	/// <summary>
	/// This is really only used for the derpy experimental mech. Code is a little dodgy and clangable.
	/// </summary>
	public class TurretDualAxis : ITurret
	{
		private int Range = 75;
		private readonly IMyRemoteControl remoteControl;
		private readonly IMyCubeGrid bodyGrid;
		private readonly IMyMotorStator azimuthRotor;
		private readonly List<IMyMotorStator> elevationRotors;
		private readonly List<IMyUserControllableGun> weapons;
		private readonly Vector3D position; // Turrets like this don't move
		private IMyPlayer playerTarget;
		private int timeSincePlayerSeen;
		private bool spoken;

		internal TurretDualAxis(IMyRemoteControl remoteControl, IMyCubeGrid bodyGrid, IMyMotorStator azimuthRotor,
			List<IMyMotorStator> elevationRotors, List<IMyUserControllableGun> weapons, int range=75)
		{
			this.remoteControl = remoteControl;
			this.bodyGrid = bodyGrid;
			this.azimuthRotor = azimuthRotor;
			this.elevationRotors = elevationRotors;
			this.weapons = weapons;
            Range = range;
//			position = bodyGrid.GetPosition();
		}

		public void Update60()
		{
			// TODO: need a reference to the whole base grid to properly determine ownership
			if (!remoteControl.IsControlledByFaction("GCORP"))
			{
//				return; // No point bothering to remove from the list, it will disappear next time the game reloads
			}

			playerTarget = DuckUtils.GetNearestPlayerToPosition(bodyGrid.GetPosition(), Range);
			if (playerTarget != null)
			{
				IHitInfo hitInfo;
				if (MyAPIGateway.Physics.CastLongRay(weapons[0].GetPosition(), playerTarget.GetPosition(), out hitInfo, false))
				{
					if (!spoken)
					{
						bodyGrid.PlaySoundBlocks();
						spoken = true;
					}

					if (timeSincePlayerSeen < 3)
					{
						timeSincePlayerSeen++;
						return;
					}

					var info = MyDetectedEntityInfoHelper.Create((MyEntity) hitInfo.HitEntity, remoteControl.OwnerId, hitInfo.Position);
					if (info.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies ||
					    info.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership ||
					    info.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral)
					{
						SetWeaponsShooting(true);
					}
					else
					{
						SetWeaponsShooting(false);
					}
					//	MyAPIGateway.Utilities.ShowNotification("Hit: " + info.Type, 2000, MyFontEnum.DarkBlue);
					//	MyAPIGateway.Utilities.ShowNotification("Relation: " + info.Relationship, 2000, MyFontEnum.DarkBlue);
				}
				else
				{
					SetWeaponsShooting(true);
				}
			}
			else
			{
				SetWeaponsShooting(false);
				StopAllRotors();
			}
		}

		private void SetWeaponsShooting(bool shoot)
		{
			var foundAMissileLauncher = false;
			foreach (var controllableGun in weapons)
			{
				if (controllableGun is IMySmallGatlingGun)
				{
					controllableGun.SetValue("Shoot", shoot);
				}
				else if (controllableGun is IMySmallMissileLauncher)
				{
					if (shoot && !foundAMissileLauncher && controllableGun.HasInventory && controllableGun.GetInventory(0).IsItemAt(0))
					{
						controllableGun.ApplyAction("ShootOnce");
						foundAMissileLauncher = true;
					}
				}
			}
		}

		public void Update1()
		{
			if (playerTarget == null)
			{
				return;
			}

			// TODO: need a reference to the whole base grid to properly determine ownership
			if (!remoteControl.IsControlledByFaction("GCORP"))
			{
//				return; // No point bothering to remove from the list, it will disappear next time the game reloads
			}

            TurnToFacePosition(playerTarget.Character.WorldAABB.Center);
            //			TurnToFacePosition(playerTarget.GetPosition());
        }

        private void StopAllRotors()
		{
			azimuthRotor.SetValue("Velocity", 0f);
			foreach (var rotor in elevationRotors)
			{
				rotor.SetValue("Velocity", 0f);
			}
		}

		/// <summary>
		/// This code is taken from Whip's AI Rotor Turret Control Script. All credit to Whiplash.
		/// https://steamcommunity.com/sharedfiles/filedetails/?id=672678005&
		/// </summary>
		/// <param name="targetPosition">point to face</param>
		private void TurnToFacePosition(Vector3D targetPosition)
		{
			//get orientation of reference
			IMyTerminalBlock turretReference = weapons[0];
			Vector3D turretFrontVec = turretReference.WorldMatrix.Forward;
			Vector3D absUpVec = azimuthRotor.WorldMatrix.Up;
			Vector3D turretSideVec = elevationRotors[0].WorldMatrix.Up;
			Vector3D turretFrontCrossSide = turretFrontVec.Cross(turretSideVec);

			//check elevation rotor orientation w.r.t. reference
			Vector3D turretUpVec;
			Vector3D turretLeftVec;
			if (DotIsSameDirection(absUpVec, turretFrontCrossSide))
			{
				turretUpVec = turretFrontCrossSide;
				turretLeftVec = turretSideVec;
			}
			else
			{
				turretUpVec = -1 * turretFrontCrossSide;
				turretLeftVec = -1 * turretSideVec;
			}

			//get vector to target point
			Vector3D referenceToTargetVec = targetPosition - turretReference.GetPosition();

			//get projections onto axis made out of our plane orientation
			Vector3D projOnFront = VectorProjection(referenceToTargetVec, turretFrontVec);
			Vector3D projOnLeft = VectorProjection(referenceToTargetVec, turretLeftVec);
			Vector3D projOnUp = VectorProjection(referenceToTargetVec, turretUpVec);
			Vector3D projOnFrontLeftPlane = projOnFront + projOnLeft;

			double azimuthAngle = Math.Asin(MathHelper.Clamp(projOnLeft.Length() * DotGetSign(projOnLeft, turretLeftVec) / projOnFrontLeftPlane.Length(), -1, 1));
			double elevationAngle = Math.Atan(projOnUp.Length() * DotGetSign(projOnUp, turretUpVec) / projOnFrontLeftPlane.Length()); //w.H*i/P=L-a!s,H-1,4.1 m.a/d.e t/h.i/s

			if (DotIsSameDirection(absUpVec, turretFrontCrossSide))
			{
				elevationAngle *= -1;
			}

			double azimuthSpeed = 20 * azimuthAngle; //derivitave term is useless as rotors dampen by default
			double elevationSpeed = 20 * elevationAngle;

			azimuthRotor.SetValue("Velocity", -(float) azimuthSpeed); //negative because we want to cancel the positive angle via our movements

			foreach (var elevationRotor in elevationRotors)
			{
				if (elevationRotor.WorldMatrix.Up == turretSideVec) // This is god-awful but maybe can be improved one day =/
				{
					elevationRotor.SetValue("Velocity", -(float) elevationSpeed);
				}
				else
				{
					elevationRotor.SetValue("Velocity", (float) elevationSpeed);
				}
			}
		}

		private bool DotIsSameDirection(Vector3D a, Vector3D b)
		{
			var x = a.Dot(b);
			return Math.Abs(Math.Abs(x) - x) < 0.00000000001; // This used to be an equals. Not sure about the tolerance value though.
		}

		private Vector3D VectorProjection(Vector3D a, Vector3D b)
		{
			var projection = a.Dot(b) / b.Length() / b.Length() * b;
			return projection;
		}

		private double DotGetSign(Vector3D a, Vector3D b)
		{
			var x = a.Dot(b);
			return x / Math.Abs(x);
		}
	}
}