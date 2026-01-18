# Part Inspector (MWC Version)

**Note:** This is the MWC version of Part Inspector. Due to heavy code divergences, the two weren't mutually compatible. The MSC version can be found here: https://github.com/Ilysen/PartInspector

This is a mod for My Winter Car that lets you look at car parts to see how damaged they are, among other things. For more info, take a look at the [Nexus page](https://www.nexusmods.com/mysummercar/mods/2291).

Part Inspector is licensed under the [GNU General Public License v3](http://www.gnu.org/licenses/agpl.html), which can be found in full in [LICENSE.md](LICENSE.md).

## Changelog

### Jan. 17, 2025
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

### Feb. 11, 2025
#### Version 1.3
* Now supports ground coffee, grill charcoal, fuse packages, R20 battery boxes, spark plug boxes, mosquito spray, spray cans, and fire extinguishers.
* Split up the "Display precision" setting into two options: one for parts (also includes oil filters and spark plugs), and one for items. Both settings now default to general descriptions.

### Jan. 12, 2025
#### Version 1.2.2
* Updated mod ID from `PartInspector` to `Ceres_PartInspector`.
* Touch up code quality and improve documentation.

### Dec. 24, 2025
#### Version 1.2.1
* Publicly released some unused code enable inspection of fluid containers.
* Fixed an issue making the mod incompatible with newer versions of MSCLoader.
* Built and tested on MSCLoader 1.3.

### Jun. 22, 2023
#### Version 1.1
* Now supports spark plugs and alternator belts.
* Relicensed to GPL v3. Code prior to commit `b94e1ccb8bf933c216384269b30703dc78a32342` remains licensed under the MIT License.
* Built and tested on MSCLoader 1.2.12, build 291.

### Sep. 6, 2022
#### Version 1.0
* Initial public release.
* All damageable parts can show their wear as a number, a general description, or just if they're broken or not.
* Oil filters display their dirtiness!
* Built and tested on MSCLoader 1.2.7.
