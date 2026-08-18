using HutongGames.PlayMaker;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Tracks spark plug condition.
	/// </summary>
	internal class MSC_SparkPlugTracker : BaseTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			// this should really be cached, but i cba. spark plugs get to be evil as a treat
			DisplayText = $"{InitialName} - {PartConditionTracker.GetDescriptor(FsmVariables.GetFsmFloat("Wear").Value)}";
		}
	}
}
