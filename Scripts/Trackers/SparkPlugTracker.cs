using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks how worn-down a spark plug is.
	/// </summary>
	internal class SparkPlugTracker : BaseWearTracker
	{
		/// <summary>
		/// The FSM that keeps track of this spark plug's wear.
		/// This is protected and not private because alternator belt trackers inherit the logic - see <see cref="AlternatorBeltTracker"/> for more info.
		/// </summary>
		protected FsmVariables _wearFsm;

		/// <inheritdoc/>
		internal override void Initialize(string initName, params object[] extraArgs)
		{
			base.Initialize(initName);
			_wearFsm = (FsmVariables)extraArgs[0];
		}

		/// <inheritdoc/>
		internal override float GetWearPercentage() => _wearFsm.GetFsmFloat("Wear").Value;

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			string newText;
			float effectiveWear = GetWearPercentage();
			switch (Ceres.PartInspectorMWC.PartDisplayPrecision.GetSelectedItemIndex())
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
