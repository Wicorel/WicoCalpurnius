using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Scenario
{
	/// <summary>
	/// Detects damage to particular grids so they can act on it.
	/// </summary>
	public class DamageSensor
	{
		private readonly Dictionary<long, IDamageObserver> damageObservers = new Dictionary<long, IDamageObserver>();
		private bool hasObservers;

		public void RegisterDamageObserver(long gridEntityId, IDamageObserver damageObserver)
		{
			if (damageObservers.Count == 0)
			{
				MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(9999, HandleDamage);
				hasObservers = true;
			}

			damageObservers.Add(gridEntityId, damageObserver);
		}

		public void UnRegisterDamageObserver(long gridEntityId)
		{
			damageObservers.Remove(gridEntityId);

			if (damageObservers.Count == 0)
			{
				// No way to remove the handler, oh well
				hasObservers = false;
			}
		}

		public void HandleDamage(object target, MyDamageInformation info)
		{
			if (!hasObservers)
			{
				return;
			}

			var slim = target as IMySlimBlock;
			if (slim != null)
			{
				GridDamaged(slim.CubeGrid, info);
			}
			else
			{
				var fat = target as IMyCubeBlock;
				if (fat != null)
				{
					GridDamaged(fat.CubeGrid, info);
				}
			}
		}

		private void GridDamaged(IMyEntity grid, MyDamageInformation info)
		{
			IDamageObserver observer;
			if (damageObservers.TryGetValue(grid.EntityId, out observer))
			{
				//MyAPIGateway.Utilities.ShowNotification("Attacker ID: " + info.AttackerId);
				observer.MaliciouslyDamaged();
			}
		}
	}
}