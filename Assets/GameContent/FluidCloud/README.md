# Fluid Cloud

GPU cloud interaction prototype for the project's Built-in Render Pipeline.

## What it uses

- A low-resolution, texture-based 2D Stable Fluids solver.
- Semi-Lagrangian velocity and density advection.
- Pressure projection and vorticity confinement.
- A world-space player capsule source that injects velocity and cloud clearance.
- A lightweight cloud surface for mobile and an optional raymarched cloud volume for PC.
- A generated, tileable 3D Worley-noise texture; no external texture asset is copied.

## Reference project

Architecture was studied from `JuconChen/HDRP-FluidProject` (Unity 2020.3 / HDRP 10.6).
That repository did not include a license when inspected on 2026-08-22. For that reason,
no source code, shader code, material, scene, or texture from the repository is included here.
This module is a clean-room implementation for Unity 2022.3 Built-in RP.

## Scene setup

Run `Tools > XiYouJi > Build Fluid Cloud Scene` to create
`Assets/GameContent/01_Scenes/cloud.unity`.

The scene uses the project's standard gameplay stack:

- `PointClickNavController` and `NavMeshAgent` for click-to-move.
- `PlayerAnimationDriver` for the existing walk animation.
- `BoundedDiscoCamera` with the same fixed isometric angle and FOV as `village`.
- `PolygonWalkableArea`, `CameraMovementBounds`, and a baked legacy NavMesh.
- `FluidCloudInteractor` only reads the existing agent velocity; it never controls the player.

The PC volume object disables itself on mobile platforms. The surface remains available as
the mobile fallback. Actual Android performance still needs verification on the target device.
