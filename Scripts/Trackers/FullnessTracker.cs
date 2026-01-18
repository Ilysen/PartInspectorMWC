using HutongGames.PlayMaker;
using System;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks fullness. What this means varies depending on the part; this tracker just keeps tabs on a specific value
	/// as well as the maximum amount that it previously sat at. This is used for fluid containers, ground coffee, etc;
	/// anything that has an amount of contents that can be used up over time can be handled by one of these.
	///
	/// Jerry cans are excluded because they already have a way to check their fullness.
	/// </summary>
	internal class FullnessTracker : BaseWearTracker
	{
		/// <summary>
		/// The FSM that keeps track of this fluid container's current contents.
		/// </summary>
		private FsmVariables _fullnessFsm;

		/// <summary>
		/// The max fluid that this container can hold. Assigned in <see cref="Ceres.PartInspectorMWC.CreateTrackerForPart(GameObject, PartInspectorMWC.TrackerType)"/> during initialization and used to calculate fractions (i.e. "half full") in <see cref="BuildDisplayText"/>.
		/// </summary>
		private float _maxFullness = 1f;

		/// <summary>
		/// The name of the FSM variable that we're keeping track of. This varies by instance.
		/// </summary>
		private string _fullnessKey = "Fluid";

		/// <summary>
		/// If true, exact percentage display will show the remaining mL instead. If false, it'll show the percentage as usual.
		/// </summary>
		private bool _isFluid = true;

		/// <inheritdoc/>
		internal override void Initialize(string initName, params object[] extraArgs)
		{
			base.Initialize(initName);
			_fullnessFsm = (FsmVariables)extraArgs[0];
			_maxFullness = (float)extraArgs[1];
			_fullnessKey = (string)extraArgs[2];
			_isFluid = (bool)extraArgs[3];
		}

		/// <inheritdoc/>
		internal override float GetWearPercentage() => (GetFullnessLevel() / _maxFullness) * 100;

		/// <summary>
		/// Gets the remaining fluid for this tracker.
		/// </summary>
		private float GetFullnessLevel() => _fullnessFsm.GetFsmFloat(_fullnessKey).Value;

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			string newText;
			float fullnessLevel = GetWearPercentage();
			if (fullnessLevel <= 0)
				return;
			switch (PartInspectorScript.ItemDisplayPrecision.GetSelectedItemIndex())
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
					if (_isFluid)
					{
						float ml = Mathf.RoundToInt(GetFullnessLevel() * 1000);
						if (ml >= 1000) // 1 liter or above - truncate value to read something like "1.2 L"
							newText = $"{System.Math.Round(GetFullnessLevel(), 2)} L";
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
