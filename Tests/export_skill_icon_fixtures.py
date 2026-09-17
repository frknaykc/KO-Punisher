"""Export shipped Archer templates for platform-independent matcher regression tests."""
import base64
import json
import re
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
catalog = (root / 'KO-Punisher/JobSkillCatalog.cs').read_text()
icons = {}
for skill_id, jobs, filename in re.findall(r'new\("([^"]+)", "[^"]+", \[([^\]]+)\], "[^"]+", "([^"]+)"', catalog):
    if 'ClassType.Archer' not in jobs:
        continue
    with Image.open(root / 'KO-Punisher/images' / filename) as image:
        rgb = image.convert('RGB').resize((24, 24), Image.Resampling.BILINEAR)
        icons[skill_id] = base64.b64encode(rgb.tobytes()).decode('ascii')
path = root / 'Tests/Fixtures/SkillIcons/archer.json'
path.parent.mkdir(parents=True, exist_ok=True)
path.write_text(json.dumps(icons, indent=2) + '\n')
print(f'{len(icons)} shipped Archer icon fixtures exported')
