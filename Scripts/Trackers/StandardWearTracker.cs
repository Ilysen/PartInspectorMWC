using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks the exact integrity of the assigned part using the provided information. Broken parts will display as broken.
	/// </summary>
	internal class StandardWearTracker : BaseWearTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			float partWear = FsmVariables.GetFsmFloat("Wear").Value;
			string newText;
			if (partWear <= 0) // Always display broken parts as just "broken"
				newText = "Broken";
			else
			{
				switch (PartInspectorScript.SettingDisplayPrecision.GetSelectedItemIndex())
				{
					case 1: // General description
						string descriptor;
						if (partWear >= 90)
							descriptor = "mint";
						else if (partWear >= 80)
							descriptor = "great";
						else if (partWear >= 65)
							descriptor = "good";
						else if (partWear >= 50)
							descriptor = "decent";
						else if (partWear >= 35)
							descriptor = "shoddy";
						else if (partWear >= 25)
							descriptor = "poor";
						else if (partWear >= 15)
							descriptor = "bad";
						else
							descriptor = "terrible";
						newText = $"In {descriptor} condition";
						break;
					case 2: // Broken/not broken
						newText = "Intact";
						break;
					default: // Exact percentage
						newText = Mathf.RoundToInt(partWear) + "%";
						break;
				}
			}
			DisplayText = $"{InitialName} - {newText}";
		}
	}
}
