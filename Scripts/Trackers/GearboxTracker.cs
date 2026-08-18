using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	internal class GearboxTracker : PartConditionTracker
	{
		private FsmFloat _mscFinalGearRatio;

		internal override string GetPartName => $"{base.GetPartName} (Ratio {(PartInspectorScript.IsMSC ? _mscFinalGearRatio.Value.ToString() : "TODO")})";

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			if (PartInspectorScript.IsMSC)
				_mscFinalGearRatio = PlayMakerExtensions.GetPlayMaker(GameObject.Find("DatabaseMechanics/Gears"), "Data").FsmVariables.FindFsmFloat("FinalGear");
		}
	}
}
