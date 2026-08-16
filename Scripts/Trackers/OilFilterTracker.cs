using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Tracks how dirty an oil filter is.
	/// </summary>
	internal class OilFilterTracker : BaseTracker
	{
		private FsmFloat _dirt;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			_dirt = FsmVariables.GetFsmFloat("Dirt").Value;
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			string newText;
			float effectiveFilth = _dirt.Value;
			switch (PartInspectorScript.SettingDisplayPrecision.GetSelectedItemIndex())
			{
				case 1: // General description
					if (effectiveFilth >= 80)
						newText = "Filthy";
					else if (effectiveFilth >= 60)
						newText = "Dirty";
					else if (effectiveFilth >= 40)
						newText = "Grimy";
					else if (effectiveFilth >= 20)
						newText = "Clean";
					else
						newText = "Brand new";
					break;
				default: // Exact percentage
					newText = $"{Mathf.RoundToInt(effectiveFilth)}% dirty";
					break;
			}
			DisplayText = $"{InitialName} - {newText}";
		}
	}
}
