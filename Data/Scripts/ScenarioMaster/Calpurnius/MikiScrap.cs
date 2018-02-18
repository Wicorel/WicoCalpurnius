using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Scenario.Calpurnius
{
	internal class MikiScrap
	{
		

		private readonly MyDefinitionId scrapMetal = MyVisualScriptLogicProvider.GetDefinitionId("Ore", "Scrap");
		private const int AudioCommentTime = 60 * 10;
		private const int AudioMaxTime = 60 * 30;
		private const int FurnaceWorkingTime = 60;
		private const float FractionOfMaterialsReturnedFromFurnace = 0.85f;
		private readonly IMyCubeGrid mikiScrapGrid;
		private readonly IMyRemoteControl remoteControl;
		private readonly MyInventory scrapIn;
		private readonly MyInventory output;
		private readonly IMyDoor furnaceDoor;
		private readonly IMySoundBlock speaker;
		private readonly List<IMyTextPanel> furnaceLcds;
		private readonly List<IMyReflectorLight> furnaceSpotlights;
		private readonly MyInventory furnaceOutput;
		private readonly QueuedAudioSystem audioSystem;
		private readonly Random random = new Random();
		private FurnaceState furnaceState = FurnaceState.Off;
		private int furnaceWorkingCountdown;

		private int timeSinceSpoken = -2; // -2 means player hasn't gone into antenna range even,
		//-1 means player hasn't visited Miki Scrap yet. Otherwise it's normally counting up to AudioMaxTime

		//				MyAPIGateway.Utilities.ShowNotification(""+timeSinceSpoken, 1000, MyFontEnum.Debug);

		internal MikiScrap(IMyCubeGrid mikiScrapGrid, IMyRemoteControl remoteControl, MyInventory scrapIn, MyInventory output,
			IMyDoor furnaceDoor, IMySoundBlock speaker, List<IMyTextPanel> furnaceLcds,
			List<IMyReflectorLight> furnaceSpotlights,
			MyInventory furnaceOutput, QueuedAudioSystem audioSystem)
		{
			this.mikiScrapGrid = mikiScrapGrid;
			this.remoteControl = remoteControl;
			this.scrapIn = scrapIn;
			this.output = output;
			this.furnaceDoor = furnaceDoor;
			this.speaker = speaker;
			this.furnaceLcds = furnaceLcds;
			this.furnaceSpotlights = furnaceSpotlights;
			this.furnaceOutput = furnaceOutput;
			this.audioSystem = audioSystem;
		}

		internal void RestoreSavedData(MikiScrapSaveData saveData)
		{
			furnaceState = saveData.FurnaceState;
			furnaceWorkingCountdown = saveData.FurnaceWorkingCountdown;
			timeSinceSpoken = saveData.TimeSinceSpoken;
		}

		internal void Update()
		{
			if (timeSinceSpoken == -2)
			{
				if (DuckUtils.GetNearestPlayerToPosition(speaker.GetPosition(), 3000) != null)
				{
					audioSystem.PlayAudio(CalAudioClip.GreetingsMartianColonists);
					timeSinceSpoken = -1;
				}

				return;
			}

			var playerVisiting = DuckUtils.GetNearestPlayerToPosition(speaker.GetPosition(), 50) != null;
			if (timeSinceSpoken == -1 && playerVisiting)
			{
				audioSystem.PlayAudio(CalAudioClip.WelcomeMikiScrap);
				timeSinceSpoken = AudioCommentTime;
			}
			else if (timeSinceSpoken >= 0 && timeSinceSpoken < AudioMaxTime)
			{
				timeSinceSpoken++;
			}
			else if (timeSinceSpoken == AudioMaxTime && playerVisiting)
			{
				audioSystem.PlayAudioRandomChance(1.0, CalAudioClip.TellAllFriends, CalAudioClip.TiredOfGrindingCrap,
					CalAudioClip.NewMikiScrapsOpen);
				timeSinceSpoken = AudioCommentTime;
			}

			var scrap = scrapIn.GetItemAmount(scrapMetal);
			if (scrap > 0)
			{
				ProcessScrap(scrap > 500 ? 500 : scrap);
			}

			if (playerVisiting)
			{
				UpdateFurnaceState();
			}
		}

		internal MikiScrapSaveData GenerateSaveData()
		{
			return new MikiScrapSaveData
			{
				TimeSinceSpoken = timeSinceSpoken,
				FurnaceWorkingCountdown = furnaceWorkingCountdown,
				FurnaceState = furnaceState,
				MikiScrapId = mikiScrapGrid.EntityId
			};
		}

		internal Vector3D GetPosition()
		{
			return remoteControl.GetPosition();
		}

		private void UpdateFurnaceState()
		{
			if (furnaceState != FurnaceState.Working && furnaceDoor.Status == DoorStatus.Open)
			{
				var grids = FindGridsInsideFurnace();
				ChangeFurnaceState(grids.Count == 0 ? FurnaceState.Waiting : FurnaceState.LoadedUp);
			}
			else if (furnaceDoor.Status == DoorStatus.Closed)
			{
				if (furnaceState == FurnaceState.Working)
				{
					if (furnaceWorkingCountdown == 0)
					{
						SetFurnaceDoorsEnabled(true);
						ChangeFurnaceState(FurnaceState.Waiting);
						speaker.Stop();
						furnaceOutput.MoveAllContentsTo(output);
						foreach (var spotlight in furnaceSpotlights)
						{
							spotlight.Intensity = 1.0f;
						}
					}
					else
					{
						furnaceWorkingCountdown--;
					}
				}
				else
				{
					var grids = FindGridsInsideFurnace();
					if (grids.Count > 0)
					{
						SetFurnaceDoorsEnabled(false);
						speaker.Play(); // Has the lava sound on it
						PlayRandomAudio(CalAudioClip.WeCrushDown, CalAudioClip.WhereIsThatFrom, CalAudioClip.DontBreatheIn);
						MeltGrids(grids);
						ChangeFurnaceState(FurnaceState.Working);
						furnaceWorkingCountdown = FurnaceWorkingTime;
						foreach (var spotlight in furnaceSpotlights)
						{
							spotlight.Intensity = 4.9f;
						}
					}
					else
					{
						ChangeFurnaceState(FurnaceState.Waiting);
					}
				}
			}
		}

		private void SetFurnaceDoorsEnabled(bool powered)
		{
			foreach (var door in mikiScrapGrid.GetTerminalBlocksOfType<IMyDoor>("FURNACE_DOOR"))
			{
				door.Enabled = powered;
				if (powered)
				{
					door.OpenDoor();
				}
			}
		}

		private const double FurnaceSizeSquared = 10.0 * 10.0;

		private ICollection<IMyCubeGrid> FindGridsInsideFurnace()
		{
			var cubeGrids = new HashSet<IMyCubeGrid>();
			var ents = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(ents, e => e is IMyCubeGrid);
			foreach (var ent in ents)
			{
				if (ent.EntityId == mikiScrapGrid.EntityId) continue; // Don't melt ourselves!
				var distSq = Vector3D.DistanceSquared(remoteControl.GetPosition(), ent.GetPosition());

				if (distSq < FurnaceSizeSquared)
				{
					var grid = (IMyCubeGrid) ent;
					if (!grid.IsIndestructableOrUneditable())
					{
						cubeGrids.Add(grid);
					}
				}
			}
			return cubeGrids;
		}

		private void MeltGrids(IEnumerable<IMyCubeGrid> grids)
		{
			foreach (var grid in grids)
			{
				var ingotsWanted = grid.CalculateIngotCost();
				foreach (var entry in ingotsWanted)
				{
					var ingotBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>(entry.Key.SubtypeName);
					furnaceOutput.AddItems(entry.Value * FractionOfMaterialsReturnedFromFurnace, ingotBuilder);
				}
				grid.CloseAll();
			}
		}

		private void ProcessScrap(MyFixedPoint scrapAmount)
		{
			scrapIn.RemoveItemsOfType(scrapAmount, scrapMetal);
			var componentType = IronComponent.GenerateComponent(random);
			var amountToMake = (int) (scrapAmount.ToIntSafe() / componentType.IronValue);
			if (amountToMake == 0)
			{
				return;
			}

			PlayRandomAudio(CalAudioClip.ThisIsGoodScrap, CalAudioClip.WhereDoYouGetScrapMetal, CalAudioClip.BestCustomer);
			output.AddItems(amountToMake, componentType.ObjectBuilder);
		}

		private void PlayRandomAudio(params CalAudioClip[] audioClips)
		{
			if (GetTalkPermission())
			{
				audioSystem.PlayAudioRandomChance(1.0, audioClips);
			}
		}

		private bool GetTalkPermission()
		{
			if (timeSinceSpoken > AudioCommentTime)
			{
				timeSinceSpoken = 0;
				return true;
			}
			return false;
		}

		private void ChangeFurnaceState(FurnaceState newState)
		{
			if (newState == furnaceState)
			{
				return;
			}

			furnaceState = newState;
			SetFurnaceLcdsText(GetTextForState(newState));
		}

		private string GetTextForState(FurnaceState state)
		{
			switch (state)
			{
				case FurnaceState.Waiting:
					return @"      Drop
     unwanted
  vehicles and
   large items
  into furnace";
				case FurnaceState.LoadedUp:
					return @"
   Close doors
    to activate
      furnace";
				case FurnaceState.Working:
					return @"  ! Caution !
   Furnace is
      active

      Extreme
  temperatures";
				case FurnaceState.Off:
					throw new ArgumentException("Furnace should never change to state: " + state);
				default:
					throw new ArgumentException("Uncoped for state: " + state);
			}
		}

		private void SetFurnaceLcdsText(string txt)
		{
			foreach (var lcd in furnaceLcds)
			{
				lcd.WritePublicText(txt);
			}
		}
	}

	public enum FurnaceState
	{
		Off,
		Waiting,
		LoadedUp,
		Working
	}

	public class MikiScrapSaveData
	{
		public long MikiScrapId { get; set; }
		public int TimeSinceSpoken { get; set; }
		public int FurnaceWorkingCountdown { get; set; }
		public FurnaceState FurnaceState { get; set; }
	}
}