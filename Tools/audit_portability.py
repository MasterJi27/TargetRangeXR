"""Read-only asset, dependency and path audit. No Unity runtime pass is implied."""
from pathlib import Path
import argparse
import json
import re

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('project', nargs='?', type=Path, default=Path(__file__).resolve().parents[1])
args = parser.parse_args()
root = args.project.resolve()
issues = []
asset_guids = {}
package_guids = set()
for p in (root / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: ([a-f0-9]{32})', p.read_text(errors='ignore'), re.M)
    if match:
        if match[1] in asset_guids:
            issues.append(f'Duplicate GUID: {p.relative_to(root)}')
        asset_guids[match[1]] = p
cache = root / 'Library/PackageCache'
if cache.is_dir():
    for p in cache.rglob('*.meta'):
        match = re.search(r'^guid: ([a-f0-9]{32})', p.read_text(errors='ignore'), re.M)
        if match:
            package_guids.add(match[1])
external = set()
for p in (root / 'Assets').rglob('*'):
    if not p.is_file() or p.suffix == '.meta' or p.name.startswith('.'):
        continue
    if not Path(str(p) + '.meta').exists():
        issues.append(f'Missing meta: {p.relative_to(root)}')
    if p.suffix in {'.unity', '.prefab', '.asset', '.mat', '.preset', '.controller'}:
        for guid in set(re.findall(r'guid: ([a-f0-9]{32})', p.read_text(errors='ignore'))):
            if guid.startswith('0000000000000000') or guid in asset_guids:
                continue
            external.add(guid)
            if cache.is_dir() and guid not in package_guids:
                issues.append(f'Unresolved asset GUID {guid}: {p.relative_to(root)}')
manifest = json.loads((root / 'Packages/manifest.json').read_text(encoding='utf-8-sig'))
for package, version in manifest['dependencies'].items():
    if version.startswith('file:') or re.match(r'^[A-Za-z]:', version):
        issues.append(f'Local package dependency: {package} -> {version}')
settings = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text()
for scene in re.findall(r'^\s+path: (.+)$', settings, re.M):
    if not (root / scene).is_file():
        issues.append(f'Missing build scene: {scene}')
result = {'project': root.name, 'asset_guid_count': len(asset_guids),
          'package_guids_checked': bool(package_guids), 'external_guid_count': len(external),
          'issues': sorted(set(issues)),
          'scope': 'Static asset/path audit only. Package GUID resolution requires restored Library/PackageCache.'}
print(json.dumps(result, indent=2))
raise SystemExit(bool(issues))
