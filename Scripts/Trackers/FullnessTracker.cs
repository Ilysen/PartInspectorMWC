using HutongGames.PlayMaker;
using System;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
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
			internal float MinValue;
			internal string FsmName;
			internal bool DisplayAsFluid;
			internal string ChildObjectName;

			internal FullnessInfo(string FsmName = "Use", string ValueKey = "Fluid", float MaxValue = default, float MinValue = 0, bool DisplayAsFluid = default, string ChildObjectName = null)
			{
				this.ValueKey = ValueKey;
				this.MaxValue = MaxValue;
				this.MinValue = MinValue;
				this.FsmName = FsmName;
				this.DisplayAsFluid = DisplayAsFluid;
				this.ChildObjectName = ChildObjectName;
			}
		}

		/// <summary>
		/// The max fluid that this container can hold. Assigned in <see cref="Ceres.PartInspector.CreateTrackerForPart(GameObject, PartInspector.TrackerType)"/> during initialization and used to calculate fractions (i.e. "half full") in <see cref="BuildDisplayText"/>.
		/// </summary>
		private float _maxFullness = 1f;

		private float _minFullness = 0f;

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
			_minFullness = fi.MinValue;
			_fullness = FsmVariables.GetFsmFloat(fi.ValueKey);
			_displayAsFluid = fi.DisplayAsFluid;
		}

		/// <inheritdoc/>
		internal override float GetWearPercentage() => (_fullness.Value / _maxFullness) * 100;

		/// <summary>
		/// How full this tracker was last frame.
		/// </summary>
		private float _cachedFullnessLevel;

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			if (_fullness.Value <= _minFullness)
			{
				if (_cachedFullnessLevel > _minFullness)
					DisplayText = "Empty";
				else
					DisplayText = string.Empty;
				return;
			}
			float percentFull = GetWearPercentage();
			string newText;
			switch (PartInspectorScript.SettingItemDisplayPrecision.GetSelectedItemIndex())
			{
				case 1: // General description
					if (percentFull >= 100)
						newText = "full";
					else if (percentFull >= 90)
						newText = "nearly full";
					else if (percentFull >= 75)
						newText = "three-quarters full";
					else if (percentFull >= 51)
						newText = "over half full";
					else if (percentFull < 51 && percentFull > 49)
						newText = "exactly half full, nice!";
					else if (percentFull >= 25)
						newText = "under half full";
					else if (percentFull >= 10)
						newText = "one quarter full";
					else
						newText = "nearly empty";
					break;
				default: // Exact percentage
					if (_displayAsFluid)
					{
						float ml = Mathf.RoundToInt(_fullness.Value * 1000);
						if (ml >= 1000) // 1 liter or above - truncate value to read something like "1.2 L"
							newText = $"{Math.Round(_fullness.Value, 2)} L";
						else // Below 1 liter - display as exact mL value, like "372 mL"
							newText = $"{ml} mL";
					}
					else
					{
						newText = $"{Math.Round(percentFull)}% full";
					}
					break;
			}
			DisplayText = $"{InitialName} - {newText}";
			_cachedFullnessLevel = _fullness.Value;
		}
	}
}
