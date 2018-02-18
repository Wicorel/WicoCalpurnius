using System.Collections.Generic;
using VRage.Game;

namespace Scenario.Calpurnius
{
	public class CalAudioClip : IAudioClip
	{
		private static int _nextId = 1;
		private static readonly Dictionary<int, CalAudioClip> Index = new Dictionary<int, CalAudioClip>();

		internal static readonly CalAudioClip Cavern = Create("Cavern", CalFactions.None, "", 4000);

		internal static readonly CalAudioClip ConvoyArrivedSafely = Create("ConvoyArrivedSafely", CalFactions.Gcorp,
			"*Muffled* Convoy arrived safely", 4000);

		internal static readonly CalAudioClip ConvoyDispatched1 = Create("ConvoyDispatched1", CalFactions.Gcorp,
			"*Muffled* Convoy dispatched", 7000);

		internal static readonly CalAudioClip ConvoyDispatched2 = Create("ConvoyDispatched2", CalFactions.Gcorp,
			"*Muffled* Convoy dispatched", 7000);

		internal static readonly CalAudioClip ConvoyDispatched3 = Create("ConvoyDispatched3", CalFactions.Gcorp,
			"*Muffled* Convoy dispatched", 6000);

		internal static readonly CalAudioClip ConvoyUnderThreat = Create("ConvoyUnderThreat", CalFactions.Gcorp,
			"Convoy under threat! Possible pirate activity", 4000);

		internal static readonly CalAudioClip DisengagingFromHostile = Create("DisengagingFromHostile",
		CalFactions.Gcorp,
			"Disengaging from hostile");

		internal static readonly CalAudioClip DroneDisarmed = Create("DroneDisarmed", CalFactions.Gcorp, 
			"Drone disarmed and returning for repairs");

		internal static readonly CalAudioClip EnemyDetectedMovingToIntercept = Create("EnemyDetectedMovingToIntercept", 
			CalFactions.Gcorp, "Enemy detected, moving to intercept");

		internal static readonly CalAudioClip FacilityDetectedHostile = Create("FacilityDetectedHostile",
			CalFactions.Gcorp, "Facility scanners have picked up a hostile. Dispatch nearby patrols to " +
			                              "location",
			 6000);

		internal static readonly CalAudioClip GCorpFacilitiesHeavilyArmed = Create("GCorpFacilitiesHeavilyArmed",					
			CalFactions.Player,
			@"Scanning shows G-Corp facilities in the area are carefully guarded, approaching one is not recommended
unless heavily armed yourself",
			8000);

		internal static readonly CalAudioClip GCorpFacilityThreatened = Create("GCorpFacilityThreatened",
		CalFactions.Gcorp,
			"G-Corp facility threatened, dispatching additional drones");

		internal static readonly CalAudioClip HostileDisappeared = Create("HostileDisappeared", CalFactions.Gcorp,
			"Hostile has disappeared from scanners. Resume patrol", 5000);

		internal static readonly CalAudioClip HostileStillPresent = Create("HostileStillPresent", CalFactions.Gcorp,
			"Hostile still present. Requesting reinforcements immediately", 5000);

		internal static readonly CalAudioClip MilitaryPatrolInitiated = Create("MilitaryPatrolInitiated",
		CalFactions.Gcorp, "Military patrol initiated, searching for hostiles");

		internal static readonly CalAudioClip OxygenGeneratorUnlocked = Create("OxygenGeneratorUnlocked",
		CalFactions.Player, "Searching G-Corp data files. Oxygen generator technology unlocked!", 5000);

		internal static readonly CalAudioClip PowerUpClipped = Create("PowerUpClipped", CalFactions.None, "");

		internal static readonly CalAudioClip TargetFleeingPursuit = Create("TargetFleeingPursuit", CalFactions.Gcorp,	// USED
			"Target fleeing pursuit, drones return to base");

		internal static readonly CalAudioClip TargetFoundDronesAttack = Create("TargetFoundDronesAttack",
		CalFactions.Gcorp, "Target found, all drones attack!");

		internal static readonly CalAudioClip TargetIdentifiedUnitsConverge = Create("TargetIdentifiedUnitsConverge",
			CalFactions.Gcorp, "Target identified, all units converge!");

		internal static readonly CalAudioClip TargetLost = Create("TargetLost", CalFactions.Gcorp,
			"Target lost, return to positions");

		internal static readonly CalAudioClip UnknownHostileOnScanners = Create("UnknownHostileOnScanners",
		CalFactions.Gcorp, "Unknown hostile showing up on scanners. Engaging");

		internal static readonly CalAudioClip UnlockAtmospherics = Create("UnlockAtmospherics", CalFactions.Player, 
			"Searching G-Corp data files. Atmospheric thruster technology unlocked!", 5000);

		internal static readonly CalAudioClip UnlockedMissiles = Create("UnlockedMissiles", CalFactions.Player,
			"Searching G-Corp data files. Missile technology unlocked!", 5000);

		internal static readonly CalAudioClip OxygenFarmUnlocked = Create("OxygenFarmUnlocked", CalFactions.Player,
				"Searching data files. Oxygen Farm technology unlocked!", 5000);

		internal static readonly CalAudioClip GasStorageUnlocked = Create("GasStorageUnlocked", CalFactions.Player,
			"Searching G-Corp data files. Gas storage technology unlocked.", 4000);

		internal static readonly CalAudioClip ExplosivesNearby = Create("ExplosivesNearby", CalFactions.Player,
			"I am detecting explosive compounds nearby.", 3000);

		internal static readonly CalAudioClip PowerSignatureBehindWall = Create("PowerSignatureBehindWall",
		CalFactions.Player, "There is a power signature coming from behind one of the walls.", 3000);

		internal static readonly CalAudioClip AllTechUnlocked = Create("AllTechUnlocked", CalFactions.Player,
	        "All technologies unlocked.", 2000);

		internal static readonly CalAudioClip FaintPowerSignature = Create("FaintPowerSignature", CalFactions.Player,
			"I am detecting a faint power signature...", 2000);

		internal static readonly CalAudioClip PursuitEvaded = Create("PursuitEvaded", CalFactions.Gcorp,
			"Pursuit evaded, resuming standard course and heading", 3000);

		internal static readonly CalAudioClip SensorsLostTrack = Create("SensorsLostTrack", CalFactions.Gcorp,
			"Sensors lost track of hostile", 2000);

		internal static readonly CalAudioClip HackingSound = Create("HackingSound", CalFactions.None,"", 14000);

		internal static readonly CalAudioClip ConnectionLostSound = Create("ConnectionLostSound", CalFactions.None,"", 2000);

		internal static readonly CalAudioClip HackFinished = Create("HackFinished", CalFactions.None, "", 2000);

		internal static readonly CalAudioClip BestCustomer = Create("BestCustomer", CalFactions.Miki,
			@"You are best customer all year! ... Only customer all year.", 6000);

		internal static readonly CalAudioClip DontBreatheIn = Create("DontBreatheIn", CalFactions.Miki,
			"Best not to ... breathe in fumes when we melt this.", 4000);

		//TODO needs replacement for Calpurnius
		internal static readonly CalAudioClip GreetingsMartianColonists = Create("GreetingsMartianColonists",
		CalFactions.Miki,
			@"Greetings Martian Colonist! Miki Scrap is now open for all recycling needs.
			You have old junk, scrap metal? We give new, better things in return.
			Just follow antenna signal!", 16000);

		internal static readonly CalAudioClip LavaLoop = Create("LavaLoop", CalFactions.None,
			"[Bubbling furnace sounds]", 75000);

		internal static readonly CalAudioClip NewMikiScrapsOpen = Create("NewMikiScrapsOpen", CalFactions.Miki,
			"New Miki Scraps open all the time! Look for us on other planets ... or we come look for you.", 8000);

		internal static readonly CalAudioClip PartOfBuilding = Create("PartOfBuilding", CalFactions.Miki,
			"What is that, part of building!? Get it out of here!", 4000);

		internal static readonly CalAudioClip TellAllFriends = Create("TellAllFriends", CalFactions.Miki,
			"Remember, tell all colonist friends about Miki Scrap!", 4000);

		internal static readonly CalAudioClip ThisIsGoodScrap = Create("ThisIsGoodScrap", CalFactions.Miki,
			"This is good scrap! We melt down for you.", 4000);

		internal static readonly CalAudioClip TiredOfGrindingCrap = Create("TiredOfGrindingCrap", CalFactions.Miki,
			"Tired of grinding crap? We can do that! Miki Scrap.", 4000);

		internal static readonly CalAudioClip WeCrushDown = Create("WeCrushDown", CalFactions.Miki,
			"We crush down to little cubes for you!", 3000);

		internal static readonly CalAudioClip WelcomeMikiScrap = Create("WelcomeMikiScrap", CalFactions.Miki,
			"Welcome to Miki Scrap!", 2000);

		internal static readonly CalAudioClip WhereDoYouGetScrapMetal = Create("WhereDoYouGetScrapMetal",
		CalFactions.Miki,
			"Where do you get all this scrap metal!? I've never seen so much!", 5000);

		internal static readonly CalAudioClip WhereIsThatFrom = Create("WhereIsThatFrom", CalFactions.Miki,
			"Where is that from? Nevermind! We make disappear for you.", 6000);
		
		private static CalAudioClip Create(string subTypeName, IFaction faction, string subtitle, int disappearTimeMs = 4200)
		{
			var id = _nextId++;
			var clip = new CalAudioClip(id, subTypeName, faction, subtitle, disappearTimeMs);
			Index.Add(id, clip);
			return clip;
		}

		public static CalAudioClip GetClipFromId(int id)
		{
			return Index[id];
		}

		public string Filename { get; }
		public string Subtitle { get; }
		public string Speaker { get; }
		public MyFontEnum Font { get; }
		public int DisappearTimeMs { get; }
		public int Id { get; }

		private CalAudioClip(int id, string filename, IFaction faction, string subtitle, int disappearTimeMs)
		{
			Id = id;
			DisappearTimeMs = disappearTimeMs;
			Speaker = faction.SubtitlesSpeaker;
			Font = faction.SubtitlesFont;
			Filename = filename;
			Subtitle = subtitle;
		}
	}
}