using HutongGames.PlayMaker;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Grotesque chimera of <see cref="PartConditionTracker"/> and <see cref="VariantTracker"/> that combines their functionality.
	/// </summary>
	internal class VariantAndWearTracker : VariantTracker
	{
		private FsmFloat _wear;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			_wear = FsmVariables.GetFsmFloat("Wear");
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			base.BuildDisplayText();
			DisplayText = $"{DisplayText} - {PartConditionTracker.GetDescriptor(_wear.Value)}";
		}
	}
}
