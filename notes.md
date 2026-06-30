# What to do in Unity Editor

## 1. Car Visual (no car models found in project)

Your project has no car FBX/OBJ files — only people, houses, and city props. To swap in your car:

- Import your car model into `Assets/`
- In the scene, find the GameObject with `ArcadeCarController` (the box)
- Drag your car mesh as a child of that GameObject, position/scale it to fit the existing collider
- Or replace the entire car GameObject — just make sure `ArcadeCarController`, `Rigidbody`, `CarImpactHandler`, and a `Collider` stay on the root

## 2. Score HUD Setup

1. **GameObject > UI > Canvas** — set to Screen Space - Overlay
2. Add 3 child TMP Text objects:
   - `HitPopup` — large font (~80pt), anchored top-center, set alpha to 0
   - `ComboTimer` — medium font (~30pt), just below popup
   - `TotalScore` — small font (~24pt), top-right corner
3. Add `ScoreManager` script to an empty GameObject in the scene
4. Add `ScoreUI` script to the Canvas root (or any UI object) and wire the 3 text refs in Inspector

## 3. Pedestrian Variety

On your `MyPrefabs/strong man c` prefab (or any NPC prefab):

1. Add `RandomPedestrianAppearance` component
2. Drag the 9 materials from `Assets/DavidJalbert/LowPolyPeople/FBX/Materials/` (palette1–9) into the Palettes array

Then in `WorldPopulator`, add multiple NPC prefab entries under **NPC Types** — use all the prefabs from `Assets/DavidJalbert/LowPolyPeople/Prefabs/` (normal/stout/strong variants) — but each one needs `NPCWander`, `RagdollController`, `NavMeshAgent`, `Health`, and `RandomPedestrianAppearance` components set up first, just like `strong man c`.

## 4. Animations — already fixed
