# Part Inspector (MWC Version)

**Note:** This is the MWC version of Part Inspector. Due to heavy code divergences, the two weren't mutually compatible. The MSC version can be found here: https://github.com/Ilysen/PartInspector

This is a mod for My Winter Car that lets you look at car parts to see how damaged they are, among other things. For more info, take a look at the [Nexus page](https://www.nexusmods.com/mysummercar/mods/2291).

Part Inspector is licensed under the [GNU General Public License v3](http://www.gnu.org/licenses/agpl.html), which can be found in full in [LICENSE.md](LICENSE.md).

## Changelog

# INDEV
#### Version 0.1 (MWC RE-RELEASE)
* Heavily refactored for My Winter Car:
	* All new items and car parts should work properly.
	* Overhauled the settings menu: 
		* "Car part condition", "Broken or intact", "Alternator belt wear", and "Spark plug wear" are all now tied to "Show car part condition".
		* "Fluid container fullness" and "Coffee and charcoal fullness" are now both tied to "Show container fullness".
		* "Verbose logging" is now split up into multiple different logging levels that can be individually toggled.
		* Each of the trackers now has some text explaining what they do.
		* Reworded basically everything.
	* Added a missing config option for quantity trackers (used for R20 batteries and fuse packages).
	* Added a new tracker type that shows an object's variant in its display name:
		* Instrument panels: Standard, clock, tachometer
		* Grilles: L/GT, LX, SLX, facelift
		* Bumper: Facelift or pre-facelift
		* Brake lines/master cylinder: Standard or power
		* Exhaust pipe front: Standard or GT
	* Changed the thresholds for standard part wear to encourage more caution:
		* Shoddy: Now 35% to 25% (previously 35% to 20%)
		* Bad: Now 25% to 15% (previously 20% to 10%)
		* Terrible: Now 15% and below (previously 10% and below)
	* Fixed a long-standing bug where where looking at objects from specific angles wouldn't consistently inspect them. Results are now always displayed whenever the object's name is visible.
