using HutongGames.PlayMaker;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks quantity of items remaining, for things like spark plug boxes and fuse packages.
	/// </summary>
	internal class QuantityTracker : BaseWearTracker
	{
		/// <summary>
		/// The FSM that keeps track of how many items are left in this container.
		/// </summary>
		private FsmVariables _wearFsm;

		/// <inheritdoc/>
		internal override void Initialize(string initName, params object[] extraArgs)
		{
			base.Initialize(initName);
			_wearFsm = (FsmVariables)extraArgs[0];
		}

		/// <inheritdoc/>
		internal override float GetWearPercentage() => _wearFsm.GetFsmInt("Quantity").Value;

		/// <inheritdoc/>
		internal override void BuildDisplayText() => DisplayText = $"{InitialName} - {GetWearPercentage()} left";
	}
}
