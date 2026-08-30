using HutongGames.PlayMaker;
using System.Collections.Generic;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Flexible tracker type meant for the many loose objects that all track condition via a single value, so they don't all have to have bespoke types.
	/// </summary>
	internal class SingleValueTracker : BaseTracker
	{
		internal struct SingleValueInfo
		{
			/// <summary>
			/// The FSM to read from.
			/// </summary>
			internal string SingleValueFsm;

			/// <summary>
			/// The value to read from.
			/// </summary>
			internal string SingleValueKey;

			/// <summary>
			/// The human-readable name of this value, shown with precision set to "exact information". Can be empty.
			/// </summary>
			internal string ValueName;

			/// <summary>
			/// Dictionary of value thresholds and the keys associated with them, shown with precision set to "general description".<br/><br/>
			/// The internal logic uses <c>>=</c> to determine which threshold to use, so this dictionary <b>must</b> be descending!
			/// </summary>
			internal Dictionary<float, string> ValueThresholds;

			internal SingleValueInfo(string SingleValueFsm = default, string SingleValueKey = default, string ValueName = default, Dictionary<float, string> ValueThresholds = default)
			{
				this.SingleValueKey = SingleValueKey;
				this.SingleValueFsm = SingleValueFsm;
				this.ValueName = ValueName;
				this.ValueThresholds = ValueThresholds;
			}
		}

		private FsmFloat _theEponymousSingleValue;
		private string _valueName;
		private Dictionary<float, string> _valueThresholds;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			SingleValueInfo si = (SingleValueInfo)extraArgs[0];
			_theEponymousSingleValue = FsmVariables.GetFsmFloat(si.SingleValueKey).Value;
			_valueName = si.ValueName;
			_valueThresholds = si.ValueThresholds;
		}

		internal override void BuildDisplayText()
		{
			string newText = string.Empty;
			float valueNum = _theEponymousSingleValue.Value;
			switch (PartInspectorScript.SettingDisplayPrecision.GetSelectedItemIndex())
			{
				case 1: // General description
					foreach (var kvp in _valueThresholds)
					{
						if (valueNum >= kvp.Key)
						{
							newText = kvp.Value;
							break;
						}
					}
					if (newText == string.Empty)
						newText = "Error, report this as a bug";
					break;
				default: // Exact percentage
					newText = $"{Mathf.RoundToInt(GetPercentage())}%{(_valueName != string.Empty ? $" {_valueName}" : "")}";
					break;
			}
			DisplayText = $"{InitialName} - {newText}";
		}

		// probably not great to just assume that it's a percentage here but hey, nobody's keeping score here
		internal override float GetPercentage() => _theEponymousSingleValue.Value;
	}
}
