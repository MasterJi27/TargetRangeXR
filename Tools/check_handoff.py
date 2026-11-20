from pathlib import Path
import re,json
root=Path(__file__).resolve().parents[1];scene=root/'Assets/Scenes/Range.unity';s=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M)
assert len(ids)==len(set(ids)), 'Duplicate Unity scene IDs'
local=set(re.findall(r'\{fileID: (-?\d+)\}',s)) - {'0'}
assert local.issubset(set(ids)), 'Missing local references: '+str(local-set(ids))
assert '  m_Name: Desktop Controls\n' in s
assert '  mouseSensitivity: 0.09\n' in s
assert '  xrOrigin: {fileID: 2147000004}' in s
assert '  simulatorRoot: {fileID: 2147000005}' in s
meta={}
for p in (root/'Assets').rglob('*.meta'):
 match=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
 if match:meta[match.group(1)]=p
for id in ['2147000002','2147000003']:
 block=s.split('--- !u!114 &'+id+'\n')[1].split('--- !u!')[0]
 guid=re.search(r'm_Script: \{fileID: 11500000, guid: (\w+)',block).group(1)
 assert guid in meta
# External prefab source IDs in the two new stripped scene references must exist in actual prefabs.
for path,source in [('Assets/Samples/XR Interaction Toolkit/3.7.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab','1717954561962503726'),('Assets/Samples/XR Interaction Toolkit/3.7.0/XR Device Simulator/XR Device Simulator.prefab','6598815579406187037')]:
 assert '&'+source+'\n' in (root/path).read_text(encoding='utf-8-sig')
for project in [root]:
 for doc in [project/'README.md',*(project/'Docs').glob('*.md')]:
  for link in re.findall(r'\]\(([^)]+)\)',doc.read_text(encoding='utf-8-sig')):
   if '://' not in link: assert (doc.parent/link).exists(),f'Broken link: {doc}: {link}'
result={'scene_objects':len(ids),'local_scene_references':'all resolved','new_script_guids':'resolved','new_prefab_source_ids':'resolved','documentation_links':'all resolved','scope':'Static references only. See VALIDATION.md for separately recorded Unity tests/build and manual simulator checks.'}
(root/'Docs/Validation/static-checks.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))


