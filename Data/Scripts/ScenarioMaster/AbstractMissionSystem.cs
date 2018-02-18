using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario
{
	public abstract class AbstractMissionSystem : ModSystemUpdatable
	{
		private readonly QueuedAudioSystem audioSystem;
		private readonly DateTime missionStartTime;
		private readonly HashSet<int> excludedIDs;
		private readonly List<TimeBasedMissionPrompt> timeBasedPrompts = new List<TimeBasedMissionPrompt>();
		private bool init;

		private readonly List<LocationBasedMissionPrompt> locationbasedMissionPrompts =
			new List<LocationBasedMissionPrompt>();

		protected AbstractMissionSystem(long missionStartTimeBinary, HashSet<int> alreadyExecutedPrompts,
			QueuedAudioSystem audioSystem)
		{
			this.audioSystem = audioSystem;
			missionStartTime = DateTime.FromBinary(missionStartTimeBinary);
			excludedIDs = alreadyExecutedPrompts;
		}

		public long GetMissionStartTimeBinary()
		{
			return missionStartTime.ToBinary();
		}

		public HashSet<int> GetExcludedIDs()
		{
			return excludedIDs;
		}

		protected abstract void GeneratePrompts();

		protected Action PlayAudioClip(IAudioClip clip)
		{
			return () => { audioSystem.PlayAudio(clip); };
		}

		protected static Action AddGps(string name, string description, Vector3D coords)
		{
			return () => { DuckUtils.AddGpsToAllPlayers(name, description, coords); };
		}

		protected void AddTimePrompt(int id, TimeSpan timeIntoGameTriggered, params Action[] actions)
		{
			if (excludedIDs.Contains(id))
			{
				return;
			}
			var prompt = new TimeBasedMissionPrompt(id, new List<Action>(actions), missionStartTime + timeIntoGameTriggered,
				excludedIDs);
			timeBasedPrompts.Add(prompt);
		}

		protected void AddProximityPrompt(int id, Vector3D locationVector, double distance, params Action[] actions)
		{
			if (excludedIDs.Contains(id))
			{
				return;
			}
			var prompt = new LocationBasedMissionPrompt(id, new List<Action>(actions), locationVector, distance, excludedIDs);
			locationbasedMissionPrompts.Add(prompt);
		}

		public override void Update60()
		{
			if (!init)
			{
				GeneratePrompts();
				timeBasedPrompts.Sort((x, y) => -x.TriggerTime.CompareTo(y.TriggerTime));
				init = true;
			}
			
			UpdateLocationBasedPrompts();
			UpdateTimeBasedPrompts();
		}

		private void UpdateLocationBasedPrompts()
		{
			if (locationbasedMissionPrompts.Count == 0)
			{
				return;
			}

			var players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);

			foreach (var player in players)
			{
				var controlled = player.Controller.ControlledEntity;
				if (controlled == null) continue;
				var position = controlled.Entity.GetPosition();

				foreach (var locationPrompt in locationbasedMissionPrompts.Reverse<LocationBasedMissionPrompt>())
				{
					var distSq = Vector3D.DistanceSquared(locationPrompt.Location, position);

					if (!(distSq <= locationPrompt.DistanceSquared))
					{
						continue;
					}
					
					locationPrompt.Run();
					locationbasedMissionPrompts.Remove(locationPrompt);
					excludedIDs.Add(locationPrompt.Id); // Never trigger this again
				}
			}
		}

		private void UpdateTimeBasedPrompts()
		{
			if (timeBasedPrompts.Count == 0)
			{
				return;
			}

			var prompt = timeBasedPrompts[timeBasedPrompts.Count - 1];
			if (MyAPIGateway.Session.GameDateTime < prompt.TriggerTime)
			{
				return;
			}
			
			prompt.Run();
			timeBasedPrompts.RemoveAt(timeBasedPrompts.Count - 1);
			excludedIDs.Add(prompt.Id); // Never trigger this again
		}
	}

	internal abstract class MissionPrompt
	{
		internal int Id { get; }
		private readonly List<Action> actions;
		private readonly HashSet<int> excludedIDs;

		internal MissionPrompt(List<Action> actions, int id, HashSet<int> excludedIDs)
		{
			this.actions = actions;
			this.excludedIDs = excludedIDs;
			Id = id;
		}

		internal void Run()
		{
			if (excludedIDs.Contains(Id))
			{
				return; // Do nothing, another prompt sharing our ID was triggered already
			}

			foreach (var action in actions)
			{
				action();
			}
		}
	}

	internal class TimeBasedMissionPrompt : MissionPrompt
	{
		internal DateTime TriggerTime { get; }

		internal TimeBasedMissionPrompt(int id, List<Action> actions, DateTime triggerTime, HashSet<int> excludedIDs)
			: base(actions, id, excludedIDs)
		{
			TriggerTime = triggerTime;
		}
	}

	internal class LocationBasedMissionPrompt : MissionPrompt
	{
		internal Vector3D Location { get; }
		internal double DistanceSquared { get; }

		internal LocationBasedMissionPrompt(int id, List<Action> actions, Vector3D locationVector, double distance,
			HashSet<int> excludedIDs) : base(actions, id, excludedIDs)
		{
			Location = locationVector;
			DistanceSquared = distance * distance;
		}
	}
}