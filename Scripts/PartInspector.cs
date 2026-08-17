using Ceres.PartInspector.Trackers;
using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Spare_Parts;
using static Ceres.PartInspector.Trackers.FreshnessTracker;
using static Ceres.PartInspector.Trackers.FullnessTracker;
using static Ceres.PartInspector.Trackers.VariantTracker;

namespace Ceres.PartInspector
{
	/// <summary>
	/// Core mod script. This does essentially all of the hard work.
	/// The way Part Inspector's backend works boils down, roughly, to the following list of essential points:
	/// <br/><br/>
	/// <b>1.</b> Determine if parts should have a tracker assigned via heuristic or if it's manually defined in <c><see cref="_partNames"/></c>.<br/>
	/// <b>2.</b> Instantiate a new tracker on that game object. These are Unity components (i.e. MonoBehaviors) that inherit from <c><see cref="BaseTracker"/></c>.<br/>
	/// <b>3.</b> Whenever the player looks at a part, give it a tracker if it needs one, and then read data from its assigned tracker and update the display text.<br/><br/>
	/// That's it! This takes a lot of code to handle and set up, but in practice it's mostly clean.
	/// There is a lot of caveats and conditionals here, primarily because MSC and MWC have divergent codebases that often track information in completely different ways.
	/// </summary>
	public class PartInspectorScript : Mod
	{
		#region Metadata
		public override string ID => "Ceres_PartInspector";
		public override string Name => "Part Inspector";
		public override string Author => "Ceres et al.";
		public override string Version => "0.2.0";
		public override string Description => "Inspect your parts! (And containers and filters and bolts and…)";
		public override Game SupportedGames => Game.MySummerCar_And_MyWinterCar;
		#endregion

		#region Mod setup and settings
		internal static SettingsDropDownList SettingDisplayLocation;
		internal static SettingsDropDownList SettingDisplayPrecision;
		internal static SettingsDropDownList SettingItemDisplayPrecision;
		internal static SettingsDropDownList SettingBoltSizePrecision;
		internal static SettingsSliderInt SettingTextUpdateFrequency;

		internal static SettingsCheckBox SettingShowCarPartCondition;
		internal static SettingsCheckBox SettingShowContainerFullness;
		internal static SettingsCheckBox SettingShowPackageQuantity;
		internal static SettingsCheckBox SettingShowFoodFreshness;
		internal static SettingsCheckBox SettingShowObjectVariants;
		internal static SettingsCheckBox SettingShowBoltSizes;
		internal static SettingsCheckBox SettingShowValveClearance;
		internal static SettingsCheckBox SettingShowSuspensionTuning;

		internal static SettingsCheckBox SettingLogVerification;
		internal static SettingsCheckBox SettingLogNewTrackers;
		internal static SettingsCheckBox SettingLogBoltSize;

		/// <summary>
		/// Because a lot of <see cref="PrintToConsole(object, ConsoleMessageScope)"/> calls happen every frame,
		/// we use these variables to cache the values of their respective settings, rather than getting the setting's value every time.<br/><br/>
		/// (programmer's note: I don't actually have any idea if it's cheaper to cache locally than it is to call the bool getter on the setting itself.
		/// I figure that there's no harm in it, though!)
		/// </summary>
		internal static bool _logVerification, _logNewTrackers, _logBoltSize;

		/// <summary>
		/// This gets checked a lot, so we cache it in OnLoad() for performance
		/// </summary>
		public static bool IsMSC;

		public static bool IsModLoaded_SpareParts;

		public override void ModSetup()
		{
			SetupFunction(Setup.OnLoad, Mod_OnLoad);
			SetupFunction(Setup.Update, Mod_OnUpdate);
			SetupFunction(Setup.ModSettings, Mod_Settings);
		}

		private void Mod_Settings()
		{
			Color headingColor = new Color(0.1f, 0.1f, 0.1f);

			Settings.AddHeader("Trackers", headingColor, Color.white);
			SettingShowCarPartCondition = Settings.AddCheckBox(nameof(SettingShowCarPartCondition), "Show car part condition", true);
			Settings.AddText("Includes every car part that can wear down, get dirty, or be broken.");
			SettingShowContainerFullness = Settings.AddCheckBox(nameof(SettingShowContainerFullness), "Show container fullness", true);
			Settings.AddText("Includes fluids (motor oil, coolant, etc.) as well as solids (ground coffee and grill charcoal).");
			SettingShowPackageQuantity = Settings.AddCheckBox(nameof(SettingShowPackageQuantity), "Show package quantity", true);
			Settings.AddText("For spark plugs, fuses, etc. Displays the amount left in the package.");
			SettingShowFoodFreshness = Settings.AddCheckBox(nameof(SettingShowFoodFreshness), "Show food freshness", false);
			Settings.AddText("Shows freshness for any food that is capable of spoiling.");
			if (ModLoader.CurrentGame == Game.MyWinterCar)
			{
				SettingShowObjectVariants = Settings.AddCheckBox(nameof(SettingShowObjectVariants), "Show object variants", true);
				Settings.AddText("A part's variant will be shown in its display name. For things like instrument panels, grilles, and brake lines.");
				SettingShowBoltSizes = Settings.AddCheckBox(nameof(SettingShowBoltSizes), "Show bolt sizes", false,
					() => _showBoltSizes = SettingShowBoltSizes.GetValue());
				Settings.AddText("When in tool mode, shows the size of whatever bolt you're looking at.");
			}

			Settings.AddHeader("Interface", headingColor, Color.white);
			Settings.AddText("Some of these settings won't do anything without specific trackers being enabled!");
			SettingDisplayLocation = Settings.AddDropDownList(nameof(SettingDisplayLocation), "Where to display information",
				new string[] { "In the item's name (recommended)", "In interaction text" }, 0, RefreshDisplayGUI);
			SettingDisplayPrecision = Settings.AddDropDownList(nameof(SettingDisplayPrecision), "Part inspection precision",
				new string[] { "Show exact information", "Show general description", "Show broken/not broken" }, 1);
			SettingItemDisplayPrecision = Settings.AddDropDownList(nameof(SettingItemDisplayPrecision), "Item inspection precision",
				new string[] { "Show exact information", "Show general description" }, 1);
			SettingBoltSizePrecision = Settings.AddDropDownList(nameof(SettingBoltSizePrecision), "Bolt size precision",
				new string[] { "Show exact information", "Show general description", "Show too big/too small only" }, 1,
				() => _boltSizeMode = SettingBoltSizePrecision.GetSelectedItemIndex());
			SettingTextUpdateFrequency = Settings.AddSlider(nameof(SettingTextUpdateFrequency), "Text update frequency<color=yellow>*</color>",
				1, 10, 10, RebuildDisplays);
			Settings.AddText("<color=yellow>* Lowering this might have an impact on performance. " +
				"Only use it if you find the default rate to be too sluggish.</color>");

			Settings.AddHeader("Logging", headingColor, Color.white);
			Settings.AddText("If you're running into bugs, these settings will put extra info into your log that'll help the author diagnose the issues. Keep them all off for regular play, but please turn on the relevant ones when submitting a bug report!");
			SettingLogNewTrackers = Settings.AddCheckBox(nameof(SettingLogNewTrackers), "Log new trackers", false,
				() => _logNewTrackers = SettingLogNewTrackers.GetValue());
			SettingLogVerification = Settings.AddCheckBox(nameof(SettingLogVerification), "Log object verification <color=yellow>(warning: spammy)</color>", false,
				() => _logVerification = SettingLogVerification.GetValue());
			SettingLogBoltSize = Settings.AddCheckBox(nameof(SettingLogBoltSize), "Log bolt size inspection <color=red>(warning: makes tool mode lag a lot)</color>", false,
				() => _logBoltSize = SettingLogBoltSize.GetValue());

			Settings.AddHeader("Experimental", Color.red, Color.white, true);
			Settings.AddText("<color=yellow>The following options are <b>experimental</b> and not intended for regular play. They may or may not work correctly. Use at your own risk - no support will be provided.</color>");
			if (ModLoader.CurrentGame == Game.MySummerCar)
			{
				SettingShowBoltSizes = Settings.AddCheckBox(nameof(SettingShowBoltSizes), "Show bolt sizes", false,
					() => _showBoltSizes = SettingShowBoltSizes.GetValue());
				Settings.AddText("When in tool mode, shows the size of whatever bolt you're looking at.");
			}
			SettingShowValveClearance = Settings.AddCheckBox(nameof(SettingShowValveClearance), "Show valve lash", false,
				() => _showValveClearance = SettingShowValveClearance.GetValue());
			Settings.AddText("When tuning rocker valves, displays their clearance. Useful for setting specific values without requiring a save editor.");
			SettingShowSuspensionTuning = Settings.AddCheckBox(nameof(SettingShowSuspensionTuning), "Show rally suspension tuning", false,
				() => _showSuspensionTuning = SettingShowSuspensionTuning.GetValue());
			Settings.AddText("Shows the percentage of bump/rebound tuning on rally suspensions. (You can already see the position of the knob for these; this just makes it quick and easy, instead of having to measure the amount of ticks from max you are.)");
		}
		#endregion

		#region Cached vars
		/// <summary>
		/// Cached reference to the interaction GUI global FSM.
		/// </summary>
		private FsmString _interactionGui;

		/// <summary>
		/// Cached reference to the item name display global FSM.
		/// </summary>
		private FsmString _pickedPartGui;

		/// <summary>
		/// Cached reference to the FSM used to track the object the player is currently looking at.
		/// We use this instead of <see cref="UnifiedRaycast"/> because it lets us benefit from the game's own logic
		/// on determining what object's name should be displaying, which the unified raycast does not.
		/// </summary>
		private FsmGameObject _plyCamObject;

		/// <summary>
		/// Cached reference to the value of <see cref="SettingShowBoltSizes"/>.
		/// </summary>
		private bool _showBoltSizes;

		/// <summary>
		/// Cached reference to the value of <see cref="SettingShowValveClearance"/>.
		/// </summary>
		private bool _showValveClearance;
		
		/// <summary>
		/// Cached reference to the value of <see cref="SettingShowSuspensionTuning"/>.
		/// </summary>
		private bool _showSuspensionTuning;

		/// <summary>
		/// Cached reference to the value of <see cref="SettingBoltSizePrecision"/>.
		/// </summary>
		private int _boltSizeMode = 3;

		#region Bolt inspection
		/// <summary>
		/// Cached reference to whether or not the player is in tool mode.
		/// </summary>
		private FsmBool _toolMode;

		/// <summary>
		/// Cached reference to the size of the currently equipped tool.
		/// </summary>
		private FsmFloat _curWrenchSize;

		/// <summary>
		/// Cached reference to whatever bolt the player is looking at.
		/// </summary>
		private FsmGameObject _curBolt;

		/// <summary>
		/// Cached reference to the bolt we're showing the size of.
		/// </summary>
		private GameObject _lastBoltInspected;

		/// <summary>
		/// Calculated display text for the bolt we're looking at.
		/// Caching this means we don't have to do a bunch of math every frame.
		/// </summary>
		private string _boltSizeText;
		#endregion

		#region MSC-exclusive
		/// <summary>
		/// MSC-exclusive. The cleanest way to track bolt size is to view this variable, which is attached to the wrench itself.
		/// </summary>
		private FsmFloat _curBoltSizeMSC;

		/// <summary>
		/// Only needed for MSC, which tracks all of its part wear data as an FSM attached to the Satsuma itself.
		/// </summary>
		private FsmVariables _mscSatsumaVars;

		/// <summary>
		/// Only needed for MSC, which tracks part installation with these lists.
		/// </summary>
		private List<PlayMakerFSM> _mscMotorDb;
			#endregion

		#endregion

		#region Internal vars
		/// <summary>
		/// Used to designate the type of wear tracker a given part should receive, mostly through assignment in <see cref="_partNames"/>.
		/// </summary>
		private enum TrackerType
		{
			/// <summary>
			/// Tracks a part's wear value, from 100 (max condition) to 0 (broken).<br/><br/>
			/// This type has very different behavior depending on the game:<br/>
			/// <b>MSC:</b> Condition is held in specific variables using <c><see cref="_mscSatsumaVars"/></c>.<br/>
			/// <b>MWC:</b> Condition is held on an FSM on the object itself, so we can simply reference that.
			/// </summary>
			PartCondition,

			/// <summary>
			/// Does exactly what it says on the tin. This is used for blocks and oilpans.
			/// </summary>
			IntactOrBroken,

			/// <summary>
			/// Oil filters track dirtiness as an ascending value rather than a descending one, so they need their own type.
			/// </summary>
			OilFilter,

			/// <summary>
			/// Flexible tracker type used for anything that gradually decreases in amount as it's used. Mostly fluids, but also stuff like ground coffee.<br/>
			/// <b>Associated data struct:</b> <c><see cref="FullnessInfo"/></c>
			/// </summary>
			Fullness,

			/// <summary>
			/// Used for packages that contain a set number of items, like fuses and battery boxes.<br/>
			/// </summary>
			Quantity,

			/// <summary>
			/// Used for food that goes bad over time.<br/>
			/// <b>Associated data struct:</b> <c><see cref="FreshnessInfo"/></c>
			/// </summary>
			Freshness,

			/// <summary>
			/// <b>MSC only:</b> Spark plugs in this game function like regular car parts do in MWC, and so they need their own tracker type to handle it.
			/// TODO: Generalize this.
			/// </summary>
			MSC_SparkPlug,

			/// <summary>
			/// <b>MWC only:</b> Used for items with multiple variants, like grilles and mufflers.
			/// </summary>
			MWC_Variant,

			/// <summary>
			/// <b>MWC only:</b> Combines the behavior of the <c><see cref="MWC_Variant"/></c> and <c><see cref="PartCondition"/></c> trackers.
			/// </summary>
			MWC_VariantAndWear,

			/// <summary>
			/// <b>Spare Parts mod:</b> Tracks part condition for spare parts, among other things depending on subtype.
			/// </summary>
			MOD_SparePart,
		}

		/// <summary>
		/// Game objects with names in the keys of this dict will gain a wear tracker component when inspected, if they don't have one already, with some of that component's info being taken from the associated value of that key.
		/// <br/><br/>
		/// Names with an associated string will be given a <see cref="PartConditionTracker"/> using that string as the wear key; names with an associated <see cref="TrackerType"/> will instead use that type when creating the tracker.
		/// </summary>
		// mmmmm yummy pasta
		private readonly Dictionary<string, object> _partNames = new Dictionary<string, object>
		{
			{ "brake fluid(itemx)", new FullnessInfo("Data", MaxValue: 1f, DisplayAsFluid: true, ChildObjectName: "BrakeFluidTrigger" ) },
			{ "two stroke fuel(itemx)", new FullnessInfo("Data", MaxValue: 5f, DisplayAsFluid: true, ChildObjectName: "TwoStrokeTrigger" ) },
			{ "motor oil(itemx)", new FullnessInfo("Data", MaxValue: 4f, DisplayAsFluid: true, ChildObjectName: "MotorOilTrigger" ) },
			{ "coolant(itemx)", new FullnessInfo("Data", MaxValue: 10f, DisplayAsFluid: true, ChildObjectName: "CoolantTrigger" ) },

			{ "spray can(itemx)", new FullnessInfo(MaxValue: 100f ) },
			{ "mosquito spray(itemx)", new FullnessInfo(MaxValue: 100f ) },


			{ "ground coffee(itemx)", new FullnessInfo(ValueKey: "Ground", MaxValue: 100f, MinValue: 1 ) },
			{ "grill charcoal(itemx)", new FullnessInfo(ValueKey: "Contents", MaxValue: 140f, MinValue: 1 ) },

			{ "spark plug box(Clone)", TrackerType.Quantity },
			{ "r20 battery box(Clone)", TrackerType.Quantity },
			{ "fuse package(Clone)", TrackerType.Quantity },

			{ "sausages(itemx)", new FreshnessInfo(MaxFreshness: 100) },
			{ "pizza(itemx)", new FreshnessInfo(MaxFreshness: 100) },
			{ "macaron box(itemx)", new FreshnessInfo(MaxFreshness: 100) },
			{ "milk(itemx)", new FreshnessInfo(MaxFreshness: 100) },
			{ "pike(itemx)", new FreshnessInfo(MaxFreshness: 40) },

			#region Parts specific to MSC
			{ "fire extinguisher(itemx)", new FullnessInfo("Use", MaxValue: 100f ) },
			{ "spark plug(Clone)", TrackerType.MSC_SparkPlug },
			{ "oil filter(Clone)", TrackerType.OilFilter },

			{ "block(Clone)", TrackerType.IntactOrBroken },
			{ "oilpan(Clone)", TrackerType.IntactOrBroken },

			{ "alternator(Clone)", "Alternator" },
			{ "clutch disc(Clone)", "Clutch" },
			{ "crankshaft(Clone)", "Crankshaft" },
			{ "fuel pump(Clone)", "Fuelpump" },
			{ "gearbox(Clone)", "Gearbox" },
			{ "head gasket(Clone)", "Headgasket" },
			{ "piston1(Clone)", "Piston1" },
			{ "piston2(Clone)", "Piston2" },
			{ "piston3(Clone)", "Piston3" },
			{ "piston4(Clone)", "Piston4" },
			{ "rocker shaft(Clone)", "Rockershaft" },
			{ "starter(Clone)", "Starter" },
			{ "water pump(Clone)", "Waterpump" },
			#endregion
			
			#region Parts specific to MWC
			{ "automatic transmission fluid(itemx)", new FullnessInfo("Data", MaxValue: 1f, DisplayAsFluid: true, ChildObjectName: "ATFOilTrigger" ) },
			{ "package(Clone)", TrackerType.Quantity }, // for parts purchased from fleetari. not the most descriptive name in the world, huh?

			// these are here because carbs track their wear using a DIFFERENT SCHEMA THAN EVERY OTHER PART WHYYY-
			{ "Carburettor(VINXX)", TrackerType.PartCondition },
			{ "2 Barrel Carb(VINXX)", TrackerType.PartCondition },
			{ "4 Barrell Racing Carb(VINXX)", TrackerType.PartCondition },

			{ "Engine Block(VINX0)", TrackerType.IntactOrBroken },
			{ "Oilpan(VINXX)", TrackerType.IntactOrBroken },

			{ "Fire Extinguisher(VINXX)", new FullnessInfo("Data", MaxValue: 100f ) },
			{ "Oil filter(VINXX)", TrackerType.OilFilter },

			{ "Brake Lines(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 1, "Standard Brakes" }, { 2, "Power Brakes" }
			}, typeof(int) ) },

			{ "Brake Master Cylinder(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 1, "Standard Brakes" }, { 2, "Power Brakes" }
			}, typeof(int), AlsoTracksWear: true ) },

			{ "Exhaust Pipe Front(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ "ALL", "Standard" }, { "GT", "GT" }
			}, typeof(string), "Code" ) },

			{ "Bootlid(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 0, "L/Pre-Facelift GT" }, { 1, "LX/SLX" }, { 2, "Facelift GT" }
			}, typeof(int) ) },

			{ "Instrument Panel(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ "A", "Standard" },
				{ "B", "Clock" },
				{ "C", "Tachometer" },
			}, typeof(string), "Model" ) },

			{ "Grille(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ "ALL", "Facelift" },
				{ "L", "L/GT" },
				{ "LX", "LX" },
				{ "SLX", "SLX" },
			}, typeof(string), "Code" ) },

			{ "Bumper(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ "A", "Pre-Facelift" },
				{ "B", "Facelift" },
				{ "GT", "GT" },
			}, typeof(string), "Code" ) },
			#endregion
		};

		/// <summary>
		/// Every wear tracker in the game world, associated to its game object.
		/// </summary>
		private Dictionary<GameObject, BaseTracker> _allTrackers;

		/// <summary>
		/// Objects in this list can never have a tracker added, no matter what.
		/// In normal conditions, objects only enter this list if they throw an error while having a tracker added to them!
		/// </summary>
		private HashSet<GameObject> _blacklistedObjects;

		/// <summary>
		/// The text GUI used to display the part's condition. This will always point to either <see cref="_pickedPartGui"/> or <see cref="_interactionGui"/>,
		/// depending on the value of <see cref="SettingDisplayLocation"/>.
		/// </summary>
		private FsmString _displayGui;

		/// <summary>
		/// To save performance, and because parts are unlikely to rapidly change condition in a given time period, display names only update every few seconds. This value tracks how often parts update, in seconds.
		/// <br/><br/>
		/// Certain trackers, like fullness trackers, will force early updates if they detect changes in their item's state. This keeps them nice and responsive.
		/// </summary>
		private float _timeBetweenUpdates = 10f;

		/// <summary>
		/// How many seconds have elapsed since we last updated displays. See <see cref="_timeBetweenUpdates"/> for more info.
		/// </summary>
		private float _timeSinceLastUpdate = 0f;
		#endregion

		#region Debug
		internal enum ConsoleMessageScope
		{
			Core, // Core logic that we always log
			NewTrackers, // Whenever a new tracker is created
			Verification, // Detailed steps for detecting if a given object is a valid part
			BoltInspection, // Step-by-step process for viewing bolts
		}

		internal static void PrintToConsole(object Message, ConsoleMessageScope Context)
		{
			if (Context == ConsoleMessageScope.Verification && !_logVerification)
				return;
			if (Context == ConsoleMessageScope.NewTrackers && !_logNewTrackers)
				return;
			if (Context == ConsoleMessageScope.BoltInspection && !_logBoltSize)
				return;
			ModConsole.Print($"[PI] {Message}");
		}
		#endregion

		#region Main functions
		private void Mod_OnLoad()
		{
			try
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				int modsWithCompat = 0; // so the list could theoretically be expanded, though I don't think it's likely atm :P
				IsMSC = ModLoader.CurrentGame == Game.MySummerCar;
				if (IsMSC && ModLoader.IsModPresent("Spare_Parts"))
				{
					modsWithCompat++;
					IsModLoaded_SpareParts = true;
				}

				PrintToConsole($"{Name} version {Version} is now initializing for My {(IsMSC ? "Summer" : "Winter")} car.", ConsoleMessageScope.Core);
				if (modsWithCompat > 0)
				{
					PrintToConsole($"Detected {modsWithCompat} mod{(modsWithCompat == 1 ? "" : "s")} with native compatibility:", ConsoleMessageScope.Core);
					if (IsModLoaded_SpareParts)
					{
						var v = ModLoader.GetModVersionByID("Spare_Parts");
						PrintToConsole($"- Spare Parts version {v}", ConsoleMessageScope.Core);
					}
				}
				else
					PrintToConsole($"No mods with integration are loaded.", ConsoleMessageScope.Core);

				PrintToConsole("Caching global objects and variables…", ConsoleMessageScope.Core);
				FsmVariables plyCam = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/Hand").GetPlayMaker("PickUp").FsmVariables;
				_plyCamObject = plyCam.GetFsmGameObject("RaycastHitObject");
				_toolMode = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerHandRight");
				_interactionGui = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
				_pickedPartGui = PlayMakerGlobals.Instance.Variables.FindFsmString("PickedPart");
				GameObject wrenchRaycast = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/2Spanner/Raycast");
				_curWrenchSize = PlayMakerGlobals.Instance.Variables.FindFsmFloat("ToolWrenchSize");
				_curBolt = wrenchRaycast.GetPlayMaker("Raycast").FsmVariables.GetFsmGameObject("Bolt");

				if (IsMSC)
				{
					_curBoltSizeMSC = wrenchRaycast.GetPlayMaker("Check").FsmVariables.GetFsmFloat("BoltSize");
					PrintToConsole("Fetching Satsuma part data…", ConsoleMessageScope.Core);
					_mscSatsumaVars = PlayMakerExtensions.GetPlayMaker(GameObject.
						Find("SATSUMA(557kg, 248)").transform.
						Find("CarSimulation/MechanicalWear").gameObject, "Data").FsmVariables;
					_mscMotorDb = new List<PlayMakerFSM>();
					foreach (PlayMakerFSM fsm in GameObject.Find("Database/DatabaseMotor").GetComponentsInChildren<PlayMakerFSM>())
					{
						PrintToConsole($"-> Adding fsm to database: {fsm.gameObject.name}", ConsoleMessageScope.Core);
						_mscMotorDb.Add(fsm);
					}
				}

				PrintToConsole("Loading settings…", ConsoleMessageScope.Core);
				_boltSizeMode = SettingBoltSizePrecision.GetSelectedItemIndex();
				_showBoltSizes = SettingShowBoltSizes.GetValue();
				_showValveClearance = SettingShowValveClearance.GetValue();
				_showSuspensionTuning = SettingShowSuspensionTuning.GetValue();
				_logNewTrackers = SettingLogNewTrackers.GetValue();
				_logVerification = SettingLogVerification.GetValue();
				_logBoltSize = SettingLogBoltSize.GetValue();

				PrintToConsole("Setting up display UI…", ConsoleMessageScope.Core);
				RefreshDisplayGUI();
				PrintToConsole("Finalizing setup…", ConsoleMessageScope.Core);
				_allTrackers = new Dictionary<GameObject, BaseTracker>();
				_blacklistedObjects = new HashSet<GameObject>();
				RebuildDisplays();

				stopwatch.Stop();
				PrintToConsole($"{Name} initialized after {stopwatch.Elapsed.Milliseconds} ms!", ConsoleMessageScope.Core);
				PrintToConsole($"Enabled logging levels: {_logNewTrackers}, {_logVerification}, {_logBoltSize}", ConsoleMessageScope.Core);
			}
			catch (Exception e)
			{
				ModConsole.Error($"{Name} version {Version} failed to initialize!!! Error: {e.StackTrace}");
			}
		}

		private void Mod_OnUpdate()
		{
			UpdateDisplays();
			UpdateInspection();
			UpdateBolts();
		}

		/// <summary>
		/// Updates the display text of all existing trackers every <see cref="_timeBetweenUpdates"/> seconds.<br/>
		/// Also removes entries in <see cref="_allTrackers"/> that have deleted game objects.
		/// </summary>
		private void UpdateDisplays()
		{
			_timeSinceLastUpdate += Time.deltaTime;
			if (_timeSinceLastUpdate >= _timeBetweenUpdates)
			{
				_timeSinceLastUpdate = 0f;
				List<GameObject> toRemove = new List<GameObject>();
				foreach (KeyValuePair<GameObject, BaseTracker> kvp in _allTrackers)
				{
					// Ensure that destroyed objects have their trackers disposed properly from the master list
					// We do this after iteration to avoid runtimes
					if (kvp.Key == null)
					{
						PrintToConsole($"Found a tracker of type {kvp.Value.GetType()} with a null object. Adding to removal queue.", ConsoleMessageScope.NewTrackers);
						toRemove.Add(kvp.Key);
						continue;
					}
					kvp.Value.BuildDisplayText();
				}
				foreach (var obj in toRemove)
				{
					PrintToConsole("Removing null tracker…", ConsoleMessageScope.NewTrackers);
					_allTrackers.Remove(obj);
				}
			}
		}

		/// <summary>
		/// Reads whatever the player is currently looking at, adding or referencing trackers as needed.
		/// This entire function is where the sauce happens, so to speak.<br/><br/>
		/// <b>Always skipped if tool mode is TRUE.</b>
		/// </summary>
		private void UpdateInspection()
		{
			// entirely skip inspection if tool mode is active
			// otherwise, if the player is looking at something when switching modes, its name will get stuck on the screen
			if (_toolMode.Value)
				return;
			GameObject lookedObj = _plyCamObject.Value;
			if (lookedObj != null)
			{
				PrintToConsole($"Checking if valid object: {lookedObj.name}", ConsoleMessageScope.Verification);
				if (_blacklistedObjects.Contains(lookedObj)) 
				{
					PrintToConsole("-> Object is blacklisted. Returning.", ConsoleMessageScope.Verification);
					return;
				}
				if (_allTrackers.Keys.Contains(lookedObj))
				{
					PrintToConsole("-> Object already has a tracker. Returning.", ConsoleMessageScope.Verification);
					BaseTracker wt = _allTrackers[lookedObj];
					if (wt.DisplayText != string.Empty)
						_displayGui.Value = wt.DisplayText;
					return;
				}

				// first: set whether or not we will need to view the part name list
				// MWC conveniently generalizes this across all of its parts; MSC, however, does not
				// as a result, for MSC we *always* check part name, but for MWC we skip it if we can find the relevant data
				bool tryPartLookup = true;
				//TrackerType? trackerTypeOverride = null;

#warning TODO: generalize this as a heuristic
				if (!IsMSC)
				{
					PlayMakerFSM dataFsm = PlayMakerExtensions.GetPlayMaker(lookedObj, "Data");
					if (dataFsm == null)
					{
						PrintToConsole("-> DOES NOT have Data fsm. Checking name.", ConsoleMessageScope.Verification);
						tryPartLookup = true;
					}
					else
					{
						PrintToConsole("-> DOES have Data fsm. Verifying if Wear is present…", ConsoleMessageScope.Verification);
						FsmFloat wearVal = PlayMakerExtensions.GetVariable<FsmFloat>(dataFsm, "Wear");
						FsmFloat minWearVal = PlayMakerExtensions.GetVariable<FsmFloat>(dataFsm, "WearMin");
						if (wearVal == null || (wearVal.Value == 99 && minWearVal == null))
						{
							PrintToConsole("--> Wear variable is not present or is 99 exactly. Checking name instead.", ConsoleMessageScope.Verification);
							tryPartLookup = true;
						}
						else
						{
							PrintToConsole($"--> Wear is present! Value: {wearVal.Value}", ConsoleMessageScope.Verification);
							tryPartLookup = false; // heuristic matched -- don't bother with a part lookup, just use a standard tracker instead
						}
					}
				}

				/*if (IsModLoaded_SpareParts && lookedObj.GetComponent<SparePart>() != null)
				{
					PrintToConsole("-> Part is a spare part from Spare Parts. Continuing.", ConsoleMessageScope.Verification);
					tryPartLookup = false;
					trackerTypeOverride = TrackerType.MOD_SparePart;
				}*/

				// second: look up the object's name in the part name list
				// if it's not, this isn't something with a tracker -- back out
				if (tryPartLookup)
				{
					PrintToConsole("-> Now checking for name in _partNames.", ConsoleMessageScope.Verification);
					if (!_partNames.Keys.Contains(lookedObj.name))
					{
						PrintToConsole("--> Part name is not present. Doing a final check on the parent object…", ConsoleMessageScope.Verification);
						if (!lookedObj.transform.parent?.gameObject || !_partNames.Keys.Contains(lookedObj.transform.parent.gameObject.name))
						{
							PrintToConsole("--> No trackable parent object. Returning.", ConsoleMessageScope.Verification);
							return;
						}
						else
						{
							if (_allTrackers.Keys.Contains(lookedObj.transform.parent.gameObject))
							{
								PrintToConsole("--> Parent object was found but already tracked. Returning.", ConsoleMessageScope.Verification);
								return;
							}
							PrintToConsole($"--> Parent object {lookedObj.transform.parent.gameObject.name} is trackable! We are valid after all. Using that one.", ConsoleMessageScope.Verification);
							lookedObj = lookedObj.transform.parent.gameObject;
						}
					}
				}

				PrintToConsole($"Detected a valid object named \"{lookedObj.name}\". Adding tracker.", ConsoleMessageScope.NewTrackers);
				CreateTrackerForPart(lookedObj, _partNames.ContainsKey(lookedObj.name) ? _partNames[lookedObj.name] : null);
				//CreateTrackerForPart(lookedObj, trackerTypeOverride ?? (_partNames.ContainsKey(lookedObj.name) ? _partNames[lookedObj.name] : null));
			}
		}

		/// <summary>
		/// Handles bolt size display: detects bolts, calculates size, and displays readable text.<br/><br/>
		/// <b>Always skipped if tool mode is FALSE.</b>
		/// </summary>
		private void UpdateBolts()
		{
			if (!_showBoltSizes && !_showValveClearance && !_showSuspensionTuning)
				return;
			if (!_toolMode.Value)
				return;
			PrintToConsole("Inspecting bolts…", ConsoleMessageScope.BoltInspection);
			if (!_curBolt.Value)
			{
				PrintToConsole("…Returning because there's no bolt.", ConsoleMessageScope.BoltInspection);
				_lastBoltInspected = null;
				if (_boltSizeText != string.Empty)
					_boltSizeText = string.Empty;
				return;
			}
			if (_curBolt.Value == _lastBoltInspected) // we're lookin at it -- show the text
			{
				PrintToConsole("…We're looking at the cached bolt. Going with that.", ConsoleMessageScope.BoltInspection);
				if (_boltSizeText != string.Empty)
					_displayGui.Value = _boltSizeText;
				return;
			}
			_boltSizeText = string.Empty;
			// checking the object name is how the game differentiates between bolt size and like. wrench size
			// so we get to do that too. god have mercy on my soul
			if (IsMSC && _curBolt.Value.name != "BoltPM")
			{
				PrintToConsole("…We're looking at something that isn't a bolt. Exiting.", ConsoleMessageScope.BoltInspection);
				return;
			}
			PrintToConsole("-> Viewing data…", ConsoleMessageScope.BoltInspection);
			var boltVals = _curBolt.Value.GetPlayMaker("Screw")?.FsmVariables;
			float? boltSize = IsMSC ? _curBoltSizeMSC.Value : boltVals?.GetFsmFloat("Boltsize").Value;
			if (boltSize == null)
			{
				PrintToConsole("…Bolt size is null. Returning.", ConsoleMessageScope.BoltInspection);
				return;
			}
			PrintToConsole($"-> Caching new bolt. Size: {boltSize}", ConsoleMessageScope.BoltInspection);
			_lastBoltInspected = _curBolt.Value;
			string toDisplay = string.Empty;
			float toolSize = _curWrenchSize.Value;
			if (toolSize == boltSize)
			{
				// satsuma's valves tune with screws; rivett's tune with a size 12 nut
				if (_showValveClearance &&
					(IsMSC && boltSize == 0.65f) ||
					!IsMSC && boltSize == 1.2f)
				{
					var valveDraft = boltVals.FindFsmFloat(IsMSC ? "Alignment" : "AdjustmentF");
					var valveName = boltVals.FindFsmString("Valve");
					if (valveDraft != null && valveName != null)
					{
						bool isExhaust = valveName.Value.Contains("exhaust");
						toDisplay = $"{(isExhaust ? "Exhaust" : "Intake")} valve lash - {Math.Round(valveDraft.Value / 100, 4)} mm";
						PrintToConsole("…We're looking at a valve. Displaying lash.", ConsoleMessageScope.BoltInspection);
						_lastBoltInspected = null; // since we're tuning in real-time, we need to constantly update this
						goto calculateText;
					}
				}
				if (_showSuspensionTuning &&
					(IsMSC && boltSize == 0.65f && boltVals.FindFsmFloat("AdjustmentStep")?.Value == 100))
				{
					var alignment = boltVals.FindFsmFloat("Alignment").Value;
					var max = boltVals.FindFsmFloat("Max").Value;
					var min = boltVals.FindFsmFloat("Min").Value;
					alignment -= min;
					max -= min;
					alignment = (float)Math.Round((alignment / max) * 100);
					bool isBump = boltVals.FindFsmGameObject("ThisBolt").Value.name.Contains("bump"); // i hate this too in fact
					toDisplay = $"Suspension {(isBump ? "bump" : "rebound")} - {alignment}%";
					PrintToConsole($"…We're looking at rally suspension. Displaying {(isBump ? "bump" : "rebound")}.", ConsoleMessageScope.BoltInspection);
					_lastBoltInspected = null;
					goto calculateText;
				}
				PrintToConsole("…Tool size is correct. Returning", ConsoleMessageScope.BoltInspection);
				return;
			}
			else if (_showBoltSizes)
			{
				if (boltSize == 0.65f)
				{
					toDisplay = "Need screwdriver";
					PrintToConsole("…This is a screw, not a bolt.", ConsoleMessageScope.BoltInspection);
				}
				else if (toolSize == 0.65f)
				{
					toDisplay = "Need wrench";
					PrintToConsole("…This is a bolt/nut, not a screw.", ConsoleMessageScope.BoltInspection);
				}
			}
		calculateText:
			PrintToConsole("-> Calculating display text…", ConsoleMessageScope.BoltInspection);
			if (toDisplay == string.Empty && _showBoltSizes)
			{
				switch (_boltSizeMode)
				{
					case 0: // Show exact size
						toDisplay = $"Size {boltSize * 10f}";
						break;
					case 1: // Show relative directionality
						float dist = Math.Abs((float)(toolSize - boltSize));
						string direction = $"{(dist > 0.3f ? "Way" : "Slightly")} Too {(toolSize > boltSize ? "Big" : "Small")}";
						toDisplay = $"Wrench {direction}";
						break;
					case 2: // Show directionality only
						toDisplay = $"Wrench Too {(toolSize > boltSize ? "Big" : "Small")}";
						break;
				}
			}
			PrintToConsole($"-> Text to display: \"{toDisplay}\"", ConsoleMessageScope.BoltInspection);
			_boltSizeText = toDisplay;
			PrintToConsole("Bolt and text now cached and displaying until we look away.", ConsoleMessageScope.BoltInspection);
			_displayGui.Value = _boltSizeText;
		}

		/// <summary>
		/// Updates the value of <see cref="_displayGui"/> based on user settings.
		/// </summary>
		private void RefreshDisplayGUI() => _displayGui = SettingDisplayLocation.GetSelectedItemIndex() == 0 ? _pickedPartGui : _interactionGui;

		/// <summary>
		/// Simple wrapper to adjust relevant values when update frequency settings are changed.
		/// </summary>
		private void RebuildDisplays()
		{
			_timeSinceLastUpdate = 0f;
			_timeBetweenUpdates = SettingTextUpdateFrequency.GetValue();
		}

		/// <summary>
		/// Big monolith of a function that parses information about an object and its tracker type and then creates a new tracker for that object based on the info provided.
		/// Every new tracker type should have handling implemented into this function.
		/// See docs on the <c>trackerInfo</c> param for important info.
		/// </summary>
		/// <param name="gameObj">The <see cref="GameObject"/> that will begin being tracked.</param>
		/// <param name="trackerInfo">Determines which type of tracker will be used.
		/// Accepts <see cref="TrackerType"/>, <see cref="FullnessInfo"/>, <see cref="VariantInfo"/>, <see cref="FreshnessInfo"/>, or null
		/// (which falls back to <see cref="TrackerType.PartCondition"/>).</param>
		private void CreateTrackerForPart(GameObject gameObj, object trackerInfo = null)
		{
			BaseTracker bwt = null;
			Type newTrackerType = null;
			try
			{
				TrackerType tt = TrackerType.PartCondition;
				if (trackerInfo is TrackerType t)
					tt = t;
				else if (trackerInfo is FullnessInfo)
					tt = TrackerType.Fullness;
				else if (trackerInfo is VariantInfo vi)
					tt = !vi.AlsoTracksWear ? TrackerType.MWC_Variant : TrackerType.MWC_VariantAndWear;
				else if (trackerInfo is FreshnessInfo)
					tt = TrackerType.Freshness;
				PrintToConsole($"Creating tracker on game object {gameObj} with type: {tt}", ConsoleMessageScope.NewTrackers);
				switch (tt)
				{
					case TrackerType.PartCondition:
						if (!SettingShowCarPartCondition.GetValue())
							break;
						if (!IsMSC) // MWC follows a standardized format for its parts that wear down -- just entrust the setup to the tracker itsef
							newTrackerType = typeof(PartConditionTracker);
						else // MSC, however, has a whole bunch of finagling that needs to be done, so we do the heavy lifting here
						{
							PartConditionTracker swt = gameObj.AddComponent<PartConditionTracker>();
							FsmVariables dbInfo = null;
							foreach (PlayMakerFSM fsm in _mscMotorDb)
							{
								var vars = fsm.FsmVariables;
								if (vars.GetFsmString("UniqueTag").Value == gameObj.name)
								{
									dbInfo = vars;
									break;
								}
							}
							bwt = swt;
							swt.Initialize(gameObj.name, _mscSatsumaVars, "Wear" + _partNames[gameObj.name], dbInfo);
							break;
						}
						break;

					case TrackerType.IntactOrBroken:
						if (!SettingShowCarPartCondition.GetValue())
							break;
						if (IsMSC)
						{
							IntactOrBrokenTracker ibt = gameObj.AddComponent<IntactOrBrokenTracker>();
							bwt = ibt;
							FsmVariables dbInfo = null;
							foreach (PlayMakerFSM fsm in _mscMotorDb)
							{
								var vars = fsm.FsmVariables;
								if (vars.GetFsmString("UniqueTag").Value == gameObj.name)
								{
									dbInfo = vars;
									break;
								}
							}
							ibt.Initialize(gameObj.name, dbInfo);
						}
						else
							newTrackerType = typeof(IntactOrBrokenTracker);
						break;

					case TrackerType.OilFilter:
						// realistically I can't imagine a case of someone wanting to know car parts but *not* oil filters, so
						if (!SettingShowCarPartCondition.GetValue())
							break;
						OilFilterTracker oft = gameObj.AddComponent<OilFilterTracker>();
						bwt = oft;
						// oil filters have slightly different variable names between games, but are otherwise identical
						oft.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, IsMSC ? "Use" : "Data").FsmVariables);
						break;
					case TrackerType.Fullness:
						if (!SettingShowContainerFullness.GetValue())
							break;
						FullnessInfo fi = (FullnessInfo)trackerInfo;
						PrintToConsole("Creating fullness tracker…", ConsoleMessageScope.NewTrackers);
						GameObject objToRead = gameObj;
						if (fi.ChildObjectName != null)
						{
							PrintToConsole($"-> Finding child object with name: {fi.ChildObjectName}", ConsoleMessageScope.NewTrackers);
							// this is necessary due to the way the game handles the data on its fluid objects;
							// the fullness value on the base object itself is generally not accurate, and instead
							// the trigger object (of which the base object is a parent) always holds the most up-to-date data
							objToRead = objToRead.transform.Find(fi.ChildObjectName)?.gameObject;
							if (objToRead == null)
							{
								PrintToConsole("-> Child object not found. Breaking.", ConsoleMessageScope.NewTrackers);
								break;
							}
							else
								PrintToConsole("-> Child object found. We'll reference its playmaker.", ConsoleMessageScope.NewTrackers);
						}
						PrintToConsole($"-> Final object to read FSM from: {objToRead}.", ConsoleMessageScope.NewTrackers);
						FullnessTracker ft = gameObj.AddComponent<FullnessTracker>();
						bwt = ft;
						ft.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(objToRead, fi.FsmName).FsmVariables, fi);
						PrintToConsole($"Fullness tracker initialized.", ConsoleMessageScope.NewTrackers);
						break;
					case TrackerType.Quantity:
						if (!SettingShowPackageQuantity.GetValue())
							break;
						QuantityTracker qt = gameObj.AddComponent<QuantityTracker>(); // qt uwu  // 2026 update: forgot I wrote this. fantastic bit, past me
						bwt = qt;
						qt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Use").FsmVariables);
						break;
					case TrackerType.Freshness:
						if (!SettingShowFoodFreshness.GetValue())
							break;
						FreshnessInfo fri = (FreshnessInfo)trackerInfo;
						FreshnessTracker frt = gameObj.AddComponent<FreshnessTracker>();
						bwt = frt;
						frt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Use").FsmVariables, fri);
						break;

					#region MSC-exclusive trackers
					case TrackerType.MSC_SparkPlug:
						if (!SettingShowCarPartCondition.GetValue())
							break;
						MSC_SparkPlugTracker spt = gameObj.AddComponent<MSC_SparkPlugTracker>();
						spt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Use").FsmVariables);
						bwt = spt;
						break;
					#endregion

					#region MWC-exclusive trackers
					case TrackerType.MWC_Variant:
						if (!SettingShowObjectVariants.GetValue())
							break;
						VariantTracker vt = gameObj.AddComponent<VariantTracker>();
						vt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Data").FsmVariables, trackerInfo);
						bwt = vt;
						break;
					case TrackerType.MWC_VariantAndWear:
						if (!SettingShowObjectVariants.GetValue() || !SettingShowCarPartCondition.GetValue())
							break;
						VariantAndWearTracker vwt = gameObj.AddComponent<VariantAndWearTracker>();
						vwt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Data").FsmVariables, trackerInfo);
						bwt = vwt;
						break;
					#endregion


					/*#region Mod integration
					case TrackerType.MOD_SparePart:
						if (!IsMSC || !SettingShowCarPartCondition.GetValue())
							return;
						MOD_SparePartTracker spapt = gameObj.AddComponent<MOD_SparePartTracker>();
						bwt = spapt;
						spapt.Initialize(gameObj.name, null);
						break;
					#endregion*/

					default:
						ModConsole.Error($"Part Inspector attempted to initialize with an invalid tracker type: {tt}");
						return;
				}
				// if we're left with a type and haven't yet initialized, do so now following a standard pattern
				if (newTrackerType != null && typeof(BaseTracker).IsAssignableFrom(newTrackerType))
				{
					PrintToConsole($"Initializing new tracker  (type: {newTrackerType})", ConsoleMessageScope.NewTrackers);
					BaseTracker bt = (BaseTracker)gameObj.AddComponent(newTrackerType);
					bt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Data").FsmVariables);
					bwt = bt;
				}
				if (bwt != null)
				{
					bwt.BuildDisplayText();
					_allTrackers.Add(gameObj, bwt);
					PrintToConsole($"A tracker component of type {bwt.GetType()} was added to an object named \"{gameObj.name}\".", ConsoleMessageScope.NewTrackers);
				}
			}
			catch (Exception e)
			{
				ModConsole.Warning($"Failed to add a tracker to object \"{gameObj.name}\"!\n" +
					$"Please report this as a bug and include your output_log.txt. You should be able to keep playing, but this item won't be tracked.");
				_blacklistedObjects.Add(gameObj);
				if (bwt != null) // in case the error happened during tracker init -- remove it
				{
					bwt.enabled = false;
					GameObject.Destroy(bwt);
					_allTrackers.Remove(gameObj);
				}
				throw e;
			}
		}
		#endregion
	}
}
