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
		/// <summary>
		/// The key used to fetch the wear value of this part.
		/// </summary>
		private string _wearKey;

		/// <summary>
		/// Used to track if this part is broken or not. My Summer Car separates these, so we gotta too.
		/// </summary>
		private FsmVariables _dbInfo;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			if (PartInspectorScript.IsMSC)
			{
				_wearKey = (string)extraArgs[0];
				_dbInfo = (FsmVariables)extraArgs[1];
			}
			else
				_wearKey = "Wear";
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			float effectiveWear;
			// in MSC, broken parts track whether or not they're broken using a separate variable
			// as a result, we have to override the usual wear value if they're busted
			if (PartInspectorScript.IsMSC && _dbInfo.GetFsmBool("Damaged").Value)
				effectiveWear = 0;
			else
				effectiveWear = FsmVariables.GetFsmFloat(_wearKey).Value;
			DisplayText = $"{InitialName} - {GetDescriptor(effectiveWear)}";
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
