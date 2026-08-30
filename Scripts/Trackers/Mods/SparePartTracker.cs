using HutongGames.PlayMaker;
using MSCLoader;
using Spare_Parts;
using UnityEngine;

namespace Ceres.PartInspector.Trackers
{
	/// <summary>
	/// Tracks the exact integrity of the assigned part using the provided information. Broken parts will display as broken.
	/// </summary>
	internal class MOD_SparePartTracker : BaseTracker
	{
		// noting this down here for myself since I couldn't find good resources online:
		//
		// type declarations CANNOT be used from other assemblies unless they're mandatory dependencies,
		// because they'll throw errors for anyone who doesn't have the mod installed.
		// to avoid this, we just store these as objects, and then typecast when we use them.
		//
		// this is because the logic itself will only fire during runtime, and this tracker type is only initialized
		// when Spare Parts is loaded.
		//
		// end result: it's safe to use references in runtime, but *not* in compile time
		// (xml docs are, of course, fine)

		/// <summary>
		/// Before use, typecast to <c><see cref="SparePart"/></c>.
		/// </summary>
		private object _partComp;

		/// <summary>
		/// Before use, typecast to <c><see cref="SpareGearbox"/></c>.
		/// </summary>
		private object _gearbox;

		/// <summary>
		/// Getter function for this object's <c><see cref="SparePart"/></c> component.
		/// Will automatically fetch if null, and throw an error if none is on the object.
		/// </summary>
		private object PART_COMP
		{
			get
			{
				if (_partComp != null)
					return _partComp;
				if (_partComp == null)
					_partComp = gameObject.GetComponent<SparePart>();
				if (_partComp == null)
				{
					ModConsole.Error($"Spare part tracker couldn't find a SparePart component on object named \"{gameObject.name}\"!!");
					enabled = false;
					return null;
				}
				else
					return _partComp;
			}
		}

		internal override void Initialize(string initName, FsmVariables fsmVars, params object[] extraArgs)
		{
			base.Initialize(initName, fsmVars, extraArgs);
			if (!PartInspectorScript.IsModLoaded_SpareParts)
			{
				ModConsole.Error("Attempted to create a spare part tracker when Spare Parts was not loaded!");
				enabled = false;
				return;
			}
			_gearbox = gameObject.GetComponent<SpareGearbox>() ?? null;
			// Not bothering to set PART_COMP here -- it's auto-assigned
		}

		/// <inheritdoc/>
		internal override void BuildDisplayText()
		{
			float effectiveWear;
			SparePart partComp = (SparePart)PART_COMP;
			SpareGearbox gearbox = (SpareGearbox)_gearbox;
			if (partComp.damaged)
				effectiveWear = 0;
			else
				effectiveWear = partComp.wear;
			DisplayText = $"{InitialName}{(gearbox != null ? $" (Ratio {gearbox.finalDriveRatio})" : "")} - {GetDescriptor(effectiveWear)}";
		}

		internal static string GetDescriptor(float WearVal)
		{
			string newText;
			if (WearVal <= 0) // Always display broken parts as just "broken"
				newText = "Broken";
			else
			{
				switch (PartInspectorScript.SettingDisplayPrecision.GetSelectedItemIndex())
				{
					case 1: // General description
						string descriptor;
						if (WearVal >= 90)
							descriptor = "mint";
						else if (WearVal >= 80)
							descriptor = "great";
						else if (WearVal >= 65)
							descriptor = "good";
						else if (WearVal >= 50)
							descriptor = "decent";
						else if (WearVal >= 35)
							descriptor = "shoddy";
						else if (WearVal >= 25)
							descriptor = "poor";
						else if (WearVal >= 15)
							descriptor = "bad";
						else
							descriptor = "terrible";
						newText = $"In {descriptor} condition";
						break;
					case 2: // Broken/not broken
						newText = "Intact";
						break;
					default: // Exact percentage
						newText = Mathf.RoundToInt(WearVal) + "% condition";
						break;
				}
			}
			return newText;
		}
	}
}
