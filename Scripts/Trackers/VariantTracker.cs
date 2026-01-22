using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using static Ceres.PartInspectorMWC.PartInspectorScript;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks how worn-down a spark plug is.
	/// Conveniently for us, alternator belts and spark plugs currently use the exact same value names to track wear,
	/// so right now we just inherit the logic to avoid duplicated code, with some exceptions...
	/// </summary>
	internal class VariantTracker : BaseTracker
	{
		/// <summary>
		/// The name of the FSM variable that keeps track of this object's variant.
		/// </summary>
		private string _variantKey;

		/// <summary>
		/// The type of the FSM variable that keeps track of this object's variant.
		/// Some objects use string variables (for things like Model), and some use numbers (for things like Type).
		/// </summary>
		private Type _variantKeyType;

		/// <summary>
		/// A dictionary of all possible variant values associated to their human-readable names.
		/// </summary>
		private Dictionary<object, string> _variants;

		/// <inheritdoc/>
		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars);
			VariantInfo vi = (VariantInfo)extraArgs[0];
			_variantKey = vi.VariantKey;
			_variants = vi.Variants;
			_variantKeyType = vi.VariantKeyType;
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			object val = null;
			if (_variantKeyType == typeof(string))
				val = FsmVariables.GetFsmString(_variantKey).Value;
			else if (_variantKeyType == typeof(int))
				val = FsmVariables.GetFsmInt(_variantKey).Value;
			if (val == null)
				return;
			string text = _variants[val];
			DisplayText = $"{InitialName} ({text})";
		}
	}
}
