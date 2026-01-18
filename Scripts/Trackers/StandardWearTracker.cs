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
		/// <summary>
		/// The data FSM for this part.
		/// </summary>
		private FsmVariables _dataFsm;

		/// <inheritdoc/>
		internal override void Initialize(string initName, params object[] extraArgs)
		{
			if (PartInspectorScript.SettingLogVerification.GetValue())
				ModConsole.Print($"Initializing new standard wear tracker: {initName}");
			base.Initialize(initName);
			_dataFsm = (FsmVariables)extraArgs[0];
		}

		/// <inheritdoc/>
		internal override float GetWearPercentage() => _dataFsm.GetFsmFloat("Wear").Value;

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			float partWear = GetWearPercentage();
			string newText;
			if (partWear <= 0) // Always display broken parts as just "broken"
				newText = "Broken";
			else
			{
				switch (PartInspectorScript.PartDisplayPrecision.GetSelectedItemIndex())
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
