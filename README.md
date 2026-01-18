# Part Inspector (MWC Version)

**Note:** This is the MWC version of Part Inspector. Due to heavy code divergences, the two weren't mutually compatible. The MSC version can be found here: https://github.com/Ilysen/PartInspector

This is a mod for My Winter Car that lets you look at car parts to see how damaged they are, among other things. For more info, take a look at the [Nexus page](https://www.nexusmods.com/mysummercar/mods/2291).

Part Inspector is licensed under the [GNU General Public License v3](http://www.gnu.org/licenses/agpl.html), which can be found in full in [LICENSE.md](LICENSE.md).

## Changelog

# INDEV
#### Version 0.1 (MWC RE-RELEASE)
* Heavily refactored for a My Winter Car version:
	* New items and car parts work properly.
	* Added a new tracker type that distinguishes an object's variant in its display name.
		* Instrument panels: Model type (standard, clock, tachometer)
		* Grilles: Trim (L/GT, LX, SLX, Facelift)
		* Bumper: Facelift or pre-facelift
		* Brake lines/master cylinder: Standard or power
		* Exhaust pipe front: Standard or GT
	* Fixed a long-standing bug where inspection results were not visible when looking at objects from specific angles. Results are now always displayed when the object's name is visible!
