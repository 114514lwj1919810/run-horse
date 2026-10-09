# Race Presentation

## Ownership

` GD32 -> protocol/display state -> AnimalRaceAnimationController -> RacePresentationPlan -> AnimalRaceUI `

- GD32 supplies the authoritative winner. Presentation never changes balances, odds, payouts or the winner.
- `AnimalRaceAnimationController` adapts the current round to the presentation request, owns the clock/pause and sends one `RACE_OVER` at contact.
- `RacePresentationPlan` generates all six racers once from winner, scene, motion duration and seed. Sampling is deterministic and has no page, serial, GameObject or wall-clock dependency.
- `RaceObstacleTimeline` owns world positions, action phases, source frames and prop positions for one racer.
- `RaceMotionCurve` integrates smoothly changing nonnegative speeds. Shared boundary speeds prevent position correction or speed jumps between phases.
- `RaceCourseGeometry` is the shared authority for lanes, course length and painted-nose finish contacts.
- `AnimalRaceUI` draws the returned poses. Running cadence follows running distance; winner celebration remains an in-place animation.

## Entry Point

```csharp
var request = new RacePresentationRequest(winnerId, sceneIndex, motionSeconds, seed);
var plan = new RacePresentationPlan(request);
RaceObstaclePose pose = plan.Sample(animalId, elapsedSeconds);
bool touched = plan.HasWinnerTouchedLine(elapsedSeconds);
```

IDs: 1 lion, 2 elephant, 3 gorilla, 4 zebra, 5 giraffe, 6 camel.
Scenes: 0 desert/barrels, 1 forest/pond, 2 castle/balls; -1 is a no-prop fallback.
The adapter currently uses `CreateSeed(sequence, round)`. Retransmitting the same round therefore does not reroll its presentation.

The existing timing contract is unchanged: the adapter reserves 1.5 seconds of `duration_ms` for the finish presentation and passes the remainder as `MotionSeconds` (minimum 0.25 seconds). The winner touches the painted finish line at the end of that motion interval. Contact immediately locks the scene, starts the shutter and sends `RACE_OVER`; it does not wait 1.5 seconds before starting the shutter. Collection remains controlled by the existing display state and start button.

## Variation And Contact

- Early lead, chase, faster obstacle handling and randomly placed running surges are available across scenes.
- Desert additionally supports a clean jump or falling and recovering quickly. Fallen landing centers are 37-72 world units beyond the barrel's left edge; clean landings are 80-115 units beyond it.
- Forest preserves three exact leaf centers and random 0.40-0.65 second rests. Jump/landing sprites retain the recovered 5/6 and 9/10 frame mapping.
- Castle randomizes acceleration onset and strength for every racer, not only the winner. Riding starts at the approach speed and increases smoothly by 22-40 percent (45 percent for the rolling-surge winner). Mount, ride and dismount times are solved together so the ride is faster than approaching ground travel, not just faster than an earlier slow ride frame.
- Ball acceleration starts around 12-34 percent into the ride and rises smoothly. The rider foot and ball share the same sampled X; Y remains at the ball surface. Ball-frame changes follow rolling distance and stop at dismount.
- Winner and loser endpoints are planned before playback. No late-frame teleport, loser pushback or catch-up correction is applied at the finish. Earlier ranking is allowed to differ from final ranking.
- The public performance profile controls approach bias, jump/recovery preference, surge timing and ball acceleration. Keep foot targets and endpoint constraints independent of these knobs.

## Verification

Pace tuning: the course remains 2600 world units with unchanged round duration. Ground-section time is allocated by distance; later average running speed is 1.0-1.32 times the approach average, depending on the replay profile. Initial speed is 35-50 percent of approach cruise speed with a gradual ramp. Ordinary pace fluctuations are smaller and surges do not occur on the initial ramp. The prop area stays at 13/28 of the course distance from the finish. Artwork, leaf rests and barrel stuns are unchanged; ball action timing follows actual travel speed. Fallen landing centers remain 37-72 units left of the barrel edge.

Run `ObstacleCourseTests.BuildValidatedPc` in Unity batch mode to run protocol, presentation and asset-contact tests before the PC build.

`RacePresentationTests` covers 3 scenes x 6 winners x 32 seeds, additional durations, deterministic replay, monotonic motion, phase-boundary velocity continuity, varied early leaders, comeback winners, fall/clear outcomes, ball acceleration/foot contact and immediate finish detection. `ObstacleCourseTests` additionally checks original-frame selection, leaf rests, enlarged prop geometry, score units and controller completion.

PC smoke tests and screenshots are in `E:/unity/project/32test/.codex-smooth-pace/`. The latest build log is `E:/unity/project/Unity_AnimalRace/smooth_pace_pc_build_6.log`. No GD32 source or Android APK is changed by this presentation update.
