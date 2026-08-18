using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	internal class GearboxTracker : PartConditionTracker
	{
		private FsmFloat _mscFinalGearRatio;

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			if (PartInspectorScript.IsMSC)
				_mscFinalGearRatio = PlayMakerExtensions.GetPlayMaker(GameObject.Find("DatabaseMechanics/Gears"), "Data").FsmVariables.FindFsmFloat("FinalGear");
		}

		internal override void BuildDisplayText()
		{
			base.BuildDisplayText();
			if (PartInspectorScript.IsMSC)
				DisplayText = DisplayText.Replace(InitialName, $"{InitialName} (Ratio {_mscFinalGearRatio.Value})");
		}
	}
}
