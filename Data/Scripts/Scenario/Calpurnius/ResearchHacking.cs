using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scenario.Calpurnius
{
	public class ResearchHacking : ModSystemUpdatable
	{
		public const string SeTextColor = "<color=202,228,241>";
		private HUDTextAPI.HUDMessage hackBar = new HUDTextAPI.HUDMessage(100, 30, new Vector2D(-0.95, 0.95), "");
		private readonly HUDTextAPI.HUDMessage hackInterrupted = new HUDTextAPI.HUDMessage(100, 30, new Vector2D(-0.95, 0.95),
			SeTextColor +"CONNECTION LOST");

		private const int HackingRangeSquared = 5*5; // 5 meters
		private const int HackingBarTicks = 26;
		private readonly ResearchControl researchControl;
		private readonly HUDTextAPI hudTextApi;
		private readonly NetworkComms networkComms;
		private readonly InterruptingAudioSystem audioSystem = new InterruptingAudioSystem();
		private readonly List<HackingLocation> hackingLocations = new List<HackingLocation>();
		private bool wasHackingLastUpdate;
		private int hackInterruptCooldown = 6;

		internal ResearchHacking(ResearchControl researchControl, HUDTextAPI hudTextApi, NetworkComms networkComms)
		{
			hackBar.options |= HUDTextAPI.Options.HideHud;
			hackInterrupted.options |= HUDTextAPI.Options.HideHud;
			this.researchControl = researchControl;
			this.hudTextApi = hudTextApi;
			this.networkComms = networkComms;
		}

		internal void InitHackingLocations()
		{
			AddHackingLocation(TechGroup.AtmosphericEngines, new Vector3D(1854774.5,-2005846.88,1325410.5));
			AddHackingLocation(TechGroup.GasStorage, new Vector3D(1869167.75,-2004920.12,1316376.38));
			AddHackingLocation(TechGroup.Rockets, new Vector3D(1843300.12,-1996436.5,1324474.12));
			AddHackingLocation(TechGroup.OxygenFarm, new Vector3D(1851936.75,-2001115.25,1324439.75));
			AddHackingLocation(TechGroup.OxygenGenerators, new Vector3D(1869136.62,-2004926.38,1316339.62));
		}

		private void AddHackingLocation(TechGroup techGroup, Vector3D coords)
		{
			if (!researchControl.UnlockedTechs.Contains(techGroup))
			{
				hackingLocations.Add(new HackingLocation(techGroup, coords));
			}
		}

		public override void Update30()
		{
			if (hackingLocations.Count == 0)
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

				foreach (var hack in hackingLocations.Reverse<HackingLocation>())
				{
					var distSq = Vector3D.DistanceSquared(hack.Coords, position);

					if (distSq <= HackingRangeSquared)
					{
						wasHackingLastUpdate = true;
						hack.CompletionTicks++;
						ShowLocalHackingProgress(hack.CompletionTicks);
						networkComms.ShowHackingProgressOnAllClients(hack.CompletionTicks);

						if (hack.CompletionTicks >= HackingBarTicks)
						{
							ShowLocalHackingSuccess();
							networkComms.ShowHackingSuccessOnAllClients();
							researchControl.UnlockTechGroupForAllPlayers(hack.TechGroup);
							hackingLocations.Remove(hack);
							wasHackingLastUpdate = false;
						}
						return; // Only one hack allowed at a time by one player
					}
				}
			}

			if (wasHackingLastUpdate)
			{
				if (hackInterruptCooldown == 0)
				{
					ShowLocalHackingInterruptStopped();
					wasHackingLastUpdate = false;
					hackInterruptCooldown = 6;
					networkComms.ShowHackingInterruptStoppedOnAllClients();
				}
				else
				{
					ShowLocalHackingInterrupted();
					hackInterruptCooldown--;
					networkComms.ShowHackingInterruptedOnAllClients();
				}
			}
		}

		internal void ShowLocalHackingProgress(int ticks)
		{
			audioSystem.EnsurePlaying(CalAudioClip.HackingSound);
			var hackbarStr = SeTextColor + "Hack in progress: ";
			var percent = ticks * 100 / HackingBarTicks;
			hackbarStr += percent + "%\n\n";
			for (var i = 0; i < ticks; i++)
			{
				hackbarStr += "|";
			}

			hackBar.message = hackbarStr;
			SendToHud(hackBar);
		}

		internal void ShowLocalHackingSuccess()
		{
			audioSystem.EnsurePlaying(CalAudioClip.HackFinished);
		}

		internal void ShowLocalHackingInterruptStopped()
		{
			audioSystem.Stop();
		}

		internal void ShowLocalHackingInterrupted()
		{
			audioSystem.EnsurePlaying(CalAudioClip.ConnectionLostSound);
			SendToHud(hackInterrupted);
		}

		private void SendToHud(HUDTextAPI.HUDMessage hudMessage)
		{
			if (hudTextApi.Heartbeat)
			{
				hudTextApi.Send(hudMessage);
			}
			else
			{
				MyAPIGateway.Utilities.ShowNotification("Error: You need to install the Text HUD API Mod!", 300, MyFontEnum.Red);
			}
		}

		internal List<HackingSaveData> GetSaveData()
		{
			var saveData = new List<HackingSaveData>();
			foreach (var hackingLocation in hackingLocations)
			{
				if (hackingLocation.CompletionTicks > 0)
				{
					saveData.Add(new HackingSaveData {
						Completion = hackingLocation.CompletionTicks, TechGroup 
						= hackingLocation.TechGroup
					});
				}
			}

			return saveData;
		}

		internal void RestoreSaveData(List<HackingSaveData> saveData)
		{
			if (saveData == null)
			{
				return;
			}
			foreach (var hackingSaveData in saveData)
			{
				foreach (var hackingLocation in hackingLocations)
				{
					if (hackingLocation.TechGroup == hackingSaveData.TechGroup)
					{
						hackingLocation.CompletionTicks = hackingSaveData.Completion;
					}
				}
			}
		}

		internal class HackingLocation
		{
			internal readonly TechGroup TechGroup;
			internal readonly Vector3D Coords;
			internal int CompletionTicks;

			public HackingLocation(TechGroup techGroup, Vector3D coords)
			{
				TechGroup = techGroup;
				Coords = coords;
			}
		}

		public class HackingSaveData
		{
			public int Completion { get; set; }
			public TechGroup TechGroup { get; set; }
		}
	}

}