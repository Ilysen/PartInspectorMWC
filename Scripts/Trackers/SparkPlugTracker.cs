using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks how worn-down a spark plug is.
	/// </summary>
	internal class SparkPlugTracker : BaseWearTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			string newText;
			float effectiveWear = GetWearPercentage();
			switch (PartInspectorScript.PartDisplayPrecision.GetSelectedItemIndex())
			{
				case 1: // General description
					string descriptor;
					if (effectiveWear >= 90)
						descriptor = "mint";
					else if (effectiveWear >= 65)
						descriptor = "great";
					else if (effectiveWear >= 25)
						descriptor = "decent";
					else if (effectiveWear >= 15)
						descriptor = "bad";
					else
						descriptor = "terrible";
					newText = $"In {descriptor} condition";
					break;
				case 2: // Broken/not broken
					newText = effectiveWear >= 85 ? "Broken" : "Intact";
					break;
				default: // Exact percentage
					newText = Mathf.RoundToInt(effectiveWear) + "%";
					break;
			}
			DisplayText = $"{InitialName} - {newText}";
		}
	}
}
