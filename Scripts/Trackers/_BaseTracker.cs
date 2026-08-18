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
		internal virtual void Initialize(string initName, FsmVariables fsmVars = null, params object[] extraArgs)
		{
			foreach (var tag in DISPLAY_NAME_TAGS_TO_TRIM)
				initName = initName.Replace(tag, "");
			InitialName = initName;
			FsmVariables = fsmVars;
		}

		/// <summary>
		/// Many tracker subtypes find it convenient to have an easy helper accessible to get a percentage representing how full/damaged/whatever they are.
		/// This is a generic overrideable function that lets them do that easily.
		/// </summary>
		internal virtual float GetPercentage() => 0;

		/// <summary>
		/// Updates the <see cref="DisplayText"/> of this wear tracker. Subtypes must each override this function.
		/// </summary>
		internal abstract void BuildDisplayText();
	}
}
