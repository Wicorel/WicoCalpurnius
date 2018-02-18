using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;

namespace Scenario.Calpurnius
{
	public class MikiScrapManager : ModSystemUpdatable
	{
		private readonly List<MikiScrap> mikiScraps = new List<MikiScrap>();
		private readonly Dictionary<long, MikiScrapSaveData> restoredMikiScrapSavaData =
			new Dictionary<long, MikiScrapSaveData>();
		private readonly QueuedAudioSystem audioSystem;

		public MikiScrapManager(QueuedAudioSystem audioSystem)
		{
			this.audioSystem = audioSystem;
		}

		public override void GridInitialising(IMyCubeGrid grid)
		{
			if (!grid.IsStatic)
			{
				return;
			}
			
			var slimBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyRemoteControl);

			foreach (var slim in slimBlocks)
			{
				var remoteControl = slim.FatBlock as IMyRemoteControl;

				if (remoteControl.CustomName.Contains("RC_FURNACE") && remoteControl.IsOwnedByFaction("MIKI"))
				{
					grid.SetAllSubgridsInvulnerable(true);
					var mikiScrap = new MikiScrap(grid, remoteControl,
						grid.GetContainerInventoryOfName("MIKISCRAP_INPUT"),
						grid.GetContainerInventoryOfName("MIKISCRAP_OUTPUT"),
						grid.GetTerminalBlockMatchingName<IMyDoor>("FURNACE_DOOR"),
						grid.GetTerminalBlockMatchingName<IMySoundBlock>("MIKISCRAP_SPEAKER"),
						grid.GetTerminalBlocksOfType<IMyTextPanel>("FURNACE_LCD"),
						grid.GetTerminalBlocksOfType<IMyReflectorLight>("FURNACE_SPOTLIGHT"),
						grid.GetContainerInventoryOfName("FURNACE_OUTPUT"),
						audioSystem);

					MikiScrapSaveData saveData;
					if (restoredMikiScrapSavaData.TryGetValue(grid.EntityId, out saveData))
					{
						mikiScrap.RestoreSavedData(saveData);
					}
					ApplyBackFixes(grid);
					mikiScraps.Add(mikiScrap);
					return;
				}
			}
		}

		private static void ApplyBackFixes(IMyCubeGrid grid)
		{
			// Worried these might be causing lag as there are a lot of them
			foreach (var productionBlock in grid.GetTerminalBlocksOfType<IMyProductionBlock>())
			{
				productionBlock.Enabled = false;
			}

			foreach (var buttonPanel in grid.GetTerminalBlocksOfTypeInSubgrids<IMyButtonPanel>())
			{
				buttonPanel.AnyoneCanUse = true;
			}
		}
		
		public void LoadSaveData(List<MikiScrapSaveData> mikiScrapSaveDatas)
		{
			foreach (var mikiScrapSaveData in mikiScrapSaveDatas)
			{
				restoredMikiScrapSavaData.Add(mikiScrapSaveData.MikiScrapId, mikiScrapSaveData);
			}
		}
		
		public List<MikiScrapSaveData> GetSaveDatas()
		{
			var mikiScrapsData = new List<MikiScrapSaveData>();
			foreach (var mikiScrap in mikiScraps)
			{
				mikiScrapsData.Add(mikiScrap.GenerateSaveData());
			}
			return mikiScrapsData;
		}
		
		public override void Update60()
		{
			foreach (var mikiScrap in mikiScraps)
			{
				mikiScrap.Update();
			}
		}
	}
}