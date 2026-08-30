using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Tracks the freshness of food items.
	/// </summary>
	internal class FreshnessTracker : BaseTracker
	{
		internal struct FreshnessInfo
		{
			/// <summary>
			/// The maximum freshness of this food item. This varies per type, so it needs to change per instance.
			/// </summary>
			internal float MaxFreshness;

			/// <summary>
			/// If true, indicates that this food can change its max freshness during regular play.
			/// This is generally used for cookable stuff.
			/// </summary>
			internal bool CanChangeMaxFreshness;

			/// <summary>
			/// If <c><see cref="CanChangeMaxFreshness"/></c> is true, then max freshness will be set to this value if the parent object's name ever changes.
			/// </summary>
			internal float AltMaxFreshness;


			internal FreshnessInfo(float MaxFreshness = default, bool CanChangeMaxFreshness = default, float AltMaxFreshness = default)
			{
				this.MaxFreshness = MaxFreshness;
				this.CanChangeMaxFreshness = CanChangeMaxFreshness;
				this.AltMaxFreshness = AltMaxFreshness;
			}
		}

		private FsmFloat _curFreshness;
		private float _maxFreshness;
		private float _altMaxFreshness;
		private bool _canChangeMaxFreshness;
		private string _startingName;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			FreshnessInfo fi = (FreshnessInfo)extraArgs[0];
			_maxFreshness = fi.MaxFreshness;
			if (fi.CanChangeMaxFreshness)
			{
				_canChangeMaxFreshness = fi.CanChangeMaxFreshness;
				_startingName = gameObject.name;
				_altMaxFreshness = fi.AltMaxFreshness;
			}
			_curFreshness = fsmVars.GetFsmFloat("Condition");
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			string newText = null;
			if (_curFreshness.Value <= 1) // spoiled -- skip
			{
				DisplayText = string.Empty;
				return;
			}
			// this is a hacky workaround for the fact that pike and moose meat start with 40 freshness,
			// but go all the way up to 100 upon being grilled, despite being the same item.
			// reloading a save with the grilled ones will correctly initialize it with the correct max freshness to begin with,
			// so this is purely to accommodate stuff that starts raw and then is grilled in the same game session
			if (_canChangeMaxFreshness && gameObject.name != _startingName)
			{
				_canChangeMaxFreshness = false;
				_maxFreshness = _altMaxFreshness;
				return;
			}
			float freshnessPercent = GetPercentage();
			switch (PartInspectorScript.SettingItemDisplayPrecision.GetSelectedItemIndex())
			{
				case 1: // General description
					if (freshnessPercent <= 10)
						newText = "Moldy";
					else if (freshnessPercent <= 30)
						newText = "Stale";
					else if (freshnessPercent <= 50)
						newText = "Fine";
					else if (freshnessPercent <= 70)
						newText = "Good";
					else
						newText = "Fresh";
					DisplayText = $"{(newText != null ? $"{newText} " : "")}{InitialName}";
					break;
				default: // Exact percentage
					DisplayText = $"{InitialName} ({Mathf.RoundToInt(freshnessPercent)}% fresh)";
					break;
			}
		}

		internal override float GetPercentage() => (_curFreshness.Value / _maxFreshness) * 100;
	}
}
