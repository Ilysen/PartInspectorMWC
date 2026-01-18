using HutongGames.PlayMaker;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks quantity of items remaining, for things like spark plug boxes and fuse packages.
	/// Nice and simple.
	/// </summary>
	internal class QuantityTracker : BaseWearTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText() => DisplayText = $"{InitialName} - {FsmVariables.GetFsmInt("Quantity").Value} left";
	}
}
