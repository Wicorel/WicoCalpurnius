using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace Scenario.Calpurnius
{
    // TODO: Need abtract base manager and have factions-specific base managers.
    // OR have base manager enumerate through the factions..

	public class BaseManager : ModSystemUpdatable
	{
		private readonly QueuedAudioSystem audioSystem;
		private readonly List<GCorpBase> bases = new List<GCorpBase>();

		private readonly Dictionary<long, GCorpBaseSaveData> restoredGcorpBaseSaveData =
			new Dictionary<long, GCorpBaseSaveData>();

		private static readonly DateTime ZeroDate = new DateTime(1970, 1, 1);

		internal BaseManager(QueuedAudioSystem audioSystem)
		{
			this.audioSystem = audioSystem;
		}

		public override void Update60()
		{
			foreach (var gCorpBase in bases)
			{
				gCorpBase.Update();
			}
		}

		internal void LoadSaveData(List<GCorpBaseSaveData> saveData)
		{
			foreach (var gCorpBaseSaveData in saveData)
			{
				restoredGcorpBaseSaveData.Add(gCorpBaseSaveData.BaseId, gCorpBaseSaveData);
			}
		}

		public override void GridInitialising(IMyCubeGrid grid)
		{

            /* Try to fix text not showing on text panels
             * 
             * (on further testing, panels are showing.. maybe Keen fixed this themselves?)
             * 
             * Saw on Rity's stream that they were NOT showing in all places.
             * And on EpikTek's https://www.youtube.com/watch?v=CkpGGPZd78k
             * */
            var slimBlocks2 = new List<IMySlimBlock>();
            grid.GetBlocks(slimBlocks2, b => b.FatBlock is IMyTextPanel);
            foreach (var slim in slimBlocks2)
            {
                var textPanel = slim.FatBlock as IMyTextPanel;
                bool bShow = textPanel.GetValueBool("ShowTextOnScreen");
                //                bool bShow = textPanel.ShowOnScreen != VRage.Game.GUI.TextPanel.ShowTextOnScreenFlag.NONE;
                if (bShow)
                {
                    textPanel.SetValue("ShowTextOnScreen", false);
                    //                     textPanel.SetShowOnScreen(VRage.Game.GUI.TextPanel.ShowTextOnScreenFlag.NONE);
                    textPanel.SetValue("ShowTextOnScreen", true);
                    //                      textPanel.SetShowOnScreen(VRage.Game.GUI.TextPanel.ShowTextOnScreenFlag.PUBLIC);
                }
                else
                {
                    // TODO: go through entityIDs and fix up graphics references

                }
            }


            //			if (grid.IsStatic && grid.IsControlledByFaction("GCORP"))
            {
                var slimBlocks = new List<IMySlimBlock>();
				grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyRemoteControl);
				foreach (var slim in slimBlocks)
				{
					var remoteControl = slim.FatBlock as IMyRemoteControl;
//					if (remoteControl.IsControlledByFaction("GCORP"))
//TODO: use another method to determine NPC bases.
					{
						var planet = DuckUtils.FindPlanetInGravity(remoteControl.GetPosition());

						if (planet == null)
						{
							continue; // Space bases not yet supported.
						}

						//TODO determine faction owner in above code
						bases.Add(new GCorpBase(remoteControl, ZeroDate, planet, audioSystem, CalFactions.Gcorp));
						return; // Accepted grid, no need to keep looping
					}
				}
			}
		}

		public override void AllGridsInitialised()
		{
			foreach (var npcBase in bases)
			{
				GCorpBaseSaveData saveData;
				if (restoredGcorpBaseSaveData.TryGetValue(npcBase.RemoteControl.EntityId, out saveData))
				{
					npcBase.RestoreSaveData(saveData);
				}
			}
			restoredGcorpBaseSaveData.Clear();
		}

		internal GCorpBase FindBaseWantingBackup()
		{
			foreach (var gCorpBase in bases)
			{
				if (gCorpBase.OfferBackup())
				{
					return gCorpBase;
				}
			}
			return null;
		}

		internal List<GCorpBaseSaveData> GetSaveData()
		{
			var gcorpBaseDatas = new List<GCorpBaseSaveData>();
			foreach (var gCorpBase in bases)
			{
				gcorpBaseDatas.Add(gCorpBase.GenerateSaveData());
			}
			return gcorpBaseDatas;
		}
	}
}