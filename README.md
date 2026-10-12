# Folding bed ladder

FeatureScript for Onshape that builds design **B3** of the folding camper bed stair: two folding treads in 5052 aluminum that fold flat against the cabinet side, plus a hook-on bottom step. All the numbers live in one parameters file, so the model can be resized without touching the part code.

The features build each part once, the way Onshape expects: a Part Studio holds one of every part to make, and an assembly places the copies and joins them with mates, so the fold can be simulated. Each part carries named mate connectors where it joins the next one.

Every bent part is an Onshape sheet-metal part, with the bend radius and K-factor for its thickness in the parameters, so its flat pattern is ready to export for SendCutSend (see [Flat patterns](#flat-patterns)).

## Files

| File | Feature | What it makes |
| --- | --- | --- |
| `onshape/B3_Parameters.fs` | (none) | Every dimension, in one place |
| `onshape/B3_Lib.fs` | (none) | Shared helpers and derived positions |
| `onshape/B3_Reference.fs` | B3 Reference | Floor, cabinet, bed-end wall, mattress, door handle (parts named `REF ...`), for a Part Studio of their own |
| `onshape/B3_Treads.fs` | B3 Tread | The tread (used twice) |
| `onshape/B3_Brackets.fs` | B3 Bracket | The bracket with its two bent pivot ears (used four times: two on the cabinet, two bolted inside the rail) |
| `onshape/B3_OuterRail.fs` | B3 Outer Rail | Stepped rail with flanges, the door handle notch, hand slot and the rail brackets' screw holes |
| `onshape/B3_StepClip.fs` | B3 Step Clip | Clip inside the outer rail with the outer keyhole |
| `onshape/B3_PivotHardware.fs` | B3 Pivot Hardware | Tread axle (used 4 times) and step axle (2), 12 mm tube; M6 fender washer and M6 button-head screw (12 each, one per axle end); stop pin (4); M6 x 16 flat-head screw and M6 nyloc nut (8 each, for the rail brackets) |
| `onshape/B3_FrontAngle.fs` | B3 Front Angle | Angle on the cabinet's front post |
| `onshape/B3_StepLedge.fs` | B3 Step Ledge | Ledge that holds the folded step |
| `onshape/B3_HookOnStep.fs` | B3 Hook-on Step | Step tray and its two folding legs |
| `onshape/B3_AllInOne.fs` | all of the above | Everything joined into one file, no imports needed |

## Quick start: one Feature Studio

1. In your Onshape document, click **+** at the bottom left and choose **Create Feature Studio**. Name it `B3`.
2. Select all the text in it, then paste the whole of `onshape/B3_AllInOne.fs` over it.
3. Click the green check (**Commit**) at the top right.
4. Open a Part Studio and name it `B3 Parts`. The B3 features appear at the right end of the toolbar. Add every one except **B3 Reference**; none of them asks for any input. You get 16 parts, one of each.
5. Create a second Part Studio named `Surroundings` and add **B3 Reference** to it.
6. Build the assembly as described under [Assembly](#assembly).

## Matching the repo: one Feature Studio per file

1. Create a Feature Studio for each file in `onshape/` except `B3_AllInOne.fs`, named after the file without `.fs` and with a space instead of the underscore (`B3 Parameters`, `B3 Lib`, `B3 Treads` and so on).
2. Paste each file into its Feature Studio.
3. In **B3 Lib**, delete the lines between `<B3-IMPORT>` and `</B3-IMPORT>`, click **Import** in the toolbar and pick **B3 Parameters**. Then type `export ` in front of the line it inserts, so the parameters pass through to every part.
4. In every part file, delete the same block, click **Import** and pick **B3 Lib**.
5. Commit each one, then add the features to the two Part Studios as in the quick start.

When you paste an updated file from the repo later, keep the import line Onshape made and paste the rest below it.

## Assembly

Insert the parts, then join each one by the mate connectors named below. Every part's connectors are listed with it in the assembly, and each copy sits where its mates put it when the stair is open. The surroundings are fixed; nothing else is.

The joints between the main parts are mates:

| Part | Copies | Mate | Its connector | Goes on |
| --- | --- | --- | --- | --- |
| REF floor, cabinet, bed-end wall, mattress, door handle | 1 each | Fix | | |
| Bracket 1, 2 (cabinet) | 2 | Fastened | Mounting face | REF cabinet: Bracket, top tread / Bracket, middle tread |
| Front angle | 1 | Fastened | Cabinet | REF cabinet: Front angle |
| Step ledge | 1 | Fastened | Cabinet | REF cabinet: Step ledge |
| Tread | 2 | Revolute | Cabinet axle | Bracket 1 / Bracket 2: Axle |
| Bracket 3 (rail, top tread) | 1 | Revolute | Axle, turned | Top tread: Outer axle |
| Outer rail | 1 | Fastened | Top tread bracket | Bracket 3: Mounting face |
| Bracket 4 (rail, middle tread) | 1 | Fastened | Mounting face | Outer rail: Middle tread bracket |
| Tread 2 (middle), its second pivot | | Revolute | Outer axle | Bracket 4: Axle, turned |
| Step clip | 1 | Fastened | Outer rail | Outer rail: Step clip |
| Hook-on step tray | 1 | Fastened | Cabinet-side axle | Front angle: Hook-on step |
| Hook-on step leg, cabinet side / outer | 1 each | Revolute | Axle | Tray: Cabinet-side axle / Outer axle |

The rail brackets are the same part as the cabinet brackets, turned to face the cabinet, so they use the bracket's "Axle, turned" connector. The hardware fixed to each part is placed by these connectors and then grouped with that part (Insert > Group), so it moves with it:

| Group | Hardware | Its connector | On |
| --- | --- | --- | --- |
| Bracket 1, Bracket 2 | Stop pin (2 each) | Bracket | Stop pin, back / Stop pin, front |
| | Tread axle (1 each) | Back end | Axle |
| Bracket 3, Bracket 4 | Tread axle (1 each) | Back end | Axle, turned |
| | M6 flat-head screw (4 each) | Head | Screw 1 to 4 |
| Outer rail | M6 nyloc nut (8) | Rail | Nut, top 1 to 4 / Nut, middle 1 to 4 |
| Hook-on step tray | Step axle (2) | Back end | Cabinet-side axle / Outer axle |
| With its axle | M6 fender washer (one per axle end, 12) | Axle | The axle: Back end / Front end |
| | M6 button-head screw (12) | Washer | Its washer: Screw |

To fold the stair, drag the top tread up toward the cabinet: the treads, the rail brackets and the rail move as a parallelogram on the four revolute mates. Lift the hook-on step off first, as on the stair itself; in the assembly it stays on the front angle, so suppress or hide it to see the folded stair clearly. The legs fold on their own revolute mates.

`tools/onshape_assembly.py` builds this assembly through the API (see below).

## Flat patterns

The bent parts are Onshape sheet-metal parts: the tread, the hook-on step tray and the step clip, front angle and step ledge on 1/8" sheet; the bracket, the outer rail and the two step legs on 3/16". Each is modeled to the outside of its bends, then converted with the thickness going inward, so the outside sizes match the B3 page.

- **Bends** use SendCutSend's bending specs for 5052: `BEND_R_SHEET` 0.125" with `K_SHEET` 0.44 on .125" sheet, `BEND_R_PLATE` 0.250" with `K_PLATE` 0.43 on .187". They don't do custom radii, so the tread and step lips bend at 0.125" too. Their bend deductions (0.216" and 0.356") match.
- **Reliefs** follow SendCutSend's rule: at least half the thickness wide (these are a full thickness) and reaching the bend radius plus the thickness plus 0.020" past the bend line on the flat pattern (0.270" on .125", 0.457" on .187"). Past a relief, the bare edge sits 1/16" inside the bend line, so no material lies on the bend line where the flange stops. There are three places: where the rail's long flange stops short of the handle notch, below each step leg's ear, and where the front angle's panel leg stops, which is a step in the outward leg. Where two rail flanges meet at the foot, Onshape cuts a round corner relief of the same radius. Every other bend runs to an edge at both ends, so it needs none: the tread and step lips, the bracket ears, the step leg ears at the top, the step clip, the ledge, and the rail's bed-side and front flanges.
- **Cut features** sit at least half SendCutSend's die width from a bend line (0.315" on .125", 0.492" on .187"), except the bushing holes (see Limitations).
- **Export:** open the flat view with the sheet-metal table button in the Part Studio, or right-click the part in the parts list and export its flat pattern as DXF for SendCutSend.

Estimated flat sizes: tread and step tray about 18 x 9 3/16, bracket about 7 3/16 x 3 7/16, outer rail about 13 11/16 x 29 1/16, all within SendCutSend's 44 x 30.

## Checking the code

`tools/fs_lint.py` checks the files offline: brackets balance, `B3_AllInOne.fs` is up to date, and, given a copy of the FeatureScript standard library at the version the files import, every function, constant and enum value they use exists.

```
git clone --filter=blob:none https://github.com/javawizard/onshape-std-library-mirror std
git -C std checkout "$(git -C std log --format=%h --grep='^Version 2641.0$')"
python3 tools/fs_lint.py std
```

`tools/onshape_check.py` runs the lint, then loads `B3_AllInOne.fs` into a test Onshape document through the API, adds the features to two Part Studios (`B3 test Part Studio` and `B3 test Surroundings`) and reports what rebuilds, each part's size, and their mate connectors. `tools/onshape_assembly.py` then builds the assembly from them: it inserts every copy in one call, placed where its mates will hold it, then adds the groups and the mates. Both need `ONSHAPE_ACCESS_KEY`, `ONSHAPE_SECRET_KEY` and `ONSHAPE_DOC_URL` (a workspace URL of a document used only for testing), and touch only the `B3 test` tabs they create there.

API calls are limited, so the scripts keep the ids of what they made in a state file and reuse them. The first check costs about 16 calls, each rerun 4, and `--report` 2. The assembly costs about 25 calls (one per group and per mate), once: it follows the Part Studios by itself, so after changing the FeatureScript only the check needs rerunning. When the parts are replaced rather than changed (their ids change, as when they became sheet metal), `--rebuild` starts a new assembly; if the API key can't delete tabs, the new one gets a numbered name and the old tab is left to delete by hand. `python3 tools/onshape_assembly.py --dry-run` prints the plan without calling the API.

## Changing dimensions

Edit `B3 Parameters` (or the parameters section at the top of the all-in-one file) and commit. The Part Studio rebuilds to match, flat patterns included. The names and values follow the "Part definitions" section of the Bed Stair B3 page, and each is commented with what it measures.

To check clearances through the fold, use the assembly: drag the tread, or animate the top tread's revolute mate, and measure.

## Coordinates

Same datum as the B3 page. The origin is on the floor, at the side panel's face, at the stair's bed end. Each part is built once, where its first copy sits when the stair is open (the top tread's bracket, axle and so on).

- **x** along the cabinet toward its front edge; the stair spans x = 0 to 12 (Onshape's X)
- **y** out from the panel face into the room (Onshape's **-Y**)
- **z** up from the floor (Onshape's Z)

The page's y runs along Onshape's -Y so that the model isn't mirrored: facing the bed-end wall, the cabinet is on the right, the stair to its left and the door handle left of the stair. The parameters and the code use the page's coordinates throughout; only the sketch-plane and solid helpers in `B3_Lib.fs` flip y. Onshape's Front view looks at the stair from the room, with the bed-end wall on the left.

## Limitations

- **Not modeled:** countersinks, which are picked in SendCutSend's configurator when ordering (the bracket's screw holes and the step ledge's are plain holes, so in the assembly each flat-head screw's head overlaps the rail bracket's web), the rail's latch holes and foot pad, the front angle's keeper holes, bushings, thrust and wave washers, and rubber parts. Each axle screw therefore sits about 1.2 mm off its lip, where the bushing flange goes.
- **Bushing holes** are modeled at their finished 14 mm. The page cuts them as 3/8" pilots, then reams them after bending, because most of them sit closer to a bend than SendCutSend's die-width rule allows; a flat pattern exported from the model has the finished size.
- **Where the model differs from the page** to meet SendCutSend's rules: the rail's bed-side and front flanges run the full length of their edges instead of stopping 1/2" short of the corners; the step leg ears' bends run up to the web's top edge, and the legs are 4 5/8 wide below the ears; the bracket's hook notch has a 1/32" radius instead of 1/8"; the step clip's outer rivet hole is at x 11 1/8 instead of 11 1/4; the step ledge's screws are at z 1/2 instead of 5/8.
