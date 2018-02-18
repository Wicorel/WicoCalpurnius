using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Scenario
{
	internal class DuckUtils
	{
		internal static IEnumerable<T> GetEnumValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>();
		}

		/// <summary>
		/// Checks if any player is within distanceMeters of the given position
		/// </summary>
		/// <param name="myPosition"></param>
		/// <param name="distanceMeters"></param>
		/// <returns>true if player found close enough</returns>
		internal static bool IsAnyPlayerNearPosition(Vector3D myPosition, double distanceMeters)
		{
			var players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);
			var distanceMetersSq = distanceMeters * distanceMeters;
			foreach (var player in players)
			{
				var controlled = player.Controller.ControlledEntity;
				if (controlled == null) continue;
				var position = controlled.Entity.GetPosition();
				var distSq = Vector3D.DistanceSquared(myPosition, position);
				if (distSq < distanceMetersSq)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the nearest player to a position, given a maximum distance they can be away from it.
		/// Those further than the maximum are ignored.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="maxDistanceMeters"></param>
		/// <returns>IMyPlayer or null</returns>
		internal static IMyPlayer GetNearestPlayerToPosition(Vector3D position, double maxDistanceMeters)
		{
			var maxDistanceSq = maxDistanceMeters * maxDistanceMeters;
			var players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);
			var closestDistSq = double.MaxValue;
			IMyPlayer result = null;

			foreach (var player in players)
			{
				var controlled = player.Controller.ControlledEntity;
				if (controlled == null) continue;
				var distSq = Vector3D.DistanceSquared(position, controlled.Entity.GetPosition());
				if (distSq < closestDistSq && distSq < maxDistanceSq)
				{
					closestDistSq = distSq;
					result = player;
				}
			}
			return result;
		}

		//We could use MyGamePruningStructure.GetClosestPlanet but it is not recommended
		internal static MyPlanet FindPlanetInGravity(Vector3D vector3D)
		{
			var planets = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(planets, x => x is MyPlanet);
			foreach (var planet in planets)
			{
				if (planet.Components.Get<MyGravityProviderComponent>().IsPositionInRange(vector3D))
				{
					return (MyPlanet) planet;
				}
			}
			return null;
		}

		internal static void SpawnInGravity(Vector3D position, Vector3D naturalGravity, long factionId, 
			string prefabName, string initialBeaconName)
		{
			SpawnInGravity(position, naturalGravity, factionId, prefabName, initialBeaconName,
				MyUtils.GetRandomPerpendicularVector(ref naturalGravity));
		}

		internal static void SpawnInGravity(Vector3D position, Vector3D naturalGravity, long factionId, 
			string prefabName, string initialBeaconName, Vector3D forwards)
		{
			var up = -Vector3D.Normalize(naturalGravity);
			// We need to set neutral owner due to a bug http://forum.keenswh.com/threads/01-157-stable-myprefabmanager-spawnprefab-doesnt-always-set-owner-id.7389238/
			MyAPIGateway.PrefabManager.SpawnPrefab(new List<IMyCubeGrid>(), prefabName, position, forwards, up,
				new Vector3(0f), spawningOptions: SpawningOptions.SetNeutralOwner, beaconName: initialBeaconName,
				ownerId: factionId);
		}

		internal static void AddGpsToAllPlayers(string name, string description, Vector3D coords)
		{
			var gpsSystem = MyAPIGateway.Session.GPS;
			var gps = gpsSystem.Create(name, description, coords, true);
			var players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);
			foreach (var myPlayer in players)
			{
				gpsSystem.AddGps(myPlayer.IdentityId, gps);
			}
		}

		internal static bool IsPlayerId(long id)
		{
			var players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);
			foreach (var player in players)
			{
				if (id == player.IdentityId)
				{
					return true;
				}
			}
			return false;
		}

		internal static bool IsPlayerUnderCover(IMyPlayer player)
		{
			var planet = FindPlanetInGravity(player.GetPosition());
			if (planet == null)
			{
				return false;
			}
			var playerNaturalGravity = planet.GetGravityAtPoint(player.GetPosition());
			var upAbovePlayer = player.GetPosition() + playerNaturalGravity * -5.0f;
			//	var vector4 = Color.Yellow.ToVector4();
			//	MySimpleObjectDraw.DrawLine(player.GetPosition(), upAbovePlayer, MyStringId.GetOrCompute("Square"), ref vector4, 0.04f);
			IHitInfo hitInfo;
			if (MyAPIGateway.Physics.CastRay(upAbovePlayer, player.GetPosition(), out hitInfo))
			{
				var entity = hitInfo.HitEntity;
				if (entity == null)
				{
					return false;
				}
				return !(entity is IMyCharacter);
			}
			return false;
		}
		
	}
	
}