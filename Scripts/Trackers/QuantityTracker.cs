using HutongGames.PlayMaker;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks quantity of items remaining, for things like spark plug boxes and fuse packages.
	/// Nice and simple.
	/// </summary>
	internal class QuantityTracker : BaseTracker
	{
		private FsmInt _quantity;
		private int _cachedQty;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			_quantity = fsmVars.GetFsmInt("Quantity");
		}

		private void Update()
		{
			var qty = _quantity.Value;
			if (qty != _cachedQty)
				BuildDisplayText();
			_cachedQty = qty;
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			var qty = _quantity.Value;
			DisplayText = qty == 0 ? string.Empty : $"{InitialName} - {_quantity.Value} left";
		}
	}
}
