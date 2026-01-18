using HutongGames.PlayMaker;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Only tracks whether or not a part is intact or damaged, nothing else. Used for blocks and oilpans.
	/// </summary>
	internal class SimpleWearTracker : BaseWearTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			DisplayText = $"{InitialName} - {(FsmVariables.GetFsmFloat("Wear").Value != 99 ? "Broken" : "Intact")}";
		}
	}
}
