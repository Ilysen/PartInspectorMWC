using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Tracks the exact integrity of the assigned part using the provided information. Broken parts will display as broken.
	/// </summary>
	internal class PartConditionTracker : BaseTracker
	{
		private FsmFloat _wear;
		private float _cachedWear;
		private FsmBool _mscIsDamaged;

		internal virtual string GetPartName => InitialName;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			if (PartInspectorScript.IsMSC)
			{
				string wearKey = (string)extraArgs[0];
				FsmVariables motorDb = (FsmVariables)extraArgs[1];
				_wear = FsmVariables.GetFsmFloat(wearKey);
				_mscIsDamaged = motorDb.GetFsmBool("Damaged");
			}
			else
				_wear = FsmVariables.GetFsmFloat("Wear");
		}

		// Spare Parts compat: force an early refresh if the wear value changes
		// This is unnecessary in regular play, but because SP changes the part wear when you look between spares and regular parts,
		// We need it here
		private void Update()
		{
			if (!PartInspectorScript.IsModLoaded_SpareParts)
				return;
			var wear = _wear.Value;
			if (wear != _cachedWear)
				BuildDisplayText();
			_cachedWear = wear;
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			float effectiveWear;
			// in MSC, broken parts track whether or not they're broken using a separate variable
			// as a result, we have to override the usual wear value if they're busted
			if (PartInspectorScript.IsMSC && _mscIsDamaged.Value)
				effectiveWear = 0;
			else
				effectiveWear = _wear.Value;
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
