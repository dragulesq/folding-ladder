# Folding bed ladder

FeatureScript for Onshape that builds design **B3** of the folding camper bed stair: two folding treads in 5052 aluminum that fold flat against the cabinet side, plus a hook-on bottom step. All the numbers live in one parameters file, so the model can be resized without touching the part code.

## Files

| File | Feature | What it makes |
| --- | --- | --- |
| `onshape/B3_Parameters.fs` | (none) | Every dimension, in one place |
| `onshape/B3_Lib.fs` | (none) | Shared helpers and derived positions |
| `onshape/B3_Reference.fs` | B3 Reference | Floor, cabinet, bed-end wall, mattress, door handle (parts named `REF ...`) |
| `onshape/B3_Treads.fs` | B3 Treads | Top and middle treads |
| `onshape/B3_Brackets.fs` | B3 Cabinet Brackets | The two brackets with pivot ears |
| `onshape/B3_OuterRail.fs` | B3 Outer Rail | Stepped rail with flanges, hand slot and pivot ears |
| `onshape/B3_PivotHardware.fs` | B3 Pivot Hardware | Four axles and four stop pins |
| `onshape/B3_FrontAngle.fs` | B3 Front Angle | Angle on the cabinet's front post |
| `onshape/B3_StepLedge.fs` | B3 Step Ledge | Ledge that holds the folded step |
| `onshape/B3_HookOnStep.fs` | B3 Hook-on Step | Step tray and two legs (provisional) |
| `onshape/B3_AllInOne.fs` | all of the above | Everything joined into one file, no imports needed |

## Quick start: one Feature Studio

1. In your Onshape document, click **+** at the bottom left and choose **Create Feature Studio**. Name it `B3`.
2. Select all the text in it, then paste the whole of `onshape/B3_AllInOne.fs` over it.
3. Click the green check (**Commit**) at the top right.
4. Open a Part Studio. The B3 features appear at the right end of the toolbar. Add each one; none of them asks for any input.

## Matching the repo: one Feature Studio per file

1. Create a Feature Studio for each file in `onshape/` except `B3_AllInOne.fs`, named after the file without `.fs` and with a space instead of the underscore (`B3 Parameters`, `B3 Lib`, `B3 Treads` and so on).
2. Paste each file into its Feature Studio.
3. In **B3 Lib**, delete the lines between `<B3-IMPORT>` and `</B3-IMPORT>`, click **Import** in the toolbar and pick **B3 Parameters**. Then type `export ` in front of the line it inserts, so the parameters pass through to every part.
4. In every part file, delete the same block, click **Import** and pick **B3 Lib**.
5. Commit each one, then add the features in a Part Studio.

When you paste an updated file from the repo later, keep the import line Onshape made and paste the rest below it.

## Changing dimensions

Edit `B3 Parameters` (or the parameters section at the top of the all-in-one file) and commit. The Part Studio rebuilds to match. The numbers are commented with what they measure.

Set `FOLD_ANGLE` to `90 * degree` to see the stair folded flat, or any angle in between to check clearances through the swing. The treads, outer rail and outer axles move; the brackets and everything on the cabinet stay put.

## Coordinates

All parts are built where they sit when the stair is open:

- **X** out from the cabinet's side panel face into the room (panel face at X = 0)
- **Y** along the stair's depth, from the bed-end wall (Y = 0) toward the cabinet's front post
- **Z** up from the floor

## Limitations

- **Solid parts.** Parts are plain solids with sharp 90° bends, not Onshape sheet-metal parts, so there are no flat patterns yet.
- **Unfinished details.** Not modeled yet: corner relief on the treads, bend reliefs, countersinks and the hook-on step's leg hinges.
- **Hook-on step is provisional.** The B3 design page doesn't detail it yet.
