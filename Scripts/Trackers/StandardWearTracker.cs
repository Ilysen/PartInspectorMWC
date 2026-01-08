using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks the exact integrity of the assigned part using the provided information. Broken parts will display as broken.
	/// </summary>
	internal class StandardWearTracker : BaseWearTracker
	{
		/// <summary>
		/// The key used to fetch the wear value of this part from <see cref="_wearValues"/>.
		/// </summary>
		private string _wearKey;

		/// <summary>
		/// These variables contain the wear value for this part.
		/// </summary>
		private FsmVariables _wearValues;

		/// <summary>
		/// Used to track if this part is broken or not. My Summer Car separates these, so we gotta too.
		/// </summary>
		private FsmVariables _dbInfo;

		/// <inheritdoc/>
		internal override void Initialize(string initName, params object[] extraArgs)
		{
			base.Initialize(initName);
			_wearKey = (string)extraArgs[0];
			_wearValues = (FsmVariables)extraArgs[1];
			_dbInfo = (FsmVariables)extraArgs[2];
		}

		/// <inheritdoc/>
		internal override float GetWearPercentage()
		{
			float partWear = 0;
			// Broken parts technically keep their wear value as-is; whether or not they're intact is tracked with a separate variable
			// As a result, we skip checking for wear values on broken parts, and instead just treat them as having zero integrity
			if (!_dbInfo.GetFsmBool("Damaged").Value && _wearKey != null)
				partWear = _wearValues.GetFsmFloat(_wearKey).Value;
			return partWear;
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			float partWear = GetWearPercentage();
			string newText;
			if (partWear <= 0) // Always display broken parts as just "broken"
				newText = "Broken";
			else
			{
				switch (Ceres.PartInspectorMWC.PartDisplayPrecision.GetSelectedItemIndex())
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
						else if (partWear >= 20)
							descriptor = "poor";
						else if (partWear >= 10)
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
