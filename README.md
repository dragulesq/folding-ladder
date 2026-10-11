# Folding bed ladder

FeatureScript for Onshape that builds design **B3** of the folding camper bed stair: two folding treads in 5052 aluminum that fold flat against the cabinet side, plus a hook-on bottom step. All the numbers live in one parameters file, so the model can be resized without touching the part code.

The features build each part once, the way Onshape expects: a Part Studio holds one of every part to make, and an assembly places the copies and joins them with mates, so the fold can be simulated. Each part carries named mate connectors where it joins the next one.

## Files

| File | Feature | What it makes |
| --- | --- | --- |
| `onshape/B3_Parameters.fs` | (none) | Every dimension, in one place |
| `onshape/B3_Lib.fs` | (none) | Shared helpers and derived positions |
| `onshape/B3_Reference.fs` | B3 Reference | Floor, cabinet, bed-end wall, mattress, door handle (parts named `REF ...`), for a Part Studio of their own |
| `onshape/B3_Treads.fs` | B3 Tread | The tread (used twice) |
| `onshape/B3_Brackets.fs` | B3 Cabinet Bracket | The bracket with its pivot ears (used twice) |
| `onshape/B3_OuterRail.fs` | B3 Outer Rail | Stepped rail with flanges, hand slot and pivot ears |
| `onshape/B3_StepClip.fs` | B3 Step Clip | Clip inside the outer rail with the outer keyhole |
| `onshape/B3_PivotHardware.fs` | B3 Pivot Hardware | Tread axle (used 4 times) and step axle (2), 12 mm tube; M6 fender washer and M6 button-head screw (12 each, one per axle end); stop pin (4) |
| `onshape/B3_FrontAngle.fs` | B3 Front Angle | Angle on the cabinet's front post |
| `onshape/B3_StepLedge.fs` | B3 Step Ledge | Ledge that holds the folded step |
| `onshape/B3_HookOnStep.fs` | B3 Hook-on Step | Step tray and its two folding legs |
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

| Part | Copies | Mate | Its connector | Goes on |
| --- | --- | --- | --- | --- |
| REF floor, cabinet, bed-end wall, mattress, door handle | 1 each | Fix | | |
| Cabinet bracket | 2 | Fastened | Cabinet | REF cabinet: Bracket, top tread / Bracket, middle tread |
| Front angle | 1 | Fastened | Cabinet | REF cabinet: Front angle |
| Step ledge | 1 | Fastened | Cabinet | REF cabinet: Step ledge |
| Stop pin | 4 | Fastened | Bracket | Each bracket: Stop pin, back / Stop pin, front |
| Tread axle | 2 | Fastened | Back end | Each bracket: Axle |
| Tread | 2 | Revolute | Cabinet axle | Each bracket: Axle |
| Outer rail | 1 | Revolute (twice) | Top tread axle / Middle tread axle | Top tread / middle tread: Outer axle |
| Tread axle | 2 | Fastened | Back end | Outer rail: Top tread axle / Middle tread axle |
| Step clip | 1 | Fastened | Outer rail | Outer rail: Step clip |
| Hook-on step tray | 1 | Fastened | Cabinet-side axle | Front angle: Hook-on step |
| Step axle | 2 | Fastened | Back end | Tray: Cabinet-side axle / Outer axle |
| Hook-on step leg, cabinet side / outer | 1 each | Revolute | Axle | Tray: Cabinet-side axle / Outer axle |
| M6 fender washer | 12 | Fastened | Axle | Each axle: Back end / Front end |
| M6 button-head screw | 12 | Fastened | Washer | Its washer: Screw |

To fold the stair, drag the top tread up toward the cabinet: the treads and rail move as a parallelogram on the four revolute mates. Lift the hook-on step off first, as on the stair itself; in the assembly it stays on the front angle, so suppress or hide it to see the folded stair clearly. The legs fold on their own revolute mates.

`tools/onshape_assembly.py` builds this assembly through the API (see below).

## Checking the code

`tools/fs_lint.py` checks the files offline: brackets balance, `B3_AllInOne.fs` is up to date, and, given a copy of the FeatureScript standard library at the version the files import, every function, constant and enum value they use exists.

```
git clone --filter=blob:none https://github.com/javawizard/onshape-std-library-mirror std
git -C std checkout "$(git -C std log --format=%h --grep='^Version 2641.0$')"
python3 tools/fs_lint.py std
```

`tools/onshape_check.py` runs the lint, then loads `B3_AllInOne.fs` into a test Onshape document through the API, adds the features to two Part Studios (`B3 test Part Studio` and `B3 test Surroundings`) and reports what rebuilds, the parts, and their mate connectors. `tools/onshape_assembly.py` then builds `B3 test Assembly` from them: it inserts every copy in one call, placed where its mates will hold it, and adds the mates. Both need `ONSHAPE_ACCESS_KEY`, `ONSHAPE_SECRET_KEY` and `ONSHAPE_DOC_URL` (a workspace URL of a document used only for testing), and touch only the `B3 test` tabs they create there.

API calls are limited, so the scripts keep the ids of what they made in a state file and reuse them. The first check costs about 16 calls, each rerun 4, and `--report` 2. The assembly costs about 50 calls (one per mate), once: it follows the Part Studios by itself, so after changing the FeatureScript only the check needs rerunning. `python3 tools/onshape_assembly.py --dry-run` prints the plan without calling the API.

## Changing dimensions

Edit `B3 Parameters` (or the parameters section at the top of the all-in-one file) and commit. The Part Studio rebuilds to match. The names and values follow the "Part definitions" section of the Bed Stair B3 page, and each is commented with what it measures.

To check clearances through the fold, use the assembly: drag the tread, or animate the top tread's revolute mate, and measure.

## Coordinates

Same datum as the B3 page. The origin is on the floor, at the side panel's face, at the stair's bed end. Each part is built once, where its first copy sits when the stair is open (the top tread's bracket, axle and so on).

- **x** along the cabinet toward its front edge; the stair spans x = 0 to 12 (Onshape's X)
- **y** out from the panel face into the room (Onshape's **-Y**)
- **z** up from the floor (Onshape's Z)

The page's y runs along Onshape's -Y so that the model isn't mirrored: facing the bed-end wall, the cabinet is on the right, the stair to its left and the door handle left of the stair. The parameters and the code use the page's coordinates throughout; only the sketch-plane and solid helpers in `B3_Lib.fs` flip y. Onshape's Front view looks at the stair from the room, with the bed-end wall on the left.

## Limitations

- **Solid parts.** Parts are plain solids with sharp 90° bends, sized to the outside of the bends, not Onshape sheet-metal parts, so there are no flat patterns yet. The page's bend radii and K-factor are in the parameters for that step.
- **Not modeled:** bend reliefs, countersinks, the notches left where the rail's ear tabs are cut, latch and keeper holes, bushings, thrust and wave washers, and rubber parts. Each axle screw therefore sits about 1.2 mm off its lip, where the bushing flange goes.
