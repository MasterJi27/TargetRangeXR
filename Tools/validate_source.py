"""Import/compile using Unity, without machine-specific cached response-file paths.
Usage: python Tools/validate_source.py [project] [--unity PATH_TO_UNITY_EXE]
An activated Unity Editor is required. This does not run tests or build a player.
"""
from pathlib import Path
import argparse, os, subprocess, sys
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('project', nargs='?', type=Path, default=Path(__file__).resolve().parents[1])
parser.add_argument('--unity', type=Path)
args = parser.parse_args()
root = args.project.resolve()
version_file = root / 'ProjectSettings/ProjectVersion.txt'
if not version_file.is_file():
    parser.error('Select the folder containing Assets, Packages and ProjectSettings.')
version = version_file.read_text().splitlines()[0].split(':', 1)[1].strip()
editor = args.unity or (Path(os.environ['UNITY_EDITOR']) if os.environ.get('UNITY_EDITOR') else
    Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Unity/Hub/Editor' / version / 'Editor/Unity.exe')
if not editor.is_file():
    parser.error(f'Unity {version} was not found. Supply --unity with your installed Unity.exe path.')
log = root / 'Logs/Source-Import.log'
log.parent.mkdir(exist_ok=True)
result = subprocess.run([str(editor), '-batchmode', '-quit', '-projectPath', str(root), '-logFile', str(log)])
print(f'Unity exit code: {result.returncode}. Log: {log}')
if result.returncode:
    print('Import/compilation did not pass. Check license, package or compiler errors in the log.')
sys.exit(result.returncode)
