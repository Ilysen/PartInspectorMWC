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
		public override string Version => "0.1";
		public override string Description => "Inspect your stuff for integrity, condition, and dirtiness.";
		public override Game SupportedGames => Game.MyWinterCar;
		#endregion

		#region Mod setup and settings
		internal static SettingsDropDownList SettingDisplayLocation;
		internal static SettingsDropDownList SettingDisplayPrecision;
		internal static SettingsDropDownList SettingItemDisplayPrecision;
		internal static SettingsSliderInt SettingTextUpdateFrequency;

		internal static SettingsCheckBox SettingShowCarPartCondition;
		internal static SettingsCheckBox SettingShowContainerFullness;
		internal static SettingsCheckBox SettingShowPackageQuantity;
		internal static SettingsCheckBox SettingShowObjectVariants;

		internal static SettingsCheckBox SettingLogVerification;
		internal static SettingsCheckBox SettingLogNewTrackers;

		public override void ModSetup()
		{
			SetupFunction(Setup.OnLoad, Mod_OnLoad);
			SetupFunction(Setup.Update, Mod_OnUpdate);
			SetupFunction(Setup.ModSettings, Mod_Settings);
		}

		private void Mod_Settings()
		{
			Color headingColor = new Color(0.1f, 0.1f, 0.1f);

			Settings.AddHeader("Interface", headingColor, Color.white);
			SettingDisplayLocation = Settings.AddDropDownList("displayLocation", "Where to display information",
				new string[] { "In the item's name (recommended)", "In interaction text" }, 0, RefreshDisplayGUI);
			SettingDisplayPrecision = Settings.AddDropDownList("displayPrecision", "Part inspection precision",
				new string[] { "Show exact information", "Show general description", "Show broken/not broken" }, 1);
			SettingItemDisplayPrecision = Settings.AddDropDownList("itemDisplayPrecision", "Item inspection precision",
				new string[] { "Show exact information", "Show general description" }, 1);
			SettingTextUpdateFrequency = Settings.AddSlider("updateFrequency", "Text update frequency<color=yellow>*</color>",
				1, 10, 10, RebuildDisplays);
			Settings.AddText("<color=yellow>* Lowering this might have an impact on performance. Only use it if you find the default rate to be too sluggish.</color>");

			Settings.AddHeader("Trackers", headingColor, Color.white);
			SettingShowCarPartCondition = Settings.AddCheckBox("showCarPartCondition", "Show car part condition", true);
			Settings.AddText("Includes every car part that can wear down, get dirty, or be broken.");
			SettingShowContainerFullness = Settings.AddCheckBox("showContainerFullness", "Show container fullness", true);
			Settings.AddText("Includes fluids (motor oil, coolant, etc.) as well as solids (ground coffee and grill charcoal).");
			SettingShowPackageQuantity = Settings.AddCheckBox("showPackageQuantity", "Show package quantity", true);
			Settings.AddText("For R20 batteries and fuse boxes: Displays the amount left in the package.");
			SettingShowObjectVariants = Settings.AddCheckBox("showObjectVariants", "Show object variants", true);
			Settings.AddText("A part's variant will be shown in its display name. For things like instrument panels, grilles, and brake lines.");

			Settings.AddHeader("Logging", headingColor, Color.white);
			Settings.AddText("If you're running into bugs, these settings will put extra info into your log that'll help the author diagnose the issues. Keep them all off for regular play, but please turn them on when submitting a bug report!");
			SettingLogNewTrackers = Settings.AddCheckBox("logNewTrackers", "Log new trackers", false);
			SettingLogVerification = Settings.AddCheckBox("logVerification", "Log object verification <color=yellow>(warning: laggy)</color>", false);
		}
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
		// msc is so spaghetti. modding is a pathway to abilities some consider to be unnatural
		private readonly Dictionary<string, object> _partNames = new Dictionary<string, object>
		{
			{ "Engine Block(VINX0)", TrackerType.Simple },
			{ "Oilpan(VINXX)", TrackerType.Simple },

			{ "automatic transmission fluid(itemx)", new FullnessInfo(MaxValue: 1f, DisplayAsFluid: true ) },
			{ "brake fluid(itemx)", new FullnessInfo(MaxValue: 1f, DisplayAsFluid: true ) },
			{ "two stroke fuel(itemx)", new FullnessInfo(MaxValue: 5f, DisplayAsFluid: true ) },
			{ "motor oil(itemx)", new FullnessInfo(MaxValue: 4f, DisplayAsFluid: true ) },
			{ "coolant(itemx)", new FullnessInfo(MaxValue: 10f, DisplayAsFluid: true ) },

			{ "Oil filter(VINXX)", TrackerType.OilFilter },
			{ "spray can(itemx)", new FullnessInfo(MaxValue: 100f ) },
			{ "mosquito spray(itemx)", new FullnessInfo(MaxValue: 100f ) },
			{ "Fire Extinguisher(VINXX)", new FullnessInfo(MaxValue: 100f, FsmName: "Data" ) },
			{ "ground coffee(itemx)", new FullnessInfo(ValueKey: "Ground", MaxValue: 100f ) },
			{ "grill charcoal(itemx)", new FullnessInfo(ValueKey: "Contents", MaxValue: 140f ) },

			{ "spark plug box(Clone)", TrackerType.Quantity },
			{ "r20 battery box(Clone)", TrackerType.Quantity },
			{ "fuse package(Clone)", TrackerType.Quantity },

			{ "Brake Lines(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 1, "Standard Brakes" }, { 2, "Power Brakes" }
			}, typeof(int) ) },

			{ "Brake Master Cylinder(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 1, "Standard Brakes" }, { 2, "Power Brakes" }
			}, typeof(int) ) },

			{ "Exhaust Pipe Front(VINXX)", new VariantInfo( new Dictionary<object, string>{
				{ 0, "Standard" }, { 1, "GT" }
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
		private Dictionary<GameObject, BaseWearTracker> _wearTrackers;

		/// <summary>
		/// The text GUI used to display the part's condition. Can either be within the part's name or in a separate area.
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

		/// <summary>
		/// A cached reference to the FSM used to track the object the player is currently looking at.
		/// We use this instead of <see cref="UnifiedRaycast"/> because it lets us benefit from the game's own logic
		/// on determining what object's name should be displaying, which the unified raycast does not.
		/// </summary>
		private FsmVariables _plyCam;

		private FsmBool _toolMode;
		#endregion

		#region Debug
		internal enum ConsoleMessageScope
		{
			Core, // Core logic that we always log
			NewTrackers, // Whenever a new tracker is created
			Verification // Detailed steps for detecting if a given object is a valid part
		}

		internal static void PrintToConsole(object Message, ConsoleMessageScope Context)
		{
			if (Context == ConsoleMessageScope.Verification && !SettingLogVerification.GetValue())
				return;
			if (Context == ConsoleMessageScope.NewTrackers && !SettingLogNewTrackers.GetValue())
				return;
			ModConsole.Print($"[PI] {Message}");
		}
		#endregion

		#region Main functions

		private void Mod_OnLoad()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			PrintToConsole($"{Name} version {Version} is attempting to initialize!", ConsoleMessageScope.Core);
			_wearTrackers = new Dictionary<GameObject, BaseWearTracker>();
			PrintToConsole("Setting stuff up...", ConsoleMessageScope.Core);
			RefreshDisplayGUI();
			RebuildDisplays();
			PrintToConsole("Detecting player hand camera...", ConsoleMessageScope.Core);
			_plyCam = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/Hand").GetPlayMaker("PickUp").FsmVariables;
			_toolMode = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerHandRight");
			stopwatch.Stop();
			PrintToConsole($"{Name} initialized after {stopwatch.Elapsed.Milliseconds} ms!", ConsoleMessageScope.Core);
		}

		private void Mod_OnUpdate()
		{
			UpdateDisplays();
			UpdateInspection();
		}

		/// <summary>
		/// Manipulate the value of <see cref="_updateTimer"/> and update display texts as needed.
		/// </summary>
		private void UpdateDisplays()
		{
			_updateTimer += Time.deltaTime;
			if (_updateTimer >= _timeBetweenUpdates)
			{
				_updateTimer = 0f;
				List<GameObject> toRemove = new List<GameObject>();
				foreach (KeyValuePair<GameObject, BaseWearTracker> kvp in _wearTrackers)
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
					_wearTrackers.Remove(obj);
				}
			}
		}

		/// <summary>
		/// Raycasts to find if the player is looking at a part. If so, displays the text from that part's wear tracker.
		/// </summary>
		private void UpdateInspection()
		{
			// entirely skip inspection if tool mode is active
			// otherwise, if the player is looking at something when switching modes, its name will get stuck on the screen
			if (_toolMode.Value) 
				return;
			GameObject lookedObj = _plyCam.GetFsmGameObject("RaycastHitObject")?.Value;
			if (lookedObj != null)
			{
				PrintToConsole($"Checking if valid object: {lookedObj.name}", ConsoleMessageScope.Verification);
				if (_wearTrackers.Keys.Contains(lookedObj))
				{
					PrintToConsole("-> Object already has a tracker. Returning.", ConsoleMessageScope.Verification);
					BaseWearTracker wt = _wearTrackers[lookedObj];
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
					if (wearVal == null || wearVal.Value == 99)
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
							if (_wearTrackers.Keys.Contains(lookedObj.transform.parent.gameObject))
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
		/// Updates the value of <see cref="_displayGui"/> based on user settings.
		/// </summary>
		private void RefreshDisplayGUI() => _displayGui = PlayMakerGlobals.Instance.Variables.FindFsmString(SettingDisplayLocation.GetSelectedItemIndex() == 0 ? "PickedPart" : "GUIinteraction");

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
			BaseWearTracker bwt = null;
			Type newTrackerType = null;
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
					FullnessTracker ft = gameObj.AddComponent<FullnessTracker>();
					ft.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, fi.FsmName).FsmVariables, fi);
					bwt = ft;
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
			if (newTrackerType != null && typeof(BaseWearTracker).IsAssignableFrom(newTrackerType))
			{
				PrintToConsole($"Initializing new tracker  (type: {newTrackerType})", ConsoleMessageScope.NewTrackers);
				BaseWearTracker bt = (BaseWearTracker)gameObj.AddComponent(newTrackerType);
				bt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Data").FsmVariables);
				bwt = bt;
			}
			if (bwt != null)
			{
				bwt.BuildDisplayText();
				_wearTrackers.Add(gameObj, bwt);
				PrintToConsole($"A tracker component of type {bwt.GetType()} was added to an object named \"{gameObj.name}\".", ConsoleMessageScope.NewTrackers);
			}
		}
		#endregion
	}
}
