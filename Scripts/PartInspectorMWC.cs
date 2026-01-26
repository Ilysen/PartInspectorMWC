using Ceres.PartInspectorMWC.Trackers;
using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static Ceres.PartInspectorMWC.Trackers.FullnessTracker;

namespace Ceres.PartInspectorMWC
{
	public class PartInspectorScript : Mod
	{
		#region Metadata
		public override string ID => "Ceres_PartInspectorMWC";
		public override string Name => "Part Inspector";
		public override string Author => "Ceres et al.";
		public override string Version => "0.1.3";
		public override string Description => "Inspect your stuff for integrity, condition, and dirtiness.";
		public override Game SupportedGames => Game.MyWinterCar;
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
		internal static SettingsCheckBox SettingShowObjectVariants;
		internal static SettingsCheckBox SettingShowBoltSizes;

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
			SettingShowObjectVariants = Settings.AddCheckBox(nameof(SettingShowObjectVariants), "Show object variants", true);
			Settings.AddText("A part's variant will be shown in its display name. For things like instrument panels, grilles, and brake lines.");
			SettingShowBoltSizes = Settings.AddCheckBox(nameof(SettingShowBoltSizes), "Show bolt sizes", false,
				() => _showBoltSizes = SettingShowBoltSizes.GetValue());
			Settings.AddText("When in tool mode, shows the size of whatever bolt you're looking at.");

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
		}
		#endregion

		#region Cached vars
		/// <summary>
		/// Cached reference to the interaction GUI global.
		/// </summary>
		private FsmString _interactionGui;
		/// <summary>
		/// Cached reference to the item name display global.
		/// </summary>
		private FsmString _pickedPartGui;

		/// <summary>
		/// Cached references to the value of <see cref="SettingShowBoltSizes"/>.
		/// </summary>
		private bool _showBoltSizes;

		/// <summary>
		/// Cached reference to the value of <see cref="SettingBoltSizePrecision"/>.
		/// </summary>
		private int _boltSizeMode = 3;

		/// <summary>
		/// Cached reference to the bolt we're showing the size of.
		/// </summary>
		private GameObject _lastBoltInspected;

		/// <summary>
		/// Calculated display text for the bolt we're looking at.
		/// Caching this means we don't have to do a bunch of math every frame.
		/// </summary>
		private string _boltSizeText;

		/// <summary>
		/// Cached reference to the FSM used to track the object the player is currently looking at.
		/// We use this instead of <see cref="UnifiedRaycast"/> because it lets us benefit from the game's own logic
		/// on determining what object's name should be displaying, which the unified raycast does not.
		/// </summary>
		private FsmGameObject _plyCamObject;

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
		#endregion

		#region Internal vars
		/// <summary>
		/// Used to designate the type of wear tracker a given part should receive, mostly through assignment in <see cref="_partNames"/>.
		/// </summary>
		private enum TrackerType
		{
			Standard = 1,
			Simple = 2,
			OilFilter = 3,
			Fullness = 4,
			Quantity = 5,
			Variant = 6
		}

		/// <summary>
		/// Used to track initialization info for variant trackers.
		/// Variants in MWC don't have a standardized way to distinguish between them;
		/// sometimes they use Type (an int), sometimes they use Model (a string), etc.
		/// This struct allows each given part type to define how its variant is determined,
		/// and the human-readable name associated with each variant type.
		/// </summary>
		internal struct VariantInfo
		{
			internal Dictionary<object, string> Variants;
			internal string VariantKey;
			internal Type VariantKeyType;

			internal VariantInfo(Dictionary<object, string> Variants, Type VariantKeyType, string VariantKey = "Type")
			{
				this.Variants = Variants;
				this.VariantKeyType = VariantKeyType;
				this.VariantKey = VariantKey;
			}
		}

		/// <summary>
		/// Game objects with names in the keys of this dict will gain a wear tracker component when inspected, if they don't have one already, with some of that component's info being taken from the associated value of that key.
		/// <br/><br/>
		/// Names with an associated string will be given a <see cref="StandardWearTracker"/> using that string as the wear key; names with an associated <see cref="TrackerType"/> will instead use that type when creating the tracker.
		/// </summary>
		// mmmmm yummy pasta
		private readonly Dictionary<string, object> _partNames = new Dictionary<string, object>
		{
			{ "Engine Block(VINX0)", TrackerType.Simple },
			{ "Oilpan(VINXX)", TrackerType.Simple },

			{ "automatic transmission fluid(itemx)", new FullnessInfo("Data", MaxValue: 1f, DisplayAsFluid: true, ChildObjectName: "ATFOilTrigger" ) },
			{ "brake fluid(itemx)", new FullnessInfo("Data", MaxValue: 1f, DisplayAsFluid: true, ChildObjectName: "BrakeFluidTrigger" ) },
			{ "two stroke fuel(itemx)", new FullnessInfo("Data", MaxValue: 5f, DisplayAsFluid: true, ChildObjectName: "MotorOilTrigger" ) },
			{ "motor oil(itemx)", new FullnessInfo("Data", MaxValue: 4f, DisplayAsFluid: true, ChildObjectName: "MotorOilTrigger" ) },
			{ "coolant(itemx)", new FullnessInfo("Data", MaxValue: 10f, DisplayAsFluid: true, ChildObjectName: "CoolantTrigger" ) },

			{ "Oil filter(VINXX)", TrackerType.OilFilter },
			{ "spray can(itemx)", new FullnessInfo(MaxValue: 100f ) },
			{ "mosquito spray(itemx)", new FullnessInfo(MaxValue: 100f ) },
			{ "Fire Extinguisher(VINXX)", new FullnessInfo("Data", MaxValue: 100f ) },
			{ "ground coffee(itemx)", new FullnessInfo(ValueKey: "Ground", MaxValue: 100f, MinValue: 1 ) },
			{ "grill charcoal(itemx)", new FullnessInfo(ValueKey: "Contents", MaxValue: 140f, MinValue: 1 ) },

			{ "spark plug box(Clone)", TrackerType.Quantity },
			{ "r20 battery box(Clone)", TrackerType.Quantity },
			{ "fuse package(Clone)", TrackerType.Quantity },
			{ "package(Clone)", TrackerType.Quantity }, // for parts purchased from fleetari. not the most descriptive name in the world, huh?

			{ "Brake Lines(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 1, "Standard Brakes" }, { 2, "Power Brakes" }
			}, typeof(int) ) },

			{ "Brake Master Cylinder(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 1, "Standard Brakes" }, { 2, "Power Brakes" }
			}, typeof(int) ) },

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
			}, typeof(string), "Code" ) }
		};

		/// <summary>
		/// Every wear tracker in the game world, associated to its game object.
		/// </summary>
		private Dictionary<GameObject, BaseTracker> _allTrackers;

		/// <summary>
		/// The text GUI used to display the part's condition. This will always point to either <see cref="_pickedPartGui"/> or <see cref="_interactionGui"/>,
		/// depending on the value of <see cref="SettingDisplayLocation"/>.
		/// </summary>
		private FsmString _displayGui;

		/// <summary>
		/// To save performance, and because parts are unlikely to rapidly change condition in a given time period, display names only update every few seconds. This value tracks how often parts update, in seconds.
		/// </summary>
		private float _timeBetweenUpdates = 10f;

		/// <summary>
		/// How many seconds have elapsed since we last updated displays. See <see cref="_timeBetweenUpdates"/> for more info.
		/// </summary>
		private float _updateTimer = 0f;
		#endregion

		#region Debug
		internal enum ConsoleMessageScope
		{
			Core, // Core logic that we always log
			NewTrackers, // Whenever a new tracker is created
			Verification, // Detailed steps for detecting if a given object is a valid part
			BoltInspection // Step-by-step process for viewing bolts
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
				PrintToConsole($"{Name} version {Version} is attempting to initialize", ConsoleMessageScope.Core);

				PrintToConsole("Caching objects and variables...", ConsoleMessageScope.Core);
				FsmVariables plyCam = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/Hand").GetPlayMaker("PickUp").FsmVariables;
				_plyCamObject = plyCam.GetFsmGameObject("RaycastHitObject");

				_toolMode = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerHandRight");
				_interactionGui = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
				_pickedPartGui = PlayMakerGlobals.Instance.Variables.FindFsmString("PickedPart");
				_boltSizeMode = SettingBoltSizePrecision.GetSelectedItemIndex();
				GameObject wrenchRaycast = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/2Spanner/Raycast");
				_curWrenchSize = PlayMakerGlobals.Instance.Variables.FindFsmFloat("ToolWrenchSize");
				_curBolt = wrenchRaycast.GetPlayMaker("Raycast").FsmVariables.GetFsmGameObject("Bolt");
				_showBoltSizes = SettingShowBoltSizes.GetValue();
				_logNewTrackers = SettingLogNewTrackers.GetValue();
				_logVerification = SettingLogVerification.GetValue();
				_logBoltSize = SettingLogBoltSize.GetValue();

				PrintToConsole("Setting up display UI...", ConsoleMessageScope.Core);
				RefreshDisplayGUI();
				PrintToConsole("Finalizing setup...", ConsoleMessageScope.Core);
				_allTrackers = new Dictionary<GameObject, BaseTracker>();
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
			_updateTimer += Time.deltaTime;
			if (_updateTimer >= _timeBetweenUpdates)
			{
				_updateTimer = 0f;
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
					PrintToConsole("Removing null tracker...", ConsoleMessageScope.NewTrackers);
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
				if (_allTrackers.Keys.Contains(lookedObj))
				{
					PrintToConsole("-> Object already has a tracker. Returning.", ConsoleMessageScope.Verification);
					BaseTracker wt = _allTrackers[lookedObj];
					if (wt.DisplayText != string.Empty)
						_displayGui.Value = wt.DisplayText;
					return;
				}

				// first, check for an FSM named Data, and then check for a field named Wear
				PlayMakerFSM dataFsm = PlayMakerExtensions.GetPlayMaker(lookedObj, "Data");
				bool checkForName = false;
				if (dataFsm == null)
				{
					PrintToConsole("-> DOES NOT have Data fsm. Checking name.", ConsoleMessageScope.Verification);
					checkForName = true;
				}
				else
				{
					PrintToConsole("-> DOES have Data fsm. Verifying if Wear is present...", ConsoleMessageScope.Verification);
					FsmFloat wearVal = PlayMakerExtensions.GetVariable<FsmFloat>(dataFsm, "Wear");
					FsmFloat minWearVal = PlayMakerExtensions.GetVariable<FsmFloat>(dataFsm, "WearMin");
					if (wearVal == null || (wearVal.Value == 99 && minWearVal == null))
					{
						PrintToConsole("--> Wear variable is not present or is 99 exactly. Checking name instead.", ConsoleMessageScope.Verification);
						checkForName = true;
					}
					else
					{
						PrintToConsole($"--> Wear is present! Value: {wearVal.Value}", ConsoleMessageScope.Verification);
					}
				}

				// if neither of those are present, then check to see if the object's name is in the list
				// if it's not, this isn't something with a tracker -- back out
				if (checkForName)
				{
					PrintToConsole("-> Now checking for name in _partNames.", ConsoleMessageScope.Verification);
					if (!_partNames.Keys.Contains(lookedObj.name))
					{
						PrintToConsole("--> Part name is not present. Doing a final check on the parent object...", ConsoleMessageScope.Verification);
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
							PrintToConsole("--> Parent object is trackable! We are valid!", ConsoleMessageScope.Verification);
						}
					}
				}

				PrintToConsole($"Detected a valid object named \"{lookedObj.name}\". Adding tracker.", ConsoleMessageScope.NewTrackers);
				CreateTrackerForPart(lookedObj, _partNames.ContainsKey(lookedObj.name) ? _partNames[lookedObj.name] : null);
			}
		}

		/// <summary>
		/// Handles bolt size display: detects bolts, calculates size, and displays readable text.<br/><br/>
		/// <b>Always skipped if tool mode is FALSE.</b>
		/// </summary>
		private void UpdateBolts()
		{
			if (!_showBoltSizes)
				return;
			if (!_toolMode.Value)
				return;
			PrintToConsole("Inspecting bolts...", ConsoleMessageScope.BoltInspection);
			if (_curBolt.Value == _lastBoltInspected) // we're lookin at it -- show the text
			{
				PrintToConsole("...We're looking at the cached bolt. Going with that.", ConsoleMessageScope.BoltInspection);
				if (_boltSizeText != string.Empty)
					_displayGui.Value = _boltSizeText;
				return;
			}
			_boltSizeText = string.Empty;
			float toolSize = _curWrenchSize.Value;
			if (toolSize == 0.65f)
			{
				PrintToConsole("...Returning because we're holding a screwdriver.", ConsoleMessageScope.BoltInspection);
				return;
			}
			if (!_curBolt.Value)
			{
				PrintToConsole("...Returning because there's no bolt.", ConsoleMessageScope.BoltInspection);
				_lastBoltInspected = null;
				return;
			}
			PrintToConsole("-> Viewing data...", ConsoleMessageScope.BoltInspection);
			float? boltSize = _curBolt.Value.GetPlayMaker("Screw")?.FsmVariables.GetFsmFloat("Boltsize").Value;
			if (boltSize == null)
			{
				PrintToConsole("...Bolt size is null. Returning.", ConsoleMessageScope.BoltInspection);
				return;
			}
			PrintToConsole($"-> Caching new bolt. Size: {boltSize}", ConsoleMessageScope.BoltInspection);
			_lastBoltInspected = _curBolt.Value;
			if (boltSize == 0.65f) // this is a screw, not a bolt!
			{
				PrintToConsole("...This is a screw, not a bolt. Returning.", ConsoleMessageScope.BoltInspection);
				return;
			}
			if (toolSize == boltSize)
			{
				PrintToConsole("...Tool size is correct. Returning", ConsoleMessageScope.BoltInspection);
				return;
			}
			string toDisplay = null;
			PrintToConsole("-> Calculating display text...", ConsoleMessageScope.BoltInspection);
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
			_updateTimer = 0f;
			_timeBetweenUpdates = SettingTextUpdateFrequency.GetValue();
		}

		/// <summary>
		/// Creates a tracker component for the provided <see cref="GameObject"/>.
		/// See arguments for info on how tracker type is determined.
		/// </summary>
		/// <param name="gameObj">The <see cref="GameObject"/> that will begin being tracked.</param>
		/// <param name="trackerInfo">Determines which type of tracker will be used.
		/// Accepts <see cref="TrackerType"/>, <see cref="FullnessInfo"/>, <see cref="VariantInfo"/>, or null
		/// (which falls back to <see cref="TrackerType.Standard"/>).</param>
		private void CreateTrackerForPart(GameObject gameObj, object trackerInfo = null)
		{
			TrackerType tt = TrackerType.Standard;
			if (trackerInfo is TrackerType t)
				tt = t;
			else if (trackerInfo is FullnessInfo)
				tt = TrackerType.Fullness;
			else if (trackerInfo is VariantInfo)
				tt = TrackerType.Variant;
			BaseTracker bwt = null;
			Type newTrackerType = null;
			PrintToConsole($"Creating tracker on game object {gameObj} with type: {tt}", ConsoleMessageScope.NewTrackers);
			switch (tt)
			{
				case TrackerType.Standard:
					if (!SettingShowCarPartCondition.GetValue())
						break;
					newTrackerType = typeof(StandardWearTracker);
					break;
				case TrackerType.Simple:
					if (!SettingShowCarPartCondition.GetValue())
						break;
					newTrackerType = typeof(SimpleWearTracker);
					break;
				case TrackerType.OilFilter:
					// realistically I can't imagine a case of someone wanting to know car parts but *not* oil filters, so
					if (!SettingShowCarPartCondition.GetValue())
						break;
					newTrackerType = typeof(OilFilterTracker);
					break;
				case TrackerType.Fullness:
					FullnessInfo fi = (FullnessInfo)trackerInfo;
					if (!SettingShowContainerFullness.GetValue())
						break;
					PrintToConsole("Creating fullness tracker...", ConsoleMessageScope.NewTrackers);
					GameObject objToRead = gameObj;
					if (fi.ChildObjectName != null)
					{
						PrintToConsole($"-> Finding child object with name: {fi.ChildObjectName}", ConsoleMessageScope.NewTrackers);
						// this is necessary due to the way MWC handles the data on its fluid objects;
						// the fullness value on the base object itself is generally not accurate, and instead
						// the trigger object (of which the base object is a parent) holds the most up-to-date data
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
					ft.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(objToRead, fi.FsmName).FsmVariables, fi);
					bwt = ft;
					PrintToConsole($"Fullness tracker initialized.", ConsoleMessageScope.NewTrackers);
					break;
				case TrackerType.Quantity:
					if (!SettingShowPackageQuantity.GetValue())
						break;
					QuantityTracker qt = gameObj.AddComponent<QuantityTracker>(); // qt uwu
					qt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Use").FsmVariables);
					bwt = qt;
					break;
				case TrackerType.Variant:
					if (!SettingShowObjectVariants.GetValue())
						break;
					VariantTracker vt = gameObj.AddComponent<VariantTracker>();
					vt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Data").FsmVariables, trackerInfo);
					bwt = vt;
					break;
				default:
					ModConsole.Error($"Part Inspector attempted to initialize with an invalid tracker type: {tt}");
					return;
			}
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
		#endregion
	}
}
