using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks how dirty an oil filter is.
	/// </summary>
	internal class FreshnessTracker : BaseTracker
	{
		internal struct FreshnessInfo
		{
			internal float MaxFreshness;

			internal FreshnessInfo(float MaxFreshness = default)
			{
				this.MaxFreshness = MaxFreshness;
			}
		}

		private FsmFloat _curFreshness;
		private float _maxFreshness;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			FreshnessInfo fi = (FreshnessInfo)extraArgs[0];
			_maxFreshness = fi.MaxFreshness - 1; // items spoil at 1 condition, not 0
			_curFreshness = fsmVars.GetFsmFloat("Condition");
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			string newText = null;
			float freshnessPercent = GetWearPercentage();
			if (freshnessPercent <= 0) // spoiled -- skip
			{
				DisplayText = null;
				return;
			}
			switch (PartInspectorScript.SettingDisplayPrecision.GetSelectedItemIndex())
			{
				case 1: // General description
					if (freshnessPercent <= 25)
						newText = "Moldy";
					else if (freshnessPercent <= 50)
						newText = "Stale";
					break;
				default: // Exact percentage
					newText = $"{Mathf.RoundToInt(freshnessPercent)}% fresh";
					break;
			}
			DisplayText = $"{(newText != null ? $"{newText} " : "")}{InitialName}";
		}

		internal override float GetWearPercentage() => (_curFreshness.Value / _maxFreshness) * 100;
	}
}
