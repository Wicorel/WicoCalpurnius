using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace Scenario.Calpurnius
{
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class CalCore : AbstractCore<SaveData>
	{
		// Current mod version, increased each time before workshop publish
		private const int CurrentModVersion = 1;

		private readonly QueuedAudioSystem audioSystem = new QueuedAudioSystem();
		private readonly HeatSystem heatSystem = new HeatSystem(-7);//TODO remove this from here, have some kind of observer for the faction owner instead
		private readonly NetworkComms networkComms = new NetworkComms();
		private readonly TurretManager turretManager = new TurretManager();
		private MikiScrapManager mikiScrapManager;
		private ResearchControl researchControl;
		private CalMissionSystem missionSystem;
		private ConvoySpawner convoySpawner;
		private HUDTextAPI hudTextApi;
		private ResearchHacking researchHacking;
		private int modBuildWhenGameStarted;
		private NpcGroupManager npcGroupManager;
		private BaseManager baseManager;
		private DustStorm dustStorm;

		protected override void InitCommon(IModSystemRegistry modSystemRegistry)
		{
			MyAPIGateway.Utilities.ShowNotification("Initialising Calpurnius build " + CurrentModVersion, 10000,
				MyFontEnum.DarkBlue);
			hudTextApi = new HUDTextAPI(11873852597);
			researchControl = new ResearchControl(audioSystem);
			researchControl.InitResearchRestrictions();
			researchHacking = new ResearchHacking(researchControl, hudTextApi, networkComms);
			networkComms.Init(audioSystem, researchControl, researchHacking);
			modSystemRegistry.AddCloseableModSystem(hudTextApi);
			modSystemRegistry.AddCloseableModSystem(networkComms);
			modSystemRegistry.AddUpatableModSystem(audioSystem);
			CalFactions.Player.JoinCurrentPlayerToFaction();
		}

		protected override void InitHostPreLoading()
		{
			if (MyAPIGateway.Session == null)
				return;
			mikiScrapManager = new MikiScrapManager(audioSystem);
			baseManager = new BaseManager(audioSystem);
			convoySpawner = new ConvoySpawner(audioSystem);
			npcGroupManager = new NpcGroupManager(heatSystem, audioSystem, baseManager);
		}
		
		protected override void InitHostPostLoading(IModSystemRegistry modSystemRegistry)
		{
			researchHacking.InitHackingLocations(); // Uses research restrictions and save data

			CalFactions.Init();
			audioSystem.AudioRelay = networkComms;
			networkComms.StartWipeHostToolbar();
			modSystemRegistry.AddRapidUpdatableModSystem(turretManager);
			modSystemRegistry.AddUpatableModSystem(researchHacking);
			modSystemRegistry.AddUpatableModSystem(missionSystem); // mabel
			modSystemRegistry.AddUpatableModSystem(mikiScrapManager);
			modSystemRegistry.AddUpatableModSystem(npcGroupManager);
			modSystemRegistry.AddUpatableModSystem(baseManager);
			modSystemRegistry.AddUpatableModSystem(convoySpawner);

            // turn OFF dust storms with commented out code.
	//		dustStorm = new DustStorm();
	//		modSystemRegistry.AddUpatableModSystem(dustStorm);
		}

		public override void Draw()
		{
			dustStorm?.Draw();
			
		}

		protected override void InitClient(IModSystemRegistry modSystemRegistry)
		{
			var player = MyAPIGateway.Session.Player;
			if (player != null)
			{
				networkComms.NotifyServerClientJoined(player);
			}
		}

		private const string SaveFileName = "EFM-SaveData.xml";

		public override string GetSaveDataFileName()
		{
			return SaveFileName;
		}

		public override SaveData GetSaveData()
		{
			if (modBuildWhenGameStarted == 0)
			{
				modBuildWhenGameStarted = CurrentModVersion;
			}
			
			var saveData = new SaveData
			{
				HeatLevel = heatSystem.HeatLevel,
				UnlockedTechs = researchControl.UnlockedTechs,
				NpcGroupSaveDatas = npcGroupManager.GetSaveData(),
				NextSpawnTime = convoySpawner.GetNextSpawnTimeForSaving(),
				MissionStartTimeBinary = missionSystem.GetMissionStartTimeBinary(),
				ExcludedMissionPrompts = missionSystem.GetExcludedIDs(),
				RegisteredPlayers = networkComms.RegisteredPlayers,
				HackingData = researchHacking.GetSaveData(),
				BuildWhenGameStarted = modBuildWhenGameStarted,
                //TODO: Add per-faction bases
				GCorpBaseSaveDatas = baseManager.GetSaveData(),
				MikiScrapSaveDatas = mikiScrapManager.GetSaveDatas()
			};
			return saveData;
		}

		public override void LoadPreviousGame(SaveData saveData)
		{
			networkComms.RegisteredPlayers = saveData.RegisteredPlayers;
			heatSystem.HeatLevel = saveData.HeatLevel;
			researchControl.UnlockedTechs = saveData.UnlockedTechs;
			npcGroupManager.LoadSaveData(saveData.NpcGroupSaveDatas);
			convoySpawner.RestoreSpawnTimeFromSave(saveData.NextSpawnTime);
			missionSystem = new CalMissionSystem(saveData.MissionStartTimeBinary, saveData.ExcludedMissionPrompts,
				audioSystem, researchControl);
			researchHacking.RestoreSaveData(saveData.HackingData);
			modBuildWhenGameStarted = saveData.BuildWhenGameStarted;
            //TODO: Add per-faction bases
			baseManager.LoadSaveData(saveData.GCorpBaseSaveDatas);
			mikiScrapManager.LoadSaveData(saveData.MikiScrapSaveDatas);
		}

		public override void StartedNewGame()
		{
			missionSystem = new CalMissionSystem(MyAPIGateway.Session.GameDateTime.ToBinary(), new HashSet<int>(),
				audioSystem, researchControl);
			modBuildWhenGameStarted = CurrentModVersion;
		}
	}

	public class SaveData
	{
		public int HeatLevel { get; set; }
		public HashSet<TechGroup> UnlockedTechs { get; set; }
		public List<NpcGroupSaveData> NpcGroupSaveDatas { get; set; }
		public long NextSpawnTime { get; set; }
		public long MissionStartTimeBinary { get; set; }
		public HashSet<int> ExcludedMissionPrompts { get; set; }
		public HashSet<long> RegisteredPlayers { get; set; }
		public List<ResearchHacking.HackingSaveData> HackingData { get; set; }
		public int BuildWhenGameStarted { get; set; }

        //TODO: Add per-faction bases
		public List<GCorpBaseSaveData> GCorpBaseSaveDatas { get; set; }
		public List<MikiScrapSaveData> MikiScrapSaveDatas { get; set; }
	}
}