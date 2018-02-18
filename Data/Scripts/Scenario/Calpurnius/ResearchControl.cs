using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Scenario.Calpurnius
{
    internal class ResearchControl
    {
        private readonly MyDefinitionId refinery = MyVisualScriptLogicProvider.GetDefinitionId("Refinery", "LargeRefinery");

        private readonly MyDefinitionId blastFurnace =
            MyVisualScriptLogicProvider.GetDefinitionId("Refinery", "Blast Furnace");

        private readonly MyDefinitionId jumpDrive =
            MyVisualScriptLogicProvider.GetDefinitionId("JumpDrive", "LargeJumpDrive");

        private readonly MyDefinitionId radioAntennaLarge = MyVisualScriptLogicProvider.GetDefinitionId("RadioAntenna",
            "LargeBlockRadioAntenna");

        private readonly MyDefinitionId radioAntennaSmall = MyVisualScriptLogicProvider.GetDefinitionId("RadioAntenna",
            "SmallBlockRadioAntenna");

        private readonly MyDefinitionId largeMissileTurret = MyVisualScriptLogicProvider.GetDefinitionId(
            "LargeMissileTurret", null);

        private readonly MyDefinitionId smallMissileTurret = MyVisualScriptLogicProvider.GetDefinitionId(
            "LargeMissileTurret", "SmallMissileTurret");

        private readonly MyDefinitionId rocketLauncher = MyVisualScriptLogicProvider.GetDefinitionId("SmallMissileLauncher",
            null);

        private readonly MyDefinitionId largeRocketLauncher =
            MyVisualScriptLogicProvider.GetDefinitionId("SmallMissileLauncher", "LargeMissileLauncher");

        private readonly MyDefinitionId smallReloadableRocketLauncher =
            MyVisualScriptLogicProvider.GetDefinitionId("SmallMissileLauncherReload", "SmallRocketLauncherReload");

        private readonly MyDefinitionId ionThrusterSmallShipSmall = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "SmallBlockSmallThrust");

        private readonly MyDefinitionId ionThrusterSmallShipLarge = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "SmallBlockLargeThrust");

        private readonly MyDefinitionId ionThrusterLargeShipSmall = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "LargeBlockSmallThrust");

        private readonly MyDefinitionId ionThrusterLargeShipLarge = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "LargeBlockLargeThrust");

        private readonly MyDefinitionId hydroThrusterSmallShipSmall = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "SmallBlockSmallHydrogenThrust");

        private readonly MyDefinitionId hydroThrusterSmallShipLarge = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "SmallBlockLargeHydrogenThrust");

        private readonly MyDefinitionId hydroThrusterLargeShipSmall = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "LargeBlockSmallHydrogenThrust");

        private readonly MyDefinitionId hydroThrusterLargeShipLarge = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "LargeBlockLargeHydrogenThrust");

        private readonly MyDefinitionId atmoThrusterSmallShipSmall = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "SmallBlockSmallAtmosphericThrust");

        private readonly MyDefinitionId atmoThrusterSmallShipLarge = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "SmallBlockLargeAtmosphericThrust");

        private readonly MyDefinitionId atmoThrusterLargeShipSmall = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "LargeBlockSmallAtmosphericThrust");

        private readonly MyDefinitionId atmoThrusterLargeShipLarge = MyVisualScriptLogicProvider.GetDefinitionId("Thrust",
            "LargeBlockLargeAtmosphericThrust");

        private readonly MyDefinitionId oxygenFarm = MyVisualScriptLogicProvider.GetDefinitionId("OxygenFarm",
            "LargeBlockOxygenFarm");

        private readonly MyDefinitionId oxygenGeneratorLarge = MyVisualScriptLogicProvider.GetDefinitionId("OxygenGenerator",
            null);

        private readonly MyDefinitionId oxygenGeneratorSmall = MyVisualScriptLogicProvider.GetDefinitionId("OxygenGenerator",
            "OxygenGeneratorSmall");

        private readonly MyDefinitionId oxygenTankLarge = MyVisualScriptLogicProvider.GetDefinitionId("OxygenTank",
            null);

        private readonly MyDefinitionId oxygenTankSmall = MyVisualScriptLogicProvider.GetDefinitionId("OxygenTank",
            "OxygenTankSmall");

        private readonly MyDefinitionId hydrogenTankLarge = MyVisualScriptLogicProvider.GetDefinitionId("OxygenTank",
            "LargeHydrogenTank");

        private readonly MyDefinitionId hydrogenTankSmall = MyVisualScriptLogicProvider.GetDefinitionId("OxygenTank",
            "SmallHydrogenTank");

	    private readonly MyDefinitionId projectorLarge = MyVisualScriptLogicProvider.GetDefinitionId("MyObjectBuilder_Projector",
		    "LargeProjector");

	    private readonly MyDefinitionId projectorSmall = MyVisualScriptLogicProvider.GetDefinitionId("MyObjectBuilder_Projector",
		    "SmallProjector");

        private readonly Dictionary<TechGroup, HashSet<MyDefinitionId>> techsForGroup =
            new Dictionary<TechGroup, HashSet<MyDefinitionId>>();

        private readonly QueuedAudioSystem audioSystem;

        internal ResearchControl(QueuedAudioSystem audioSystem)
        {
            this.audioSystem = audioSystem;
        }

	    internal HashSet<TechGroup> UnlockedTechs { get; set; } = new HashSet<TechGroup>();

        internal void InitResearchRestrictions()
        {
            NeedsResearch(refinery, TechGroup.Permabanned);
            NeedsResearch(blastFurnace, TechGroup.Permabanned);
            NeedsResearch(jumpDrive, TechGroup.Permabanned);
	        NeedsResearch(projectorLarge, TechGroup.Permabanned);
	        NeedsResearch(projectorSmall, TechGroup.Permabanned);
            NeedsResearch(largeMissileTurret, TechGroup.Rockets);
            NeedsResearch(smallMissileTurret, TechGroup.Rockets);
            NeedsResearch(rocketLauncher, TechGroup.Rockets);
            NeedsResearch(largeRocketLauncher, TechGroup.Rockets);
            NeedsResearch(smallReloadableRocketLauncher, TechGroup.Rockets);
            NeedsResearch(ionThrusterSmallShipSmall, TechGroup.Permabanned);
            NeedsResearch(ionThrusterSmallShipLarge, TechGroup.Permabanned);
            NeedsResearch(ionThrusterLargeShipSmall, TechGroup.Permabanned);
            NeedsResearch(ionThrusterLargeShipLarge, TechGroup.Permabanned);
            NeedsResearch(hydroThrusterSmallShipSmall, TechGroup.Permabanned);
            NeedsResearch(hydroThrusterSmallShipLarge, TechGroup.Permabanned);
            NeedsResearch(hydroThrusterLargeShipSmall, TechGroup.Permabanned);
            NeedsResearch(hydroThrusterLargeShipLarge, TechGroup.Permabanned);
            NeedsResearch(atmoThrusterSmallShipSmall, TechGroup.AtmosphericEngines);
            NeedsResearch(atmoThrusterSmallShipLarge, TechGroup.AtmosphericEngines);
            NeedsResearch(atmoThrusterLargeShipSmall, TechGroup.AtmosphericEngines);
            NeedsResearch(atmoThrusterLargeShipLarge, TechGroup.AtmosphericEngines);
            NeedsResearch(oxygenFarm, TechGroup.OxygenFarm);
            NeedsResearch(oxygenGeneratorLarge, TechGroup.OxygenGenerators);
            NeedsResearch(oxygenGeneratorSmall, TechGroup.OxygenGenerators);
            NeedsResearch(oxygenTankLarge, TechGroup.GasStorage);
            NeedsResearch(oxygenTankSmall, TechGroup.GasStorage);
            NeedsResearch(hydrogenTankLarge, TechGroup.GasStorage);
            NeedsResearch(hydrogenTankSmall, TechGroup.GasStorage);
        }

        private void NeedsResearch(MyDefinitionId techDef, TechGroup techgroup)
        {
            MyVisualScriptLogicProvider.ResearchListAddItem(techDef);

            HashSet<MyDefinitionId> techsInGroup;
            if (!techsForGroup.TryGetValue(techgroup, out techsInGroup))
            {
                techsInGroup = new HashSet<MyDefinitionId>();
                techsForGroup.Add(techgroup, techsInGroup);
            }
            techsInGroup.Add(techDef);
        }


        internal void UnlockTechGroupForAllPlayers(TechGroup techGroup)
        {
            if (UnlockedTechs.Contains(techGroup))
            {
                return; // Already unlocked
            }

	        HashSet<MyDefinitionId> technologies;
	        if(!techsForGroup.TryGetValue(techGroup, out technologies))
	        {
		        ModLog.Error("No technologies for group: " + techGroup);
		        return;
	        }

	        var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var player in players)
            {
	            foreach (var technology in technologies)
                {
                    MyVisualScriptLogicProvider.PlayerResearchUnlock(player.IdentityId, technology);
                }
            }
            UnlockedTechs.Add(techGroup);
            audioSystem.PlayAudio(GetAudioClipForTechGroup(techGroup));
        }

        private static CalAudioClip GetAudioClipForTechGroup(TechGroup techGroup)
        {
            switch (techGroup)
            {
                case TechGroup.Permabanned:
                    return CalAudioClip.AllTechUnlocked;
                case TechGroup.AtmosphericEngines:
                    return CalAudioClip.UnlockAtmospherics;
                case TechGroup.Rockets:
                    return CalAudioClip.UnlockedMissiles;
                case TechGroup.OxygenGenerators:
                    return CalAudioClip.OxygenGeneratorUnlocked;
                case TechGroup.OxygenFarm:
                    return CalAudioClip.OxygenFarmUnlocked;
                case TechGroup.GasStorage:
                    return CalAudioClip.GasStorageUnlocked;
                default:
                    return CalAudioClip.PowerUpClipped;
            }
        }

	    public void UnlockTechsSilently(long playerId, HashSet<TechGroup> techGroups)
	    {
		    foreach (var techGroup in techGroups)
		    {
			    var technologies = techsForGroup[techGroup];
			    if (technologies == null)
			    {
				    ModLog.Error("No technologies for group: " + techGroup);
				    return;
			    }

			    foreach (var technology in technologies)
			    {
				    MyVisualScriptLogicProvider.PlayerResearchUnlock(playerId, technology);
			    }
		    }
	    }


        public void UnlockTechForJoiningPlayer(long playerId)
        {
	        foreach (var techGroup in UnlockedTechs)
	        {
		        var technologies = techsForGroup[techGroup];
		        if (technologies == null)
		        {
			        ModLog.Error("No technologies for group: " + techGroup);
			        return;
		        }

		        foreach (var technology in technologies)
		        {
			        MyVisualScriptLogicProvider.PlayerResearchUnlock(playerId, technology);
		        }
	        }
        }
    }
}