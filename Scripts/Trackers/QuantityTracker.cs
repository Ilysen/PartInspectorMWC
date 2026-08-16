using HutongGames.PlayMaker;

namespace Ceres.PartInspector.Trackers
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

		// using Unity's update here isn't super clean, but eh. if it ain't broke
		private void Update()
		{
			var qty = _quantity.Value;
			if (qty != _cachedQty)
			{
				if (qty == 0 && _cachedQty != 0)
					DisplayText = "Empty";
				else
					BuildDisplayText();
			}
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
