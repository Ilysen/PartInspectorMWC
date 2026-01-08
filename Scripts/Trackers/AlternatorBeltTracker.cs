namespace Ceres.PartInspectorMWC.Trackers
{
	/// <summary>
	/// Tracks how worn-down a spark plug is.
	/// Conveniently for us, alternator belts and spark plugs currently use the exact same value names to track wear,
	/// so right now we just inherit the logic to avoid duplicated code, with some exceptions...
	/// </summary>
	internal class AlternatorBeltTracker : SparkPlugTracker
	{
		// unlike nearly every other part, MSC tracks integrity on alternator belts as a positive value and not a negative one;
		// i.e. it's closer to "remaining health" than it is to "total damage".
		// why it does this in differently from everything other part, I will never know
		/// <inheritdoc/>
		internal override float GetWearPercentage() => 100 - _wearFsm.GetFsmFloat("Wear").Value;
	}
}
