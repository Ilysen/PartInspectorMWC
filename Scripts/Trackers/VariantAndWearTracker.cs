using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using static Ceres.PartInspectorMWC.PartInspectorScript;

namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Grotesque chimera of <see cref="StandardWearTracker"/> and <see cref="VariantTracker"/> that combines their functionality.
	/// </summary>
	internal class VariantAndWearTracker : VariantTracker
	{
		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			base.BuildDisplayText();
			DisplayText = $"{DisplayText} - {StandardWearTracker.GetDescriptor(FsmVariables.GetFsmFloat("Wear").Value)}";
		}
	}
}
