using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks the exact integrity of the assigned part using the provided information. Broken parts will display as broken.
	/// </summary>
	internal class StandardWearTracker : BaseTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			DisplayText = $"{InitialName} - {GetDescriptor(FsmVariables.GetFsmFloat("Wear").Value)}";
		}

		internal static string GetDescriptor(float WearVal)
		{
			string newText;
			if (WearVal <= 0) // Always display broken parts as just "broken"
				newText = "Broken";
			else
			{
				switch (PartInspectorScript.SettingDisplayPrecision.GetSelectedItemIndex())
				{
					case 1: // General description
						string descriptor;
						if (WearVal >= 90)
							descriptor = "mint";
						else if (WearVal >= 80)
							descriptor = "great";
						else if (WearVal >= 65)
							descriptor = "good";
						else if (WearVal >= 50)
							descriptor = "decent";
						else if (WearVal >= 35)
							descriptor = "shoddy";
						else if (WearVal >= 25)
							descriptor = "poor";
						else if (WearVal >= 15)
							descriptor = "bad";
						else
							descriptor = "terrible";
						newText = $"In {descriptor} condition";
						break;
					case 2: // Broken/not broken
						newText = "Intact";
						break;
					default: // Exact percentage
						newText = Mathf.RoundToInt(WearVal) + "%";
						break;
				}
			}
			return newText;
		}
	}
}
