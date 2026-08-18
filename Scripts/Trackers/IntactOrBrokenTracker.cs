using HutongGames.PlayMaker;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Only tracks whether or not a part is intact or damaged, nothing else. Used for blocks and oilpans.
	/// </summary>
	internal class IntactOrBrokenTracker : BaseTracker
	{
		private FsmBool _mscIsDamaged;
		private FsmFloat _mwcWearVal;

		internal override void Initialize(string initName, FsmVariables fsmVars = null, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			if (PartInspectorScript.IsMSC)
				_mscIsDamaged = FsmVariables.GetFsmBool("Damaged");
			else
				_mwcWearVal = FsmVariables.GetFsmFloat("Wear");
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			DisplayText = $"{InitialName} - {((PartInspectorScript.IsMSC ? !_mscIsDamaged.Value :_mwcWearVal.Value != 0) ? "Intact" : "Broken")}";
		}
	}
}
