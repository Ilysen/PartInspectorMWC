using Ceres.PartInspectorMWC.Trackers;
using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ceres.PartInspector
{
	public class PartInspectorMWC : Mod
	{
		public override string ID => "Ceres_PartInspectorMWC";
		public override string Name => "Part Inspector";
		public override string Author => "Ceres et al.";
		public override string Version => "0.1";
		public override string Description => "Inspect your stuff for integrity, condition, and dirtiness.";

		#region Mod setup and settings
		internal static SettingsDropDownList DisplayLocation;
		internal static SettingsDropDownList PartDisplayPrecision;
		internal static SettingsDropDownList ItemDisplayPrecision;
		internal static SettingsSlider TextUpdateFrequency;

		internal static SettingsCheckBox EnableBasicTrackers;
		internal static SettingsCheckBox EnableSimpleTrackers;
		internal static SettingsCheckBox EnableAlternatorBeltTrackers;
		internal static SettingsCheckBox EnableSparkPlugTrackers;
		internal static SettingsCheckBox EnableOilFilterTrackers;
		internal static SettingsCheckBox EnableFluidContainerTrackers;
		internal static SettingsCheckBox EnableFullnessContainerTrackers;

		internal static SettingsCheckBox VerboseLogging;

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
			EnableAlternatorBeltTrackers = Settings.AddCheckBox("enableAlternatorBeltTrackers", "Alternator belt wear", true);
			EnableSparkPlugTrackers = Settings.AddCheckBox("enableSparkPlugTrackers", "Spark plug wear", true);
			EnableOilFilterTrackers = Settings.AddCheckBox("enableOilFilterTrackers", "Oil filter dirtiness", true);
			EnableFluidContainerTrackers = Settings.AddCheckBox("enableFluidContainerTrackers", "Fluid container fullness", true);
			EnableFullnessContainerTrackers = Settings.AddCheckBox("enableOtherFullnessTrackers", "Coffee and charcoal fullnes", true);
			Settings.AddText("Includes brake fluid, motor oil, two-stroke fuel, and coolant canisters.");

			Settings.AddHeader("Debug", headingColor, Color.white);
			VerboseLogging = Settings.AddCheckBox("verboseLogging", "Verbose logging", false);
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
			AlternatorBelt = 5,
			Fullness = 6,
			Quantity = 7
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

			internal TrackerInfo(TrackerType TrackerType, string ValueKey = default, float MaxValue = default, bool IsFluid = default)
			{
				this.TrackerType = TrackerType;
				this.ValueKey = ValueKey;
				this.MaxValue = MaxValue;
				this.IsFluid = IsFluid;
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

			{ "block(Clone)", TrackerType.Simple },
			{ "oilpan(Clone)", TrackerType.Simple },
			{ "alternator belt(Clone)", TrackerType.AlternatorBelt },

			{ "brake fluid(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 1f, IsFluid: true ) },
			{ "two stroke fuel(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 5f, IsFluid: true ) },
			{ "motor oil(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 4f, IsFluid: true ) },
			{ "coolant(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 10f, IsFluid: true ) },

			{ "oil filter(Clone)", TrackerType.OilFilter },
			{ "spark plug(Clone)", TrackerType.SparkPlug },
			{ "spray can(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 100f ) },
			{ "mosquito spray(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 100f ) },
			{ "fire extinguisher(itemx)", new TrackerInfo(TrackerType.Fullness, MaxValue: 100f ) },
			{ "ground coffee(itemx)", new TrackerInfo(TrackerType.Fullness, "Ground", 100f ) },
			{ "grill charcoal(itemx)", new TrackerInfo(TrackerType.Fullness, "Contents", 140f ) },

			{ "spark plug box(Clone)", TrackerType.Quantity },
			{ "r20 battery box(Clone)", TrackerType.Quantity },
			{ "fuse package(Clone)", TrackerType.Quantity },
		};

		/// <summary>
		/// The FSM variables used to track the Satsuma's part wear. We reference this a lot, so we save it early.
		/// </summary>
		private FsmVariables _satsumaVars;

		/// <summary>
		/// A list of all FSMs used in the motor database. These are where the game keeps track of if parts are installed, broken, etc - but not wear-and-tear, which exists on independently FSMs on each part.
		/// </summary>
		private List<PlayMakerFSM> _motorDb;

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
		#endregion

		#region Main functions
		private void Mod_OnUpdate()
		{
			UpdateDisplays();
			UpdateInspection();
		}

		private void Mod_OnLoad()
		{
			_satsumaVars = PlayMakerExtensions.GetPlayMaker(GameObject.Find("SATSUMA(557kg, 248)").transform.Find("CarSimulation/MechanicalWear").gameObject, "Data").FsmVariables;
			_motorDb = new List<PlayMakerFSM>();
			_wearTrackers = new Dictionary<GameObject, BaseWearTracker>();
			foreach (PlayMakerFSM fsm in GameObject.Find("Database/DatabaseMotor").GetComponentsInChildren<PlayMakerFSM>())
			{
				if (VerboseLogging.GetValue())
					ModConsole.Print($"Adding fsm to database: {fsm.gameObject.name}");
				_motorDb.Add(fsm);
			}
			RefreshDisplayGUI();
			RebuildDisplays();
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
						if (VerboseLogging.GetValue())
							ModConsole.Print($"Found a tracker of type {kvp.Value.GetType()} with a null object. Adding to removal queue.");
						toRemove.Add(kvp.Key);
						continue;
					}
					kvp.Value.BuildDisplayText();
				}
				foreach (var obj in toRemove)
				{
					if (VerboseLogging.GetValue())
						ModConsole.Print($"Removing null tracker...");
					_wearTrackers.Remove(obj);
				}
			}
		}

		/// <summary>
		/// Raycasts to find if the player is looking at a part. If so, displays the text from that part's wear tracker.
		/// </summary>
		private void UpdateInspection()
		{
			RaycastHit hit = UnifiedRaycast.GetRaycastHit();
			if (hit.distance <= 1f && hit.collider?.gameObject != null)
			{
				// If we're aiming at a part designated in _partNames, continue
				GameObject go = hit.collider.gameObject;
				if (!_partNames.Keys.Contains(go.name))
				{
					// We check for a parent object because some parts (like the water pump) have children with colliders
					if (!go.transform.parent?.gameObject || !_partNames.Keys.Contains(go.transform.parent.gameObject.name))
						return;
					go = go.transform.parent.gameObject;
				}
				// Does the object already have a wear tracker? Display the part's integrity data
				if (_wearTrackers.Keys.Contains(go))
				{
					BaseWearTracker wt = _wearTrackers[go];
					_displayGui.Value = wt.DisplayText;
				}
				// Otherwise, add a wear tracker component. We'll use the data next frame
				// We avoid doing this on load so that this way it's compatible with objects that can show up during gameplay
				else
				{
					if (VerboseLogging.GetValue())
						ModConsole.Print($"Detected a valid object named \"{go.name}\". Adding wear tracker.");
					CreateTrackerForPart(go, _partNames[go.name]);
				}
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
		/// Info will be taken from <see cref="_partNames"/> to create the component; invalid objects will thus cause this function to throw an error.
		/// </summary>
		/// <param name="go">The <see cref="GameObject"/> that will begin being tracked.</param>
		private void CreateTrackerForPart(GameObject go, object info = null)
		{
			TrackerType tt = TrackerType.Standard;
			string key = "Fluid";
			float max = 1f;
			bool isFluid = false;
			if (info is TrackerType t)
				tt = t;
			else if (info is TrackerInfo ti)
			{
				tt = ti.TrackerType;
				isFluid = ti.IsFluid;
				if (ti.ValueKey != default)
					key = ti.ValueKey;
				if (ti.MaxValue != default)
					max = ti.MaxValue;
			}
			FsmVariables dbInfo = null;
			foreach (PlayMakerFSM fsm in _motorDb)
			{
				FsmVariables vars = fsm.FsmVariables;
				if (vars.GetFsmString("UniqueTag").Value == go.name)
				{
					dbInfo = vars;
					break;
				}
			}
			BaseWearTracker bwt = null;
			Type newTrackerType = null;
			switch (tt)
			{
				case TrackerType.Standard:
					if (!EnableBasicTrackers.GetValue())
						break;
					StandardWearTracker swt = go.AddComponent<StandardWearTracker>();
					swt.Initialize(go.name, "Wear" + _partNames[go.name], _satsumaVars, dbInfo);
					bwt = swt;
					break;
				case TrackerType.Simple:
					if (!EnableSimpleTrackers.GetValue())
						break;
					SimpleWearTracker smt = go.AddComponent<SimpleWearTracker>();
					smt.Initialize(go.name, dbInfo);
					bwt = smt;
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
				case TrackerType.AlternatorBelt:
					if (!EnableAlternatorBeltTrackers.GetValue())
						break;
					newTrackerType = typeof(AlternatorBeltTracker);
					break;
				case TrackerType.Fullness:
					if ((isFluid && !EnableFluidContainerTrackers.GetValue()) || (!isFluid && !EnableFullnessContainerTrackers.GetValue()))
						break;
					FullnessTracker ft = go.AddComponent<FullnessTracker>();
					ft.Initialize(go.name, PlayMakerExtensions.GetPlayMaker(go, "Use").FsmVariables, max, key, isFluid);
					bwt = ft;
					break;
				case TrackerType.Quantity:
					newTrackerType = typeof(QuantityTracker);
					break;
			}
			// We have a convenient thing going for us here with a bunch of different types of part:
			// they all keep their integrity variables in a playmaker with the name "Use"
			// as such, instead of copy-pasting all the relevant logic, we do some evil code here to apply a tracker of the relevant type
			// as determined by the part we're looking.
			// it's technically cleaner!
			if (newTrackerType != null && typeof(BaseWearTracker).IsAssignableFrom(newTrackerType))
			{
				BaseWearTracker bt = (BaseWearTracker)go.AddComponent(newTrackerType);
				bt.Initialize(go.name, PlayMakerExtensions.GetPlayMaker(go, "Use").FsmVariables);
				bwt = bt;
			}
			if (bwt != null)
			{
				bwt.BuildDisplayText();
				_wearTrackers.Add(go, bwt);
			}
			if (VerboseLogging.GetValue())
				ModConsole.Print($"A wear tracker component of type {bwt.GetType()} was added to a GameObject named \"{go.name}\".");
		}
		#endregion
	}
}
