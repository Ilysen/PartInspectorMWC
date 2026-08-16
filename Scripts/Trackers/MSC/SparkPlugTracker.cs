using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks spark plug condition.
	/// </summary>
	internal class MSC_SparkPlugTracker : BaseTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			DisplayText = $"{InitialName} - {StandardWearTracker.GetDescriptor(FsmVariables.GetFsmFloat("Wear").Value)}";
		}
	}
}
