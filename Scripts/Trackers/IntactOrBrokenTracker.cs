using HutongGames.PlayMaker;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Only tracks whether or not a part is intact or damaged, nothing else. Used for blocks and oilpans.
	/// </summary>
	internal class IntactOrBrokenTracker : BaseTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			DisplayText = $"{InitialName} - {((PartInspectorScript.IsMSC ? !FsmVariables.GetFsmBool("Damaged").Value : FsmVariables.GetFsmFloat("Wear").Value != 0) ? "Intact" : "Broken")}";
		}
	}
}
