# Part Inspector

This is a mod for My Summer Car and My Winter Car that lets you look at car parts to see how damaged they are, among other things. For more info, take a look at the [Nexus page](https://www.nexusmods.com/mysummercar/mods/2291).

Part Inspector is licensed under the [GNU General Public License v3](http://www.gnu.org/licenses/agpl.html), which can be found in full in [LICENSE.md](LICENSE.md).

## Changelog

### 25 August, 2026
#### Version 2.0.1
* Emergency hotfix to disable Spare Parts integration until I can find out how to fix a crash bug when loading the mod without it. Oops.

#### Version 2.0
* Unified the MSC and MWC versions of the mod. The same file should now work on both games, and adjust accordingly to match!
	* If you're playing MWC, then there won't be many differences here. If you're playing MSC, however, there are a lot of changes and new features, so make sure you go through your settings thoroughly. You should find the mod to be overall much more stable and responsive than before!
	* I usually use zerover, but because the MSC version was at 1.X, I'm gonna make an exception to my rule and just do 2.0 with this one.
* Implemented several new information displays related to part tuning, each of which can be toggled independently and will display when looking at that part's adjustment screw/bolt/whatever with the correct tool:
	* **Rocker valve:** Current valve lash. (This was previously an experimental feature, but is now generally available.)
	* **Rally suspension:** Current bump and rebound. This can already be tracked via ticks from max/min, but this makes it easier to set and remember specific values.
	* **Wheel alignment:** What it says on the tin. Because it's currently a little unstable, this is in the Experimental tab rather than the Tuning tab.
* Added a new tracker type, disabled by default, that shows the current freshness of food that can spoil.
* When the mod fails to set up a new tracker, it will try to handle the failure more gracefully and stop trying to add trackers to that object instead of repeatedly failing until the mod disables itself due to error spam. This should allow you to continue playing even if you run into issues, though mileage may vary.
* **MSC only:** Tentative compatibility with the Spare Parts mod.
* **MSC only:** Gearboxes now display their final drive ratio. This will come to MWC at some point in the future, hopefully.

### 1 August, 2026
#### Version 0.1.6
* Fixed an issue where two-stroke fuel wouldn't have its fullness tracked correctly.
* Rewrote the single-sentence mod description to hopefully have more \~\*Soul\*\~. This is accompanied by some very minor updates to the Nexus page too.

### 6 May, 2026
#### Version 0.1.5
* Updated for compatibility with build 260504-01. Possible known issue: some newly-updated parts might not show their wear values if they're at maximum condition (i.e. zero wear). If you run into these, please report them!

### 29 January, 2026
#### Version 0.1.4
* Brake master cylinders now show both variant and wear, instead of just their variant. For now, it only shows data if both options are enabled.
* Added an experimental feature to display the clearance of rocker valves while looking at them, for precisely tuning valve lash without requiring a save editor. **This is unfinished, untested, and may not even stick around;** support will not be provided, but it's still listed here for posterity.

### 26 January, 2026
#### Version 0.1.3
* Corrected variant names for bootlids.

### 23 January, 2026
#### Version 0.1.2
* Added quantity tracking for part packages purchased from Fleetari's shop.
* Re-enables variant identification for front exhaust pipes.
* Fixed an issue where parts at exactly 99 condition wouldn't give any information when looked at.

### 22 January, 2026
#### Version 0.1.1
* Hotfix for error spam when looking at the GT variant of the front exhaust pipe part. For now, it's just disabled until I can do a more proper implementation.

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
		* *Note:* Things like door trims and seat colors aren't included, since that's information you can immediately intuit just by looking at them (unlike the other affected objects, which require prior knowledge to correctly identify!)
	* Added a QoL setting, disabled by default, that shows the size of a bolt you're looking at while in tool mode. Mainly meant so you don't have to install another mod for it.
	* Changed the thresholds for standard part wear to better represent when using a part becomes a bad idea:
		* Shoddy: Now 35% to 25% (previously 35% to 20%)
		* Bad: Now 25% to 15% (previously 20% to 10%)
		* Terrible: Now 15% and below (previously 10% and below)
	* Fullness and quantity trackers will now skip the refresh interval while their values are actively changing. In layman's terms, their readout will visually update in real-time as you use them.
	* Fixed a long-standing bug where where looking at objects from specific angles wouldn't consistently inspect them. Results are now always displayed whenever the object's name is visible.
