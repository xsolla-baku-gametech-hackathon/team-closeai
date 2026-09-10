# Unity A/B Netcode Demo

Five complete, commented C# MonoBehaviours for a controlled freeze-and-snap versus inertial dead-reckoning demonstration. No third-party networking library is required.

## Scope and honest claims

- Recommended target: Unity 2022.3 LTS or Unity 6, with Unity UI/uGUI installed. Built-in Render Pipeline is the simplest setup; URP also works with two independent Base cameras, not a camera stack.
- This is a local, position-only, shared burst-loss experiment. Every normal simulation frame delivers one identical position/time snapshot to both clients. Active loss discards 100% of samples. It is not a fixed-tick transport simulator.
- The classic implementation intentionally freezes without snapshots, then snaps. It is NOT actual rollback netcode. Real rollback restores past simulation state and replays buffered inputs; well-designed rollback or snapshot clients can also smooth presentation. Do not use this demo to claim all standard/rollback networking behaves this way.
- The requested inertial behavior is dead reckoning plus presentation-error smoothing, not an established networking protocol or a biologically validated motion model.
- The UI's 150 ms ping is explicitly ILLUSTRATIVE. No delay queue is applied. Adding real one-way latency would mean recovery snaps to a delayed snapshot, not the authority's actual current position. This demo prioritizes the requested immediate recovery behavior.
- CPU telemetry is measured, not a fabricated constant. '< 0.05 ms' is a target, not a guarantee. The number covers this inertial component's snapshot processing and Update, including timer/Transform overhead; it excludes rendering, UI, transport dispatch, and other clients. Max is the maximum within the latest roughly 250 ms reporting window, not lifetime max.
- 0.05 seconds gives three frame intervals at 60 FPS. It cannot simultaneously be frame-rate independent and always last exactly 2–3 frames. At 30 FPS a 50 ms recovery spans roughly two intervals; at 120 FPS, six.
- A large correction compressed into 50 ms can still LOOK abrupt. The blend is position-continuous when evaluated over time, not a guarantee of invisible correction at arbitrary displacement or frame rate. For longer outages, use 0.15–0.25 seconds and/or cap error-correction speed in a real game.
- Supplied as complete demo source, not a production transport stack. Unity compilation, Play Mode, and hardware profiling have NOT been run in the authoring environment. See VALIDATION.md for the exact checks performed.

## Files

1. `TargetMovement.cs`: hidden authoritative ellipse; exposes analytic world position, velocity, and acceleration.
2. `NetworkPacketLossSimulator.cs`: shared packet gate; public `IsPacketDropped`, `PacketLossChanged`, `SnapshotReceived`, and button-callable `TogglePacketLoss` / `SetPacketLossActive`.
3. `ClassicNetcodeDummy.cs`: event-driven freeze/snap baseline, with no movement Update during loss.
4. `InertialNetcodeDummy.cs`: finite-difference velocity/acceleration, bounded damped prediction, and smooth recovery error decay.
5. `DemoUIController.cs`: 50/50 cameras, red/green status, moving object labels, button wiring, real FPS, illustrative ping, and measured CPU telemetry.

All classes use the `InertialNetcodeDemo` namespace. Unity Inspector attachment works normally. File names must remain identical to class names.

## 1. Import and project settings

1. Create a 3D project. In Package Manager, ensure **Unity UI** (`com.unity.ugui`) is installed.
2. Copy all five `.cs` files into `Assets/InertialNetcodeDemo/Scripts/` and wait for compilation. The Markdown/Python files are documentation/validation only and do not need to be imported.
3. If using custom assembly definitions, reference the uGUI assembly (`Unity.ugui`) and, when enabled, the Input System assembly (`Unity.InputSystem`). The simplest setup is to use no custom assembly definition for these files.
4. For keyboard input, use either **Input Manager (Old)**, **Input System Package (New)**, or **Both**, under Project Settings > Player > Active Input Handling. Install the Input System package if choosing New/Both; restart the editor if prompted. Conditional compilation supports both backends without double toggling.
5. Start with a 1920x1080 or 1280x720 Game view. The cameras adjust their orthographic size on resize. Very narrow portrait layouts will naturally make labels harder to read.
6. Keep Time Scale at 1. The simulation and recovery use scaled `Time.deltaTime`; FPS uses unscaled time. At Time Scale 0 the trajectory/prediction pause, while UI/hotkeys remain active.

## 2. Suggested hierarchy

```text
Demo
├── MasterTarget                  TargetMovement (no Renderer)
├── Network                       NetworkPacketLossSimulator
├── ClassicWorld
│   ├── Ground                    Plane, layer DemoClassic
│   └── ClassicAvatar             Sphere, ClassicNetcodeDummy, layer DemoClassic
├── InertialWorld
│   ├── Ground                    Plane, layer DemoInertial
│   └── InertialAvatar            Sphere, InertialNetcodeDummy, layer DemoInertial
├── ClassicCamera                 Camera (orthographic)
├── InertialCamera                Camera (orthographic)
├── Directional Light
├── Canvas                        Canvas, CanvasScaler, GraphicRaycaster
│   ├── Status                    Text (Legacy)
│   ├── Telemetry                 Text (Legacy)
│   ├── ClassicLabel              Text (Legacy)
│   ├── InertialLabel             Text (Legacy)
│   ├── Divider                   Image (optional)
│   └── ToggleLossButton          Button
│       └── Text                  Text (Legacy)
├── EventSystem                   Exactly one matching UI input module
└── DemoUI                        DemoUIController
```

Keep root/parent scales at `(1,1,1)` and rotations at `(0,0,0)`. Display translations are applied in world space by the client scripts. Do not also move the world parents to +/-20; position their ground children explicitly as below.

## 3. Authority and packet gate

1. Create empty `MasterTarget`, attach `TargetMovement`, and leave it invisible.
2. Set Center `(0, 0.5, 0)`, Radius X `6`, Radius Z `4`, Period Seconds `8`, Initial Phase Degrees `0`.
3. Create empty `Network`, attach `NetworkPacketLossSimulator`, and drag `MasterTarget` into **Target**.
4. Leave **Start With Packet Loss** off, **Enable Spacebar** on, and **Illustrative Ping Milliseconds** at `150`.

The execution order attributes guarantee target motion before snapshot delivery, then client motion, then UI LateUpdate. Do not override these orders in Project Settings.

## 4. Client objects and ground

1. Add layers `DemoClassic` and `DemoInertial` under Project Settings > Tags and Layers.
2. Create a Sphere named `ClassicAvatar`, scale `(1,1,1)`, assign layer `DemoClassic`, and attach `ClassicNetcodeDummy`.
3. Assign the `Network` component to its **Network** reference. Set **Presentation Offset** to `(-20,0,0)`. Give it a blue material.
4. Create a Sphere named `InertialAvatar`, assign layer `DemoInertial`, attach `InertialNetcodeDummy`, and assign the SAME `Network` component.
5. Set its **Presentation Offset** to `(20,0,0)`. Give it a yellow/orange material. Keep initial tuning:
   - Damping Per Second: `0.45`
   - Maximum Prediction Seconds: `1.5`
   - Maximum Speed: `15` (clamps the history-derived initial velocity estimate)
   - Maximum Acceleration: `30` (clamps the history-derived initial acceleration estimate)
   - Reconciliation Seconds: `0.05`
   - Maximum History Gap Seconds: `0.25`
6. Do not attach a Rigidbody, CharacterController, Animator root motion, NavMeshAgent, or another Transform writer to either client. Remove the sphere colliders if they are not needed; this is a visual demonstration, not a collision simulation.
7. Create Plane `ClassicWorld/Ground`: world position `(-20,0,0)`, scale `(1.6,1,1.6)`, layer `DemoClassic`.
8. Create Plane `InertialWorld/Ground`: world position `(20,0,0)`, same scale, layer `DemoInertial`. Give both planes an identical neutral material.
9. Keep the two world-parent GameObjects at the origin. The clients snap to their first accepted snapshot on startup, so manually matching their initial sphere positions is optional.

Only translation differs between the presentations. Both clients operate on the exact same logical coordinates; this is not two differently phased target simulations.

## 5. Cameras

1. Create `ClassicCamera`:
   - Position `(-20,20,0)`
   - Rotation `(90,0,0)`
   - Projection **Orthographic**
   - Size `9` initially; the UI controller adjusts this based on panel aspect ratio.
   - Clipping planes `0.1` / `100`
   - Culling Mask: `DemoClassic` only
   - Solid-color background; leave the camera enabled.
2. Create `InertialCamera` with the same settings, but position `(20,20,0)` and Culling Mask `DemoInertial` only.
3. The controller sets left camera viewport `(0,0,0.5,1)` and right camera `(0.5,0,0.5,1)` automatically.
4. Remove/disable the original Main Camera if it still exists. Keep **only one AudioListener** in the scene, or remove both if no audio is needed.
5. For URP, use two **Base** cameras with independent viewports; do not stack them. For other pipelines, verify independent camera viewport rendering in your pipeline/version.
6. Keep one directional light illuminating both layers. Use identical lighting/post-processing for a fair A/B view.

## 6. Canvas and text

1. Create a UI Canvas with Render Mode **Screen Space - Overlay**.
2. On its CanvasScaler, choose **Scale With Screen Size**, Reference Resolution `(1920,1080)`, Match `0.5`.
3. Use **UI > Legacy > Text** for the text objects. Do not use TextMeshProUGUI with these serialized `Text` fields. Assign an available font if the Text component has none.
4. Create `Status`: top-center anchors/pivot, anchored position `(0,-25)`, size `(1100,45)`, font size `28`, centered. The controller owns its status text and color.
5. Create `Telemetry`: top-center anchors/pivot, position `(0,-80)`, size `(1780,150)`, font size `20`, centered, white. Turn Rich Text off if desired. Four lines include the methodology disclaimer.
6. Create `ClassicLabel` and `InertialLabel` as direct children of Canvas: center anchors, pivot `(0.5,0.5)`, size `(520,70)`, font size `22`, centered, horizontal wrap enabled. Their positions are controlled each LateUpdate using the corresponding camera. Use a Shadow/Outline component if readability needs improvement.
7. Disable **Raycast Target** on all noninteractive Text/Image elements so they cannot block the button.
8. Optionally add a thin centered vertical Divider Image, width `2`, stretching between top/bottom; disable Raycast Target.
9. Create `ToggleLossButton` with its label as **Text (Legacy)**: bottom-center anchors/pivot, position `(0,25)`, size `(400,60)`.
10. Ensure exactly one EventSystem. Use **StandaloneInputModule** for the legacy backend, or **InputSystemUIInputModule** with its default UI action assignments for the new backend; do not run both UI modules simultaneously.

## 7. Connect the controller

Create empty `DemoUI`, attach `DemoUIController`, and assign:

| Field | Scene object/component |
|---|---|
| Network | NetworkPacketLossSimulator on Network |
| Classic Client | ClassicNetcodeDummy on ClassicAvatar |
| Inertial Client | InertialNetcodeDummy on InertialAvatar |
| Classic Camera | ClassicCamera |
| Inertial Camera | InertialCamera |
| Overlay Canvas | Canvas |
| Packet Loss Text | Status Text |
| Telemetry Text | Telemetry Text |
| Classic Label | ClassicLabel Text |
| Inertial Label | InertialLabel Text |
| Toggle Loss Button | ToggleLossButton Button |
| Toggle Button Text | ToggleLossButton's Text child |

**Leave the button's Inspector On Click list empty.** The controller adds/removes its listener automatically. Alternatively, omit the controller's button reference and manually wire On Click to Network > NetworkPacketLossSimulator.TogglePacketLoss, but never wire both ways.

Save the scene and press Play. Click inside the Game view so Space reaches it. Allow at least three normal frames to establish acceleration history. Press Space or click the button to start loss; press/click again to restore packets.

## Expected behavior

- Normal: both spheres show identical motion after accounting for their +/-20 world translations.
- Loss: the classic sphere stops receiving position writes and freezes. The inertial sphere predicts using only its last received motion history.
- Recovery: the classic sphere immediately snaps to the current authoritative sample. The inertial sphere retains its last rendered position on the recovery frame, then fades its error relative to the moving authority over the configured time.
- Sustained loss: inertial prediction stops after 1.5 seconds instead of allowing unbounded extrapolation. Velocity is stopped at the horizon; position remains continuous. This deliberately trades velocity smoothness for bounded prediction.
- Consecutive outages: a new outage starts from the current rendered position, including any unfinished correction, so there is no position reset.
- Loss at startup: with no accepted snapshot, neither client has usable motion history. They hold their initial scene positions. First delivery establishes initial placement rather than pretending to reconstruct unknown history.
- Disable/re-enable: event subscriptions are removed/re-added. Re-enabling a client reinitializes it from the last DELIVERED snapshot, even during loss; it is a demo reset, not seamless pooled-spawn restoration.

## Kinematics and recovery details

For accepted positions p0, p1, p2 and timestamps t0, t1, t2:

```text
h0 = t1 - t0
h1 = t2 - t1
u0 = (p1 - p0) / h0
u1 = (p2 - p1) / h1
a2 = 2 * (u1 - u0) / (h0 + h1)
v2 = u1 + 0.5 * a2 * h1
```

The acceleration denominator uses the separation of the interval midpoints, not just the newest frame's dt. For constant acceleration this recovers endpoint velocity/acceleration exactly even at unequal sample intervals. On recovery/gaps the history resets, rather than treating an entire outage as one normal interval. One fresh sample gives position, two give velocity, three give acceleration. These are estimates, not guaranteed true derivatives of arbitrary motion/noisy network samples.

With zero damping the prediction is exactly:

```text
pNew = p + v * dt + 0.5 * a * dt^2
vNew = v + a * dt
```

With damping lambda, the code integrates this continuous model:

```text
a' = -lambda * a
v' = a - lambda * v
p' = v
```

Here `a` is a decaying driving-acceleration estimate. The exact closed-form solution avoids frame-dependent per-frame damping and numerical substep overhead. Small-argument series avoid cancellation near lambda*dt=0. The finite prediction horizon bounds unseen motion in time. Initial speed/acceleration estimates are clamped; this is not a collision-aware world-distance boundary or a hard speed cap throughout prediction.

Recovery retains an initial presentation offset e0, then follows:

```text
u = clamp01(recoveryElapsed / recoveryDuration)
ease = u*u*(3 - 2*u)
displayedPosition = latestAuthoritativePosition + Lerp(e0, zero, ease)
```

Only the FIRST recovery snapshot starts the correction timer. Subsequent snapshots move the authority normally without restarting the blend. A smoothstep curve removes the initial/final error-correction velocity; it does not guarantee matching the old predicted trajectory's velocity. This presentation transform is not a suitable authoritative gameplay/collision transform in a real networked game.

## Acceptance checklist in Unity

1. Compile with no Console errors in your chosen Unity version/backend.
2. Observe normal alignment for at least 10 seconds.
3. Drop packets for 0.25–0.5 seconds and restore: classic freezes/snaps; inertial predicts/reconciles.
4. Try 1 second and 5 seconds: observe accumulated prediction error and the 1.5-second safety horizon; do not claim indefinite accuracy.
5. Toggle rapidly during reconciliation: verify no reset to the hidden master occurs on a new outage.
6. Test 30, 60, 120 FPS with VSync disabled for controlled limits. Motion/recovery duration should be time-based, while the number of displayed blend frames changes.
7. Resize the Game view; check both camera tracks remain visible and labels stay in their own panels.
8. Test both Space and the button; verify one action changes the state once.
9. Disable/re-enable clients; verify no duplicate subscriptions and the documented reset behavior.
10. Start with packet loss active; verify no invented velocity until samples arrive.
11. Set Time Scale 0; verify movement pauses while UI still operates.
12. Profile a standalone build on target hardware after warm-up. Use the Unity Profiler for CPU/GC analysis. Do not use Editor-only sub-millisecond timings as production performance proof.

The movement/prediction hot path uses value types and no explicit managed allocation. UI string formatting allocates at 4 Hz by design; first-use/JIT/engine internals may still allocate. Real production networking additionally needs fixed-tick input/snapshot buffering, sequencing, latency/jitter/reordering simulation, quantization, clock synchronization, authority/security, collisions, interest management, and automated Unity tests. None is disguised as implemented here.
