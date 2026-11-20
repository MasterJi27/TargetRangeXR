from pathlib import Path
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, PageBreak, KeepTogether
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.lib import colors
import sys

styles=getSampleStyleSheet()
styles['BodyText'].fontSize=9.5
styles['BodyText'].leading=12.5
styles['Heading1'].fontSize=15
styles['Heading1'].leading=18
styles['Heading1'].spaceBefore=8
styles['Heading1'].spaceAfter=5
styles['Heading1'].keepWithNext=True
styles['BodyText'].spaceAfter=6
styles['Heading1'].textColor=colors.HexColor('#173b63')
def footer(canvas, doc):
    canvas.setFont('Helvetica',8)
    canvas.setFillColor(colors.HexColor('#556477'))
    canvas.drawRightString(190*mm,10*mm,f'Page {doc.page}')

root=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
n=2 if (root/'Assets/Scripts/Core/XRDesktopBridge.cs').exists() else 1
if True:
    title='Chroma Strike' if n==1 else 'Target Range XR'
    story=[Paragraph(f'Task {n}: {title}',styles['Title'])]
    def section(name,body):
        story.extend([Paragraph(name,styles['Heading1']),Paragraph(body,styles['BodyText'])])
    if n==1:
        section('Project summary and gameplay','A first-person sphere game. Start a 120-second round with 15 red, 10 green and 5 blue spheres. Click spheres to destroy them. Each set of 10 destroyed spheres produces a replacement batch with the same color distribution. A random sphere also spawns every five seconds. The UI shows live color counts and remaining time; the end screen shows final counts and restart.')
        section('Controls and instructions','Open Assets/Scenes/Game.unity in Unity 6000.6.7f1 and press Play, or run the Windows installer. Select Start Game. Move with WASD, aim with the mouse and click to destroy a sphere. Shift sprints; Escape releases the cursor. Select Restart after the round.')
        section('Packages used','Input System 1.20.1; Universal Render Pipeline 17.6.0; uGUI / TextMesh Pro 2.0.0; Unity Test Framework 1.4.6. Package manifest and lockfile are in Packages/.')
        section('Key scripts and classes','GameManager coordinates game states, timer, UI and spawning. SpawnerManager handles initial, timed and replacement spawns; SpawnPointFinder checks clear positions. BatchTracker and ColorTally track destroyed colors. FirstPersonController and PlayerInputReader handle movement and look; ClickHandler casts the center-screen ray. UIManager updates menus, counters and results. GameConfig holds configurable gameplay values.')
        section('Known limitations','The installer was compiled and installed into a clean folder. A complete installed-game playthrough and uninstall check have not been recorded. Unity 6000.6.7f1 was used. Dense spawn requests can wait until a non-overlapping position becomes available.')
        shots=[('01_start_screen.png','Start screen'),('02_gameplay_start.png','Gameplay'),('05_end_screen.png','End screen')]
    else:
        section('Game description','A 60-second shooting range with an XR grabbable pistol, physical bullets, stationary and moving targets, score and timer. Hits knock targets down and update the score. At time-up the game freezes and shows results with restart and exit options. Targets later respawn during play.')
        section('XR Device Simulator setup and controls','Open Assets/Scenes/Range.unity in Unity 6000.6.7f1 and press Play. Without a headset, press F2 or choose XR Simulator. Click the Game view; F1 displays simulator help. Hold Space to manipulate the right controller, point at the gun first, then press and hold G to grip; release G to drop it. Left mouse activates the trigger. Hold right mouse to manipulate the headset and use WASD to translate the selected device. Use a controller ray and trigger for the results buttons. F2 returns to desktop mode and resets the round.')
        section('Desktop controls','Press E or Enter to pick up the gun and start. Use mouse or arrow keys to aim, WASD to move, and left mouse or Space to fire. Escape releases or recaptures the cursor; the timer continues. R or Enter restarts from results; Q exits.')
        section('Unity version and packages','Unity 6000.6.7f1. XR Interaction Toolkit 3.7.0 with Starter Assets and XR Device Simulator; Input System 1.20.1; OpenXR 1.19.0; XR Plugin Management 4.6.1; Universal Render Pipeline 17.6.0; uGUI / TextMesh Pro 2.0.0; Unity Test Framework 1.4.6. Unity 2022.3 LTS was recommended in the assignment and has not been tested with this project.')
        section('Known limitations and bugs','The 1 November automated tests passed with programmatic XR selection; full manual grip/trigger and real-headset checks remain unverified. Fresh Editor validation on 2 November was blocked by a missing local Unity license. The included 88-second owner recording shows XR and desktop gameplay; complete expiry/replay coverage is not certified. Screenshots are 1 November scripted captures with timer advancement. The previous Windows build had one warning; complete installed gameplay and uninstall checks remain pending.')
        shots=[('06_desktop_start.png','Current desktop start screen'),('07_desktop_gameplay.png','Current desktop gameplay and score'),('08_desktop_results.png','Current desktop results and replay'),('09_xr_rig_and_controllers.png','Current XR mode - simulated controllers'),('10_xr_pistol_grabbed.png','Current XR mode - pistol grabbed by controller'),('11_xr_results.png','Current XR mode - world-space results')]
    if n == 2: section('Windows installation','Run Installer/TargetRangeXR-Setup-1.0.0.exe and follow setup. Launch Target Range XR from the Start Menu. Documentation and screenshots are installed with the player; the MP4 remains in the submission ZIP under Docs/.')
    section('Opening the submission','Extract the entire ZIP to a short local folder, for example C:/UnityAssignment. Run PLAY.cmd in the task folder to play without Unity, or use Installer/. Keep the whole Builds/Windows folder intact. To edit, add the task folder containing Assets, Packages and ProjectSettings in Unity Hub, using Unity 6000.6.7f1. The first import requires an activated Editor license and package internet access. Open the supplied scene; scene regeneration is not required.')
    section('Project folder structure','Assets/ contains scenes, scripts, settings and assets; Packages/ contains package definitions; ProjectSettings/ contains Unity settings; Docs/ contains documentation and screenshots; Installer/ contains the Windows setup executable and recipe; Tools/ contains repeatable test, documentation and packaging scripts. Builds/Windows contains the complete portable player; PLAY.cmd launches it using relative paths. Docs/ also includes the owner-provided gameplay MP4. README.md contains the full setup and controls. Library/, Temp/ and Logs/ are generated and excluded from the ZIP.')
    story.append(PageBreak()); story.append(Paragraph('Gameplay and UI screenshots',styles['Heading1']))
    for filename,caption in shots:
        im=Image(str(root/'Docs'/'Screenshots'/filename)); ratio=im.imageHeight/im.imageWidth; im.drawWidth=160*mm; im.drawHeight=160*mm*ratio; story.append(KeepTogether([Paragraph(caption,styles['Heading2']),im,Spacer(1,8*mm)]))
    pdf=root/'Docs'/'Project_Structure_and_Submission_Guide.pdf'
    SimpleDocTemplate(str(pdf),pagesize=(210*mm,297*mm),leftMargin=20*mm,rightMargin=20*mm,topMargin=18*mm,bottomMargin=18*mm).build(story, onFirstPage=footer, onLaterPages=footer)
    print(pdf)





