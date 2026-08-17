using HutongGames.PlayMaker;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Base wear tracker class with shared logic.
	/// </summary>
	internal abstract class BaseTracker : MonoBehaviour
	{
		/// <summary>
		/// Any occurrence of these strings in the display name will be removed before it is displayed.
		/// </summary>
		internal static readonly string[] DISPLAY_NAME_TAGS_TO_TRIM = { "(Clone)", "(itemx)", "(VINXX)", "(VINX0)", "(spare)" };

		/// <summary>
		/// The human-readable name for the part this component is attached to.
		/// </summary>
		public string InitialName;

		/// <summary>
		/// The text displayed on-screen when the player hovers over an object with this component.
		/// </summary>
		public string DisplayText;

		/// <summary>
		/// Every tracker type has to reference FSMs of some kind, even if the specifics vary.
		/// </summary>
		public FsmVariables FsmVariables;

		/// <summary>
		/// Initializes this wear tracker with the provided arguments.
		/// A name is required, but after that, any number of arguments can be passed. Subtypes can use this for special logic.
		/// </summary>
		internal virtual void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			foreach (var tag in DISPLAY_NAME_TAGS_TO_TRIM)
				initName = initName.Replace(tag, "");
			InitialName = initName;
			FsmVariables = fsmVars;
		}

		/// <summary>
		/// Returns the wear percentage for this part.
		/// MSC doesn't track this in a standardized way, so each different type of tracker needs its own logic to get the appropriate value.
		/// This should be overridden on all subtypes, but is virtual and not abstract because some types don't need to worry about it.
		/// </summary>
		internal virtual float GetWearPercentage() => 0;

		/// <summary>
		/// Updates the <see cref="DisplayText"/> of this wear tracker. Subtypes must each override this function.
		/// </summary>
		internal abstract void BuildDisplayText();
	}
}
