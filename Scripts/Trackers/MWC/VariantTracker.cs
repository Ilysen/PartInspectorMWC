using HutongGames.PlayMaker;
using System;
using System.Collections.Generic;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Tracks how worn-down a spark plug is.
	/// Conveniently for us, alternator belts and spark plugs currently use the exact same value names to track wear,
	/// so right now we just inherit the logic to avoid duplicated code, with some exceptions...
	/// </summary>
	internal class VariantTracker : BaseTracker
	{
		/// <summary>
		/// Used to track initialization info for variant trackers.
		/// Variants in MWC don't have a standardized way to distinguish between them;
		/// sometimes they use Type (an int), sometimes they use Model (a string), etc.
		/// This struct allows each given part type to define how its variant is determined,
		/// and the human-readable name associated with each variant type.
		/// </summary>
		internal struct VariantInfo
		{
			internal Dictionary<object, string> Variants;
			internal string VariantKey;
			internal Type VariantKeyType;
			internal bool AlsoTracksWear;

			internal VariantInfo(Dictionary<object, string> Variants, Type VariantKeyType, string VariantKey = "Type", bool AlsoTracksWear = false)
			{
				this.Variants = Variants;
				this.VariantKeyType = VariantKeyType;
				this.VariantKey = VariantKey;
				this.AlsoTracksWear = AlsoTracksWear;
			}
		}

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
				val = FsmVariables.FindFsmString(_variantKey)?.Value;
			else if (_variantKeyType == typeof(int))
				val = FsmVariables.FindFsmInt(_variantKey)?.Value;
			// this lets us use this tracker for parts that share a name without needing bespoke implementation
			// i.e. aftermarket exhaust pipes, which share a name with stock/GT exhaust pipes but *don't* have a variant value
			// this results in the tracker being added and just not doing anything. which isn't super clean but lets us avoid something bespoke
			if (val == null)
			{
				DisplayText = string.Empty;
				return;
			}
			string text = _variants[val];
			DisplayText = $"{InitialName} ({text})";
		}
	}
}
