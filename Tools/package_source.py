"""Build verified source + portable player + installer + documentation/video ZIPs.

python Tools/package_source.py
python Tools/package_source.py --combined OTHER_PROJECT_ROOT
"""
from pathlib import Path
import argparse
import hashlib
import json
import zipfile

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def project_info(root):
    xr = (root / 'Assets/Scripts/Core/XRDesktopBridge.cs').is_file()
    return ('Task2', 'Target Range XR', 'TargetRangeXR', 'Range') if xr else ('Task1', 'Chroma Strike', 'ChromaStrike', 'Game')

def collect(root):
    label, title, stem, scene = project_info(root)
    required = ['PLAY.cmd', 'README.md', 'Packages/manifest.json', 'Packages/packages-lock.json',
                'ProjectSettings/ProjectVersion.txt', f'Assets/Scenes/{scene}.unity',
                f'Installer/{stem}-Setup-1.0.0.exe', f'Installer/{stem}.iss',
                'Docs/Project_Structure_and_Submission_Guide.pdf', f'Docs/{stem}_Gameplay.mp4',
                f'Builds/Windows/{title}.exe', 'Builds/Windows/UnityPlayer.dll',
                f'Builds/Windows/{title}_Data/globalgamemanagers',
                f'Builds/Windows/{title}_Data/Managed/Assembly-CSharp.dll']
    # Projects use asmdef assemblies rather than Assembly-CSharp.
    required[-1] = f'Builds/Windows/{title}_Data/Managed/{"TargetRange" if label == "Task2" else "ChromaStrike"}.Runtime.dll'
    for relative in required:
        if not (root / relative).is_file():
            raise FileNotFoundError(f'Missing required deliverable: {root / relative}')
    files = [root / 'PLAY.cmd', root / 'README.md', root / f'Installer/{stem}-Setup-1.0.0.exe', root / f'Installer/{stem}.iss']
    for top in ['Assets', 'Packages', 'ProjectSettings', 'Docs', 'Tools', 'Builds/Windows']:
        for path in (root / top).rglob('*'):
            if not path.is_file():
                continue
            rel = path.relative_to(root)
            if '__pycache__' in rel.parts or path.name.startswith('InitTestScene'):
                continue
            if any('BackUpThisFolder' in part or part == 'DoNotShip' for part in rel.parts):
                continue
            if top == 'Tools' and (len(rel.parts) != 2 or path.suffix not in {'.py', '.ps1'}):
                continue
            if 'Validation' in rel.parts and path.suffix in {'.dll', '.pdb', '.rsp', '.log'}:
                continue
            files.append(path)
    return label, files

def package(roots, destination):
    payload = {}
    guide = ['UNITY DEVELOPER ASSIGNMENT - RAGHAV KATHURIA', '',
             '1. Download the ZIP and Extract All to a short local folder, e.g. C:\\UnityAssignment.',
             '2. Do not run files from inside the ZIP or move only the EXE.',
             '3. Play the Windows games using the PLAY.cmd files below. Unity is NOT needed to play.',
             '4. To edit source: use Unity Hub > Add project from disk. Choose Task1 or Task2 itself,',
             '   the folder containing Assets, Packages and ProjectSettings. Do not choose Assets.',
             '   Use Unity 6000.6.7f1, an activated Editor license and internet for package restoration.',
             '   After import, open the scene listed below. No scene regeneration is needed.', '',
             'Windows x64 players and both MP4 recordings are included locally; no Drive/YouTube link is required.', '']
    for root in sorted(roots, key=lambda p: project_info(p)[0]):
        label, files = collect(root)
        _, title, stem, scene = project_info(root)
        guide += [f'{label} - {title}', f'  Play: {label}/PLAY.cmd',
                  f'  Install instead: {label}/Installer/{stem}-Setup-1.0.0.exe',
                  f'  Video: {label}/Docs/{stem}_Gameplay.mp4',
                  f'  PDF: {label}/Docs/Project_Structure_and_Submission_Guide.pdf',
                  f'  Unity scene: {label}/Assets/Scenes/{scene}.unity',
                  f'  Setup, controls and limitations: {label}/README.md', '']
        for path in files:
            name = label + '/' + path.relative_to(root).as_posix()
            if name in payload:
                raise ValueError('Duplicate archive entry: ' + name)
            if len(name) > 200:
                raise ValueError('Archive path too long: ' + name)
            payload[name] = path
    guide += ['If a path is missing: extract the entire ZIP again; keep the complete Builds/Windows folder.',
              'Player startup logs: %TEMP%/Task1-Player.log and %TEMP%/Task2-Player.log when launched via PLAY.cmd.',
              'For editor problems, check the version and the first Console error. See each Docs/VALIDATION.md.',
              'FILES_SHA256.json is an integrity inventory, not a file needed to play.']
    hashes = {name: digest(path) for name, path in sorted(payload.items())}
    destination.parent.mkdir(exist_ok=True)
    temporary = destination.with_suffix('.tmp')
    with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        archive.writestr('START_HERE.txt', '\n'.join(guide) + '\n')
        archive.writestr('FILES_SHA256.json', json.dumps(hashes, indent=2))
        for name, path in sorted(payload.items()):
            archive.write(path, name)
    with zipfile.ZipFile(temporary) as archive:
        assert archive.testzip() is None, 'ZIP CRC failure'
        assert len(archive.namelist()) == len(set(archive.namelist()))
        for name, expected in hashes.items():
            with archive.open(name) as stream:
                assert hashlib.file_digest(stream, 'sha256').hexdigest() == expected, name
    temporary.replace(destination)
    result = {'archive': destination.name, 'files': len(payload) + 2, 'bytes': destination.stat().st_size,
              'sha256': digest(destination), 'longest_relative_path': max(map(len, payload)),
              'integrity': 'All CRCs and payload SHA-256 hashes passed. Runtime validation is documented separately.'}
    destination.with_suffix('.manifest.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('project', nargs='?', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--combined', type=Path, help='Other task project root to include')
    args = parser.parse_args()
    root = args.project.resolve()
    roots = [root, args.combined.resolve()] if args.combined else [root]
    name = 'Unity_Assignment_Raghav_Kathuria.zip' if args.combined else root.name + '_Submission.zip'
    package(roots, root / 'Submission' / name)
