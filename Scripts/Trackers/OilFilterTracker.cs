using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks how dirty an oil filter is.
	/// </summary>
	internal class OilFilterTracker : BaseWearTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			string newText;
			float effectiveFilth = FsmVariables.GetFsmFloat("Dirt").Value;
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
