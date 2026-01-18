using Ceres.PartInspectorMWC.Trackers;
using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TanjentOGG;
using UnityEngine;

namespace Ceres.PartInspectorMWC
{
	public class PartInspectorScript : Mod
	{
		public override string ID => "Ceres_PartInspectorMWC";
		public override string Name => "Part Inspector";
		public override string Author => "Ceres et al.";
		public override string Version => "0.1";
		public override string Description => "Inspect your stuff for integrity, condition, and dirtiness.";
		public override Game SupportedGames => Game.MyWinterCar;

		#region Mod setup and settings
		internal static SettingsDropDownList DisplayLocation;
		internal static SettingsDropDownList PartDisplayPrecision;
		internal static SettingsDropDownList ItemDisplayPrecision;
		internal static SettingsSlider TextUpdateFrequency;

		internal static SettingsCheckBox EnableBasicTrackers;
		internal static SettingsCheckBox EnableSimpleTrackers;
		internal static SettingsCheckBox EnableSparkPlugTrackers;
		internal static SettingsCheckBox EnableOilFilterTrackers;
		internal static SettingsCheckBox EnableFluidContainerTrackers;
		internal static SettingsCheckBox EnableFullnessContainerTrackers;
		internal static SettingsCheckBox EnableObjectVariants;

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
			DisplayLocation = Settings.AddDropDownList("displayLocation", "Display location",
				new string[] { "Part name", "Interaction text" }, 0, RefreshDisplayGUI);
			PartDisplayPrecision = Settings.AddDropDownList("displayPrecision", "Part inspection precision",
				new string[] { "Show exact information", "Show general description", "Show broken/not broken" }, 1);
			ItemDisplayPrecision = Settings.AddDropDownList("itemDisplayPrecision", "Item inspection precision",
				new string[] { "Show exact information", "Show general description" }, 1);
			TextUpdateFrequency = Settings.AddSlider("updateFrequency", "Text update frequency",
				0f, 10f, 10f, RebuildDisplays);

			Settings.AddHeader("Enable specific trackers", headingColor, Color.white);
			EnableBasicTrackers = Settings.AddCheckBox("enablePartTrackers", "Car part condition", true);
			EnableSimpleTrackers = Settings.AddCheckBox("enableSimpleTrackers", "Broken or intact (block and oil pans)", true);
			EnableSparkPlugTrackers = Settings.AddCheckBox("enableSparkPlugTrackers", "Spark plug wear", true);
			EnableOilFilterTrackers = Settings.AddCheckBox("enableOilFilterTrackers", "Oil filter dirtiness", true);
			EnableFluidContainerTrackers = Settings.AddCheckBox("enableFluidContainerTrackers", "Fluid container fullness", true);
			EnableFullnessContainerTrackers = Settings.AddCheckBox("enableOtherFullnessTrackers", "Coffee and charcoal fullness", true);
			Settings.AddText("Includes brake fluid, motor oil, two-stroke fuel, and coolant canisters.");
			EnableObjectVariants = Settings.AddCheckBox("enableObjectVariants", "Identify object variants", true);
			Settings.AddText("A part's variant/model/etc. will be included in its displayed name; instrument panels, grilles, and so on.");

			Settings.AddHeader("Logging", headingColor, Color.white);
			SettingLogVerification = Settings.AddCheckBox("logVerification", "Log object verification", false);
			SettingLogNewTrackers = Settings.AddCheckBox("logNewTrackers", "Log new trackers", false);
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
			SparkPlug = 4,
			Fullness = 5,
			Quantity = 6,
			Variant = 7
		}

		/// <summary>
		/// Used to track initialization info for fullness trackers, which are the most complex tracker type by far due to accounting for many items.
		/// This struct can be used to designate the name of the FSM variable that's being tracked, as well as its maximum value (for percentage/ratio calculations)
		/// and whether or not it's a fluid (for deciding whether to display liters remaining or just the percentage left.)
		/// </summary>
		private struct TrackerInfo
		{
			internal TrackerType TrackerType;
			internal string ValueKey;
			internal float MaxValue;
			internal bool IsFluid;
			internal string FsmName;

			internal TrackerInfo(TrackerType TrackerType, string ValueKey = default, float MaxValue = default, bool IsFluid = default, string FsmName = default)
			{
				this.TrackerType = TrackerType;
				this.ValueKey = ValueKey;
				this.MaxValue = MaxValue;
				this.IsFluid = IsFluid;
				this.FsmName = FsmName;
			}
		}

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
			{ "Engine Block(VINXX)", TrackerType.Simple },
			{ "Oilpan(VINXX)", TrackerType.Simple },

			{ "automatic transmission fluid(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 1f, IsFluid: true ) },
			{ "brake fluid(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 1f, IsFluid: true ) },
			{ "two stroke fuel(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 5f, IsFluid: true ) },
			{ "motor oil(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 4f, IsFluid: true ) },
			{ "coolant(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 10f, IsFluid: true ) },

			{ "Oil filter(VINXX)", TrackerType.OilFilter },
			{ "spark plug(Clone)", TrackerType.SparkPlug },
			{ "spray can(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 100f ) },
			{ "mosquito spray(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 100f ) },
			{ "Fire Extinguisher(VINXX)", new TrackerInfo(TrackerType.Fullness, MaxValue: 100f, FsmName: "Data" ) },
			{ "ground coffee(itemx)", new TrackerInfo(TrackerType.Fullness, "Ground", 100f ) },
			{ "grill charcoal(itemx)", new TrackerInfo(TrackerType.Fullness, "Contents", 140f ) },

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

		private FsmVariables _plyCam;
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
			ModConsole.Print($"[PI] {Message}");
		}
		#endregion

		#region Main functions
		private void Mod_OnUpdate()
		{
			UpdateDisplays();
			UpdateInspection();
		}

		private void Mod_OnLoad()
		{
			_wearTrackers = new Dictionary<GameObject, BaseWearTracker>();
			RefreshDisplayGUI();
			RebuildDisplays();
			ModConsole.Print("Detecting player camera...");
			_plyCam = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/Hand").GetPlayMaker("PickUp").FsmVariables;
			ModConsole.Print("Player camera located.");
			ModConsole.Print($"{Name} version {Version} has been initialized!");
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
						PrintToConsole($"Found a tracker of type {kvp.Value.GetType()} with a null object. Adding to removal queue.", ConsoleMessageScope.Verification);
						toRemove.Add(kvp.Key);
						continue;
					}
					kvp.Value.BuildDisplayText();
				}
				foreach (var obj in toRemove)
				{
					PrintToConsole("Removing null tracker...", ConsoleMessageScope.Verification);
					_wearTrackers.Remove(obj);
				}
			}
		}

		/// <summary>
		/// Raycasts to find if the player is looking at a part. If so, displays the text from that part's wear tracker.
		/// </summary>
		private void UpdateInspection()
		{
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
		private void RefreshDisplayGUI() => _displayGui = PlayMakerGlobals.Instance.Variables.FindFsmString(DisplayLocation.GetSelectedItemIndex() == 0 ? "PickedPart" : "GUIinteraction");

		/// <summary>
		/// Simple wrapper to adjust relevant values when update frequency settings are changed.
		/// </summary>
		private void RebuildDisplays()
		{
			_updateTimer = 0f;
			_timeBetweenUpdates = TextUpdateFrequency.GetValue();
		}

		/// <summary>
		/// Creates a wear tracker component for the provided <see cref="GameObject"/>.
		/// </summary>
		/// <param name="gameObj">The <see cref="GameObject"/> that will begin being tracked.</param>
		/// <param name="trackerInfo">A <see cref="TrackerType"/> or <see cref="TrackerInfo"/> for the object.
		/// If null, it will fall back to <see cref="TrackerType.Standard"/>.</param>
		private void CreateTrackerForPart(GameObject gameObj, object trackerInfo = null)
		{
			TrackerType tt = TrackerType.Standard;

			string key = "Fluid";
			float max = 1f;
			bool isFluid = false;
			string fsmName = "Use";
			if (trackerInfo is TrackerType t)
				tt = t;
			else if (trackerInfo is TrackerInfo ti)
			{
				tt = ti.TrackerType;
				isFluid = ti.IsFluid;
				if (ti.ValueKey != default)
					key = ti.ValueKey;
				if (ti.MaxValue != default)
					max = ti.MaxValue;
				if (ti.FsmName != default)
					fsmName = ti.FsmName;
			}
			else if (trackerInfo is VariantInfo)
			{
				tt = TrackerType.Variant;
			}
			if (SettingLogVerification.GetValue())
				ModConsole.Print($"Tracker type: {tt}");
			BaseWearTracker bwt = null;
			Type newTrackerType = null;
			if (SettingLogVerification.GetValue())
				ModConsole.Print("Choosing new type...");
			switch (tt)
			{
				case TrackerType.Standard:
					if (!EnableBasicTrackers.GetValue())
						break;
					newTrackerType = typeof(StandardWearTracker);
					break;
				case TrackerType.Simple:
					if (!EnableSimpleTrackers.GetValue())
						break;
					newTrackerType = typeof(SimpleWearTracker);
					break;
				case TrackerType.OilFilter:
					if (!EnableOilFilterTrackers.GetValue())
						break;
					newTrackerType = typeof(OilFilterTracker);
					break;
				case TrackerType.SparkPlug:
					if (!EnableSparkPlugTrackers.GetValue())
						break;
					newTrackerType = typeof(SparkPlugTracker);
					break;
				case TrackerType.Fullness:
					if ((isFluid && !EnableFluidContainerTrackers.GetValue()) || (!isFluid && !EnableFullnessContainerTrackers.GetValue()))
						break;
					FullnessTracker ft = gameObj.AddComponent<FullnessTracker>();
					ft.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, fsmName).FsmVariables, max, key, isFluid);
					bwt = ft;
					break;
				case TrackerType.Quantity:
					QuantityTracker qt = gameObj.AddComponent<QuantityTracker>(); // qt uwu
					qt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, fsmName).FsmVariables);
					bwt = qt;
					break;
				case TrackerType.Variant:
					VariantTracker vt = gameObj.AddComponent<VariantTracker>();
					vt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Data").FsmVariables, trackerInfo);
					bwt = vt;
					break;
			}
			// We have a convenient thing going for us here with a bunch of different types of part:
			// they all keep their integrity variables in a playmaker with the name "Data"
			// as such, instead of copy-pasting all the relevant logic, we do some evil code here to apply a tracker of the relevant type
			// as determined by the part we're looking.
			// it's technically cleaner!
			if (newTrackerType != null && typeof(BaseWearTracker).IsAssignableFrom(newTrackerType))
			{
				if (SettingLogVerification.GetValue())
					ModConsole.Print($"Initializing new tracker  (type: {newTrackerType})");
				BaseWearTracker bt = (BaseWearTracker)gameObj.AddComponent(newTrackerType);
				bt.Initialize(gameObj.name, PlayMakerExtensions.GetPlayMaker(gameObj, "Data").FsmVariables);
				bwt = bt;
			}
			if (bwt != null)
			{
				bwt.BuildDisplayText();
				_wearTrackers.Add(gameObj, bwt);
			}
			if (SettingLogVerification.GetValue())
				ModConsole.Print($"Wear tracker complete.");
			if (SettingLogVerification.GetValue())
				ModConsole.Print($"A wear tracker component of type {bwt.GetType()} was added to a GameObject named \"{gameObj.name}\".");
		}
		#endregion
	}
}
