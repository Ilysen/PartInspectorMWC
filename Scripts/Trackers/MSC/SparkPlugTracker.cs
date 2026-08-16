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
			DisplayText = $"{InitialName} - {PartConditionTracker.GetDescriptor(FsmVariables.GetFsmFloat("Wear").Value)}";
		}
	}
}
