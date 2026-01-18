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
	internal class VariantTracker : BaseWearTracker
	{
		private FsmVariables _fsm;

		private string _variantKey;

		private Type _variantKeyType;

		private Dictionary<object, string> _variants;

		/// <inheritdoc/>
		internal override void Initialize(string initName, params object[] extraArgs)
		{
			base.Initialize(initName);
			_fsm = (FsmVariables)extraArgs[0];
			VariantInfo vi = (VariantInfo)extraArgs[1];
			_variantKey = vi.VariantKey;
			_variants = vi.Variants;
			_variantKeyType = vi.VariantKeyType;
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			object val = null;
			if (_variantKeyType == typeof(string))
				val = _fsm.GetFsmString(_variantKey).Value;
			else if (_variantKeyType == typeof(float))
				val = _fsm.GetFsmFloat(_variantKey).Value;
			if (val == null)
				return;
			string text = _variants[val];
			DisplayText = $"{InitialName} ({text})";
		}
	}
}
