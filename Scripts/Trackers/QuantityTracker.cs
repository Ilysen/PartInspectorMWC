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

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			var qty = _quantity.Value;
			// we do this for a specific reason!
			// the game changes the object's name to "Empty" when it runs out of stuff, and if we just use the string.Empty part right away,
			// the "1 left" thing will get stuck on the screen. doing this here means that the name will appear to change correctly,
			// after which point we immediately use string.Empty as normal, creating the illusion that it adapted to the new name seamlessly
			if (qty == 0 && _cachedQty != 0)
				DisplayText = "Empty";
			else
				DisplayText = qty == 0 ? string.Empty : $"{InitialName} - {_quantity.Value} left";
			_cachedQty = qty;
		}
	}
}
