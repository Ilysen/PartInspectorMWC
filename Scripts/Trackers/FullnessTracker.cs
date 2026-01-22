using HutongGames.PlayMaker;
using System;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks fullness. What this means varies depending on the part; this tracker just keeps tabs on a specific value
	/// as well as the maximum amount that it previously sat at. This is used for fluid containers, ground coffee, etc.;
	/// anything that has an amount of contents that can be used up over time can be handled by one of these.
	/// <br/><br/>
	/// Because they already have a built-in way of checking their contents, gasoline and diesel canisters are excluded.
	/// </summary>
	internal class FullnessTracker : BaseTracker
	{
		/// <summary>
		/// Used to track initialization info for fullness trackers, which have to account for many different values.
		/// This struct can be used to designate the name of the FSM variable that's being tracked, as well as its maximum value (for percentage/ratio calculations)
		/// and whether or not it's a fluid (for deciding whether to display liters remaining or just the percentage left.)
		/// </summary>
		internal struct FullnessInfo
		{
			internal string ValueKey;
			internal float MaxValue;
			internal string FsmName;
			internal bool DisplayAsFluid;

			internal FullnessInfo(string FsmName = "Use", string ValueKey = "Fluid", float MaxValue = default, bool DisplayAsFluid = default)
			{
				this.ValueKey = ValueKey;
				this.MaxValue = MaxValue;
				this.FsmName = FsmName;
				this.DisplayAsFluid = DisplayAsFluid;
			}
		}

		/// <summary>
		/// The max fluid that this container can hold. Assigned in <see cref="Ceres.PartInspectorMWC.CreateTrackerForPart(GameObject, PartInspectorMWC.TrackerType)"/> during initialization and used to calculate fractions (i.e. "half full") in <see cref="BuildDisplayText"/>.
		/// </summary>
		private float _maxFullness = 1f;

		/// <summary>
		/// The FSM variable that we're keeping track of. This varies by instance.
		/// </summary>
		private FsmFloat _fullness;

		/// <summary>
		/// If true, exact percentage display will show the remaining mL instead. If false, it'll show the percentage as usual.
		/// </summary>
		private bool _displayAsFluid = true;

		/// <inheritdoc/>
		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars);
			FullnessInfo fi = (FullnessInfo)extraArgs[0];
			_maxFullness = fi.MaxValue;
			_fullness = FsmVariables.GetFsmFloat(fi.ValueKey);
			_displayAsFluid = fi.DisplayAsFluid;
		}

		/// <inheritdoc/>
		internal override float GetWearPercentage() => (GetFullnessLevel() / _maxFullness) * 100;

		/// <summary>
		/// Gets the remaining fluid for this tracker.
		/// </summary>
		private float GetFullnessLevel() => _fullness.Value;

		/// <summary>
		/// How full this tracker was last frame.
		/// If current fullness does not equal this value, we refresh the tracker's display text.
		/// </summary>
		private float _cachedFullnessLevel;

		// you thought I was a bespoke class, but it was me, MonoBehavior!
		private void Update()
		{
			// we do this so that the text updates in realtime while we're pouring a liquid, basically
			// skipping the usual refresh period makes things feel a lot snappier
			var curFullness = GetFullnessLevel();
			if (curFullness != _cachedFullnessLevel)
				BuildDisplayText();
			_cachedFullnessLevel = curFullness;
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			if (GetFullnessLevel() < 1)
			{
				DisplayText = string.Empty;
				return;
			}
			float fullnessLevel = GetWearPercentage();
			string newText;
			switch (PartInspectorScript.SettingItemDisplayPrecision.GetSelectedItemIndex())
			{
				case 1: // General description
					if (fullnessLevel >= 100)
						newText = "full";
					else if (fullnessLevel >= 90)
						newText = "nearly full";
					else if (fullnessLevel >= 75)
						newText = "three-quarters full";
					else if (fullnessLevel >= 51)
						newText = "over half full";
					else if (fullnessLevel < 51 && fullnessLevel > 49)
						newText = "exactly half full, nice!";
					else if (fullnessLevel >= 25)
						newText = "under half full";
					else if (fullnessLevel >= 10)
						newText = "one quarter full";
					else
						newText = "nearly empty";
					break;
				default: // Exact percentage
					if (_displayAsFluid)
					{
						float ml = Mathf.RoundToInt(GetFullnessLevel() * 1000);
						if (ml >= 1000) // 1 liter or above - truncate value to read something like "1.2 L"
							newText = $"{Math.Round(GetFullnessLevel(), 2)} L";
						else // Below 1 liter - display as exact mL value, like "372 mL"
							newText = $"{ml} mL";
					}
					else
					{
						newText = $"{Math.Round(fullnessLevel)}% full";
					}
					break;
			}
			DisplayText = $"{InitialName} - {newText}";
		}
	}
}
