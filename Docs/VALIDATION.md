# Validation status

1 November 2026.

Unity EditMode 11/11 and PlayMode 10/10 passed. The XR test selects the pistol programmatically after simulated controller activation; manual grip/trigger operation has not yet been verified. The Windows build succeeded with 0 errors and 1 warning. Current desktop start/gameplay/results and XR controllers/grab/results screenshots were refreshed on 1 November 2026 using the current scene. Both capture walkthroughs passed; XR selection is programmatic and the timer is advanced to show results. The owner-provided simulator/desktop recording was included on 2 November; sampled video frames are not a complete manual acceptance test. Physical headset use is unverified.

The earlier XR failure was caused by Unity Editor test input routing. The test harness routes simulated input to the Game view; the right-controller interactor then activates and programmatic XR selection starts the round. This does not establish manual grip or firing usability. Test XML is in Docs/Validation; build details are in Logs/Release-Build.log.

A previous installer was silently installed to a project-local folder. Full installed gameplay and uninstall remain unverified.



## Portable handoff - 2 November 2026

The submission now includes the complete Windows player, a relative-path PLAY.cmd launcher, installer, source, PDF, screenshots and the owner-provided MP4. The combined ZIP keeps both Unity roots separate under Task1 and Task2 and embeds all content directly, without nested task ZIPs.

A fresh Task 2 ZIP import was attempted from a separate directory without its previous Library cache. Unity exited with code 198: no valid local Editor license. This prevented fresh Editor import/tests/build verification on this date; it is not evidence that the recipient's reported path error was caused by licensing. Previous test results above retain their original dates.

The portable Windows player is the previously built binary; no gameplay C# was changed in this handoff. Tools/validate_source.py now uses the installed Unity Editor instead of a hard-coded cached compiler response-file directory. For Task 2, an unresolved optional default sprite reference in TMP Settings was cleared (it previously resolved to null). The recipient's exact error was not available, so its original cause is not confirmed.
