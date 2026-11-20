# Test and capture procedure - Target Range XR

## Automated checks

After activating Unity, run `Tools/Run-Checks.ps1` from PowerShell. It runs EditMode logic tests and PlayMode `RangeTests` plus `DesktopTests`, and builds Windows only after both suites pass. Test XML and logs must be inspected. Capture helpers are intentionally excluded from this fast regression suite.

The existing project uses Input System actions; do not switch Active Input Handling to legacy input to work around controls. A successful source-only compilation is not a substitute for these checks.

## Desktop acceptance

1. Open Range and press Play. Confirm Ready, no held gun, 01:00, and no moving countdown before E.
2. At 1280x720, 1600x900 and 1920x1080, confirm the start card, buttons, HUD and results are within the viewport with readable text.
3. Press E once: exactly one pickup, one round start, cursor captured, gun below-right of the center reticle. Simulated controllers/large simulator overlay/world boards must not cover the view.
4. Move/aim with WASD and mouse. Test arrow-key aim and Space firing with no mouse. Confirm movement stays in the booth and does not cross the bench.
5. Aim at near, moving and far targets. Fire one click: one registered shot, barrel flash/recoil/audio, projectile hit, knockdown, score update and popup. Verify no consistent crosshair offset.
6. Press E to return the gun. Click/Space while unheld must not fire. Re-equip during the round; the clock must not restart.
7. Press Escape. No camera movement or firing should occur while the cursor is free. Click Resume or press Escape to recapture; the resume click must not fire. Lose/recover focus and repeat.
8. Let the real 60 seconds elapse. Confirm physics and shooting freeze, final score is visible, buttons are clickable and R/Enter works without a mouse.
9. Restart while a shot or recoil is in progress. Confirm old bullets are gone, gun flash is off, gun returns home, score is zero and the next round waits for pickup. Repeat at least three rounds.
10. Press Q on results or click Exit in the standalone player. Confirm it exits normally.

## XR simulator acceptance

1. F2 switches to simulator, resetting score/time/gun. F1 reveals help and hides it again without disabling simulated devices.
2. Hold Space to select the right controller. Move its ray onto the gun first, then press and hold G to grip. Confirm the gun is genuinely selected by XRGrabInteractable.
3. Aim the selected controller and pull the simulated trigger with Left Mouse. The gun must follow the hand, fire, and knock a target down with score feedback.
4. Release grip and wait for return to bench. Re-grab and continue.
5. Complete a 60-second round. Confirm world-space results are readable and controller rays can activate Restart and Exit.
6. Switch back to desktop with F2 and re-enter simulator. Repeat pickup/fire to catch lost device/action subscriptions.

## Recording and screenshots

Use screen recording with the simulator mode for the mandatory MP4/WebM. Include ready scene, controller pickup, several aimed shots, target knockdown/disappearance, score/time updates, normal expiry, results and replay. Do not splice in a desktop-only run as simulator evidence. Record the current build/revision and state if capture is scripted.

`TargetRange.Tests.PlayMode.DemoRecorder.RecordGameplayVideo` can capture scripted simulator frames in a focused editor window. It uses queued simulated input and programmatic XR selection; it does not replace a manual usability test. Its raw frames and audio-event log go to `_demo`; encode/inspect the MP4 before replacing the existing recording.

Save current screenshots for desktop ready/gameplay/results and XR pickup/target hit/results. Keep old screenshots labelled as previous version until replacements are verified. Inspect for clipped text, hidden buttons and controller/board overlap.

## Current result

Unity tests ran: 11 EditMode and 10 PlayMode passed. XR selection was programmatic; manual simulator grip and firing remain unverified. Current Windows build succeeded with 0 errors and 1 warning. Current desktop screenshots passed the capture walkthrough; manual simulator checks and its video remain pending.



