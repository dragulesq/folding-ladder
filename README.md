# Folding bed ladder

FeatureScript for Onshape that builds design **B3** of the folding camper bed stair: two folding treads in 5052 aluminum that fold flat against the cabinet side, plus a hook-on bottom step made of a third tread on two folding legs. All the numbers live in one parameters file, so the model can be resized without touching the part code.

The features build each part once, the way Onshape expects: a Part Studio holds one of every part to make, and an assembly places the copies and joins them with mates, so the fold can be simulated. Each part carries named mate connectors where it joins the next one.

Every bent part is an Onshape sheet-metal part, with the bend radius and K-factor for its thickness in the parameters, so its flat pattern is ready to export for SendCutSend (see [Flat patterns](#flat-patterns)).

## Files

| File | Feature | What it makes |
| --- | --- | --- |
| `onshape/B3_Parameters.fs` | (none) | Every dimension, in one place |
| `onshape/B3_Lib.fs` | (none) | Shared helpers and derived positions |
| `onshape/B3_Reference.fs` | B3 Reference | Floor, cabinet, bed-end wall, mattress, door handle, and the room for mock-up: the camper's side wall, the dinette seat across the entrance aisle and the shower wall across the middle aisle (parts named `REF ...`), for a Part Studio of their own |
| `onshape/B3_Treads.fs` | B3 Tread | The tread (used three times: the top and middle treads and the hook-on step's tray; the two 4.5 mm holes in its front lip are for the step's leg locks) |
| `onshape/B3_Brackets.fs` | B3 Bracket | The bracket with its two bent pivot ears (used four times: two on the cabinet, two bolted inside the rail) |
| `onshape/B3_OuterRail.fs` | B3 Outer Rail | Stepped rail with flanges, a straight lower edge (the 17" treads keep it inside the door handle, so it has no notch), hand slot and the rail brackets' and step clip's screw holes |
| `onshape/B3_StepClip.fs` | B3 Step Clip | The keyhole clip for the hook-on step's pins, one part used twice: on the cabinet's front post and, upside down, inside the outer rail |
| `onshape/B3_PivotHardware.fs` | B3 Pivot Hardware | Tread axle (used 4 times) and step axle (2), 12 mm tube; M6 fender washer (12, one per axle end); M6 button-head screw (16: one per axle end, two for each step clip); stop pin (4); M6 x 20 flat-head screw (6, for the rail brackets; 6 longer ones hold the cabinet brackets); M6 nyloc nut (8, for the rail brackets and the rail's step clip) |
| `onshape/B3_BushingsPads.fs` | B3 Bushings and Pads | The bought-in soft parts, one of each: lip bushing (16 used), ear bushing (8), PTFE thrust washer (12), wave washer (6), neoprene washer (2), rail foot pad (2), step leg pad (2), ledge strip (1), panel bumper (3: two on the panel, one inside the rail's foot) |
| `onshape/B3_StepLedge.fs` | B3 Step Ledge | The step ledge and lower stow hook in one sheet-metal part with two bends: a back wall on the panel (three M6 button heads, behind the stowed step), the shelf the folded step stands on bent out along its bottom edge, and the hook plate for the step's lower pin bent out at its front end. Nothing of it is below the shelf or in front of the hook plate |
| `onshape/B3_HookOnStep.fs` | B3 Hook-on Step Leg | The hook-on step's folding leg (one part used twice, the outer one turned to face the cabinet; each ear has the two 14 mm pivot holes and, between them and 0.3" toward the web, a hole tapped M6 for the leg's lock) and the locks' M6 thin jam nut |
| `onshape/B3_StowLocks.fs` | B3 Stow Locks | The stow bracket (travel lock shelf and upper hook in one sheet-metal part with two bends) and the plunger's jam nut |
| `onshape/B3_AllInOne.fs` | all of the above | Everything joined into one file, no imports needed |

## Quick start: one Feature Studio

1. In your Onshape document, click **+** at the bottom left and choose **Create Feature Studio**. Name it `B3`.
2. Select all the text in it, then paste the whole of `onshape/B3_AllInOne.fs` over it.
3. Click the green check (**Commit**) at the top right.
4. Open a Part Studio and name it `B3 Parts`. The B3 features appear at the right end of the toolbar. Add every one except **B3 Reference**; none of them asks for any input. You get 14 parts, one of each.
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
| REF floor, cabinet, bed-end wall, mattress, door handle, side wall, dinette seat, shower wall | 1 each | Fix | | |
| Bracket 1, 2 (cabinet) | 2 | Fastened | Mounting face | REF cabinet: Bracket, top tread / Bracket, middle tread |
| Step clip 1 (cabinet) | 1 | Fastened | Mount | REF cabinet: Step clip |
| Step ledge | 1 | Fastened | Cabinet | REF cabinet: Step ledge |
| Tread 1, 2 (top, middle) | 2 | Revolute | Cabinet axle | Bracket 1 / Bracket 2: Axle |
| Bracket 3 (rail, top tread) | 1 | Revolute | Axle, turned | Top tread: Outer axle |
| Outer rail | 1 | Fastened | Top tread bracket | Bracket 3: Mounting face |
| Bracket 4 (rail, middle tread) | 1 | Fastened | Mounting face | Outer rail: Middle tread bracket |
| Tread 2 (middle), its second pivot | | Revolute | Outer axle | Bracket 4: Axle, turned |
| Step clip 2 (rail, upside down) | 1 | Fastened | Mount | Outer rail: Step clip |
| Tread 3 (the hook-on step's tray) | 1 | Fastened | Cabinet axle | Step clip 1: Hook-on step |
| Hook-on step leg 1 (cabinet side) | 1 | Revolute | Axle | Tread 3: Cabinet axle |
| Hook-on step leg 2 (outer) | 1 | Revolute | Axle, turned | Tread 3: Outer axle |

The rail brackets are the same part as the cabinet brackets, turned to face the cabinet, so they use the bracket's "Axle, turned" connector. The outer step leg is the same part as the cabinet-side one, turned the same way, so it uses the leg's "Axle, turned" connector, which is on its lower hole. The hardware fixed to each part is placed by these connectors and then grouped with that part (Insert > Group), so it moves with it:

| Group | Hardware | Its connector | On |
| --- | --- | --- | --- |
| Bracket 1, Bracket 2 | Stop pin (2 each) | Bracket | Stop pin, back / Stop pin, front |
| | Tread axle (1 each) | Back end | Axle |
| Bracket 3, Bracket 4 | Tread axle (1 each) | Back end | Axle, turned |
| | M6 flat-head screw (3 each) | Head | Screw 1 to 3 |
| Outer rail | M6 nyloc nut (8) | Rail | Nut, top 1 to 3 / Nut, middle 1 to 3 / Nut, clip 1 and 2 |
| Step clip 1 and 2 | M6 button-head screw (2 each) | Washer | Screw 1 and 2 (the front pair, on the cabinet) / Screw 3 and 4 (the rear pair, on the rail) |
| Tread 3 | Step axle (2) | Tread | Cabinet axle / Outer axle |
| Tread 1, 2, 3 | Lip bushing (4 each) | Axle | Cabinet axle / Cabinet axle, turned / Outer axle / Outer axle, turned |
| | Thrust washer (4 each) | Axle | the same four |
| Tread 1, 2, 3 | Wave washer (2 each, at the back) | Axle | Cabinet axle / Outer axle |
| Tread 3 | Neoprene washer (2) | Tread | Cabinet axle / Outer axle |
| Bracket 1 to 4 | Ear bushing (2 each) | Axle | Axle / Axle, turned |
| Hook-on step leg 1, 2 | Lip bushing (2 each) | Leg ear | Axle / Axle, front (Lower axle / Axle, turned on the outer leg) |
| Hook-on step leg 1, 2 | M6 jam nut (1 each) | Ear | Lock (Lock, turned on the outer leg) |
| | Step leg pad (1 each) | Leg | Foot pad |
| Outer rail | Rail foot pad | Rail | Foot pad |
| Step ledge | Ledge strip | Ledge | Rubber strip |
| REF cabinet | Panel bumper (2) | Panel | Bumper, top tread / Bumper, middle tread |
| With its axle | M6 fender washer (one per axle end, 12) | Axle | The axle: Back end / Front end |
| | M6 button-head screw (12) | Washer | Its washer: Screw |

To fold the stair, drag the top tread up toward the cabinet: the treads, the rail brackets and the rail move as a parallelogram on the four revolute mates. Lift the hook-on step off first, as on the stair itself; in the assembly it stays on the cabinet's step clip, so suppress or hide it to see the folded stair clearly. The legs fold on their own revolute mates, the cabinet-side leg first.

`tools/onshape_assembly.py` builds this assembly through the API (see below); its own `HARDWARE` table is the exact list of which connector goes on which. Where the document holds McMaster's own models (imported STEP or Parasolid files), the assembly uses them in place of the modeled hardware: the flat heads (92125A240 on the rail brackets, 92125A242 into the 8020), the button heads (92095A226 on the axle ends, 92095A238 on the step clip), the fender washers (94316A560), the nyloc nuts (93625A250) and the wave washers (1775N12). The script's `SWAPS` table names which, and the state file's `imports` holds each import's Part Studio and the frame in its own coordinates that matches the modeled part's mate connector. McMaster's models import as composite parts, which an insert skips unless it asks for them. The igus bushings (JFM-1214-04 in the lips and step leg ears, GFM-1214-06 in the bracket ears) swap in the same way. The hook-on step's legs lock open with a McMaster 8501A54 twist-lock spring plunger each (M6 x 1, 4 mm nose, 10 mm travel; McMaster's CAD file for it is named 8501A65): `ADDS` places one on the cabinet-side leg's "Lock" connector and one on the outer leg's "Lock, turned". It threads into the leg's front ear at a tapped hole halfway between the ear's two pivot holes and 0.3" toward the web, with an M6 thin jam nut inside the ear; open, the nose springs through a 4.5 mm hole in the tray's front lip and stands 3.6 mm proud. Folded, both noses ride on the inside of the lip and snap in by themselves when the leg opens. So that SendCutSend can tap the hole (0.246" from its center to any edge, 0.517" to a bend line), the leg's web stands 15/16" from the pivots, in the panel's plane just past the cabinet's front, and the ears are 1 3/8" wide. They cannot grow the other way: folded, the cabinet-side leg's ear tips stop 1/16" under the tray's skin. The stowed step's back therefore stands 1/32" off the panel. The travel lock, a McMaster 8501A58 twist-lock spring plunger, has no modeled stand-in: `ADDS` places it on the cabinet's "Plunger" connector, pointing up through the shelf of the "Stow bracket" (1/8" sheet metal on the panel, 17" up: the shelf is its base, the panel leg is bent down along the shelf's panel edge and the hook plate is bent up along its bed-end edge), clamped by the M10 thin "Plunger jam nut" on the shelf and a regular nut under it, handle hanging under the shelf. Folded, the rail's foot flange lands 3/16" over the nut and the nose comes up through a hole at mid-foot, between the two rubber foot pads. The folded rail's first move on the way open is straight out from the panel, square to the pin, so the pin locks the fold (a pin lying along that move, as in two earlier tries, only slides out of its hole). The same Stow bracket's hook plate has a slot, open to the room, that takes the stowed hook-on step's upper pin; the hook plate at the front end of the "Step ledge" takes its lower pin in an open-top slot. Stow the step first: with the stair open, carry it in on end through the front, under the middle tread (3/4" to spare upright, 1 1/2" leaning 25 degrees), set its bottom end on the ledge and tip its top in to the panel. Then fold the stair: the rail's foot is 7" long, so its back inch stands in front of the step's top, and a rubber bumper stuck inside the foot presses on the step's face, so the one plunger locks the step too. Nothing sticks out past the entry cabinet. In `B3 test Stowed` the hook-on step and its hardware are one group on a revolute mate at its lower pin in the step ledge's hook plate, so it can be tipped in and out of the upper hook; everything else is fixed. `--stowed` builds a second assembly, `B3 test Stowed`, with every part fixed where it sits folded: the treads up against the panel, the rail lifted with them, and the hook-on step standing on its ledge, cabinet-side end down and tread face to the room.

## Flat patterns

The bent parts are Onshape sheet-metal parts: the tread, the step leg, the step clip, the step ledge, the stow bracket and the lower stow hook on 1/8" sheet; the bracket and the outer rail on 3/16". The rail's sheet is set by `RAIL_T` and the three constants beside it in `B3_Parameters.fs`; 3/16" is for stiffness (1/8" is about 30% as stiff as a plate, and steel of equal stiffness is 3.3 mm and twice the weight). Each is modeled to the outside of its bends, then converted with the thickness going inward, so the outside sizes match the B3 page.

- **Bends** use SendCutSend's bending specs for 5052: `BEND_R_SHEET` 0.125" with `K_SHEET` 0.44 on .125" sheet, `BEND_R_PLATE` 0.250" with `K_PLATE` 0.43 on .187". They don't do custom radii, so the tread lips bend at 0.125" too. Their bend deductions (0.216" and 0.356") match.
- **Reliefs** follow SendCutSend's rule: at least half the thickness wide (these are a full thickness) and reaching the bend radius plus the thickness plus 0.020" past the bend line on the flat pattern (0.270" on .125", 0.457" on .187"). Past a relief, the bare edge sits 1/16" inside the bend line, so no material lies on the bend line where the flange stops. There are two places: below the rail's riser flange, where the middle step carries on past it, and at both ends of the step clip's bend, where the taller mounting leg carries on past the keyhole leg. On the stow bracket, a notch in the shelf's corner keeps its two bends apart instead. Where two flanges meet (the rail's at the foot, and each step leg's side flanges and foot), Onshape cuts a round corner relief of the same radius. Every other bend runs to an edge at both ends, so it needs none: the tread lips, the bracket ears, the step leg's side flanges at the top, the step clip, the ledge, and the rail's bed-side and front flanges.
- **Cut features** sit at least half SendCutSend's die width from a bend line (0.315" on .125", 0.492" on .187"), except the bushing holes (see Limitations).
- **Export:** open the flat view with the sheet-metal table button in the Part Studio, or right-click the part in the parts list and export its flat pattern as DXF for SendCutSend.

Estimated flat sizes: tread about 17 x 9 3/16, bracket about 7 3/16 x 3 7/16, outer rail about 13 11/16 x 29 1/16, step leg about 6 13/16 x 10 7/16, all within SendCutSend's 44 x 30.

## Checking the code

`tools/fs_lint.py` checks the files offline: brackets balance, `B3_AllInOne.fs` is up to date, and, given a copy of the FeatureScript standard library at the version the files import, every function, constant and enum value they use exists.

```
git clone --filter=blob:none https://github.com/javawizard/onshape-std-library-mirror std
git -C std checkout "$(git -C std log --format=%h --grep='Version 2641.0$')"
python3 tools/fs_lint.py std
```

Three more tools work offline, from `B3_Parameters.fs` alone, and cost no API calls. `tools/b3_model.py` repeats the joints of `B3_Lib.fs` in Python: where every part and mate connector sits, open and stowed. `tools/b3_check.py` prints the clearances the hardware and the stowed stair depend on (door handle, wave washer room, the folded step under the folded stair) and fails if one runs out. `tools/stow_view.py` draws the stowed stair, the rail's outline and a plan of the room with the stair open, to scale, into `drawings/`. Run the last two after changing a number.

`tools/onshape_check.py` runs the lint, then loads `B3_AllInOne.fs` into a test Onshape document through the API, adds the features to two Part Studios (`B3 test Part Studio` and `B3 test Surroundings`) and reports what rebuilds, each part's size, and their mate connectors. `tools/onshape_assembly.py` then builds the assembly from them: it inserts every copy in one call, placed where its mates will hold it, then adds the groups and the mates. Both need `ONSHAPE_ACCESS_KEY`, `ONSHAPE_SECRET_KEY` and `ONSHAPE_DOC_URL` (a workspace URL of a document used only for testing), and touch only the `B3 test` tabs they create there.

API calls are limited, so the scripts keep the ids of what they made in a state file and reuse them. The first check costs about 16 calls, each rerun 4, and `--report` 2. The check also compares Onshape's mate connectors with the offline model's, so a joint changed in one and not the other shows up. The assembly costs about 31 calls (one per group and per mate), once, and the stowed one 2: it follows the Part Studios by itself, so after changing the FeatureScript only the check needs rerunning. When the parts are replaced rather than changed (their ids change, as when they became sheet metal), `--rebuild` starts a new assembly; if the API key can't delete tabs, the new one gets a numbered name and the old tab is left to delete by hand. `python3 tools/onshape_assembly.py --dry-run` prints the plan without calling the API, and with `--offline` it plans from the offline model, before the Part Studios exist.

## Changing dimensions

Edit `B3 Parameters` (or the parameters section at the top of the all-in-one file) and commit. The Part Studio rebuilds to match, flat patterns included. The names and values follow the "Part definitions" section of the Bed Stair B3 page, and each is commented with what it measures.

To check clearances through the fold, use the assembly: drag the tread, or animate the top tread's revolute mate, and measure.

## Coordinates

Same datum as the B3 page. The origin is on the floor, at the side panel's face, at the stair's bed end. Each part is built once, where its first copy sits when the stair is open (the top tread's bracket, axle and so on).

- **x** along the cabinet toward its front edge; the stair spans x = 0 to 11 1/2 (Onshape's X)
- **y** out from the panel face into the room (Onshape's **-Y**)
- **z** up from the floor (Onshape's Z)

The page's y runs along Onshape's -Y so that the model isn't mirrored: facing the bed-end wall, the cabinet is on the right, the stair to its left and the door handle left of the stair. The parameters and the code use the page's coordinates throughout; only the sketch-plane and solid helpers in `B3_Lib.fs` flip y. Onshape's Front view looks at the stair from the room, with the bed-end wall on the left.

## Limitations

- **Not modeled:** countersinks, which are picked in SendCutSend's configurator when ordering (the bracket's screw holes and the step ledge's are plain holes, so in the assembly each flat-head screw's head overlaps the rail bracket's web), the nut under the stow bracket's shelf, the stow parts' screws and the step clip screws' shanks. The wave washer is a flat ring squeezed to its gap.
- **Bushings** are the igus sizes the page calls for (14 mm bore, 1 mm flange, 4 mm long in a lip and 6 mm in an ear), and follow `AXLE_DIA` and `BUSHING_WALL`. McMaster's nearest, 2705T142, is 4.5 mm long: its sleeve would stand 0.3 mm proud inside a 1/8" lip and take half the wave washer's room.
- **Not yet rebuilt in Onshape:** the 17" treads, the 5 3/4" goings, the rail's straight lower edge, the room's reference blocks, and the bushings and pads are checked by the lint and the offline tools only. The rail's long flange now meets the bed-side flange at a 20 degree corner, which relies on Onshape's corner relief and may need a relief of its own.
- **Bushing holes** are modeled at their finished 14 mm. The page cuts them as 3/8" pilots, then reams them after bending, because most of them sit closer to a bend than SendCutSend's die-width rule allows; a flat pattern exported from the model has the finished size.
- **Flange corners:** every free flange corner is rounded. Where two flanges meet at a corner relief (the rail's foot, and each step leg's bottom corners, where the side flanges meet the foot flange), their tips stay square: Onshape's corner relief takes most of the short edge between them, and rounding the tips as well leaves it nothing to meet. Where a step leg's ear steps back to its flange, the outside corner is rounded 1/4" and the inside one 1/8"; the ear's top outer corner is 3/8", as the page's leg table gives it.

## Flat patterns for SendCutSend

Every sheet-metal part is an Onshape sheet-metal model with SendCutSend's own numbers for 5052: 1/8" sheet with a 0.125" inside bend radius and K 0.44, 3/16" sheet with 0.250" and K 0.43 (`SHEET_T`, `BEND_R_SHEET`, `K_SHEET`, `PLATE_T`, `BEND_R_PLATE`, `K_PLATE` in `B3_Parameters.fs`). They give SendCutSend's bend deductions, 0.217" and 0.356".

The document has one drawing per part, "Drawing: <part>" (the ones the script made end in "(in)"), holding the part's flat pattern at 1:1 with no border or title block; export it as DXF for SendCutSend. The drawings follow the model. `python3 tools/flat_drawings.py "<part>"` makes one in inches (about 5 calls; without `units` the API makes drawings that export in meters): the API can create the flat pattern view, but its bend notes and tangent lines still come out in the DXF until they are turned off on the view by hand.

`python3 tools/flat_patterns.py` writes the same flats to `flat/` as DXF files built from the flat patterns' geometry (8 calls): cut geometry on layer `CUT`, bend centerlines on layer `BEND`, and `flat-patterns.svg` to look at.

### Order list

All parts are 5052 H32 aluminum, cut and bent.

| Part | Qty | Sheet | Bends | Countersinking | Tapping | Hardware |
|---|---|---|---|---|---|---|
| Tread | 3 | 1/8" | 2 | none | none | none |
| Bracket | 4 | 3/16" | 2 | 3 holes, M6 flat head, 90 degrees, 0.488" (12.4 mm) major diameter, on the face the ears bend toward | none | On the 2 cabinet brackets only: 2 flush standoffs SO-M5-12 (M5 x 0.8 inside, smooth outside, 0.472" long) in the small ear holes, head on the inside of the ear so the standoff points outward. The 2 rail brackets get none: a pin there would be in the tread lip's way as it folds |
| Outer rail | 1 | 3/16" | 5 | none | none | none |
| Step clip | 2 | 1/8" | 1 | none | none | none |
| Step ledge | 1 | 1/8" | 2 | none | none | none |
| Hook-on step leg | 2 | 1/8" | 3 | none | 2 holes M6 x 1.0 (the 5 mm holes, one in each ear) | none |
| Stow bracket | 1 | 1/8" | 2 | none | none | none |

The standoffs are the treads' stop pins: the folded-down lip's bottom edge rests on the smooth body. The ear hole is 0.281", SendCutSend's size for this standoff, 0.32" from the ear's edge (0.315" at least), and the standoff ends 0.10" past the lip's outer face.
