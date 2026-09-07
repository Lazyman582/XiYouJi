import os, sys

asset_dir = r'D:\Github\XiYouJi\Assets\GameContent\01_Scenes\HDRP-FluidProject-main\Assets\SampleSceneAssets\Scripts\对话\文本'

dialogue = [
    ('白骨精', '小的们！听说东土大唐派往西天取经的和尚，乃是十世修炼的童男，吃他一块肉可以长生不老。你们巡山时要多多留意。', False),
    ('黑狐精', '大王放心！', False),
    ('白骨精', '听说他的徒弟孙悟空很厉害，可要多加小心才是！', False),
    ('黑狐精', '大王放心！', True),
]

lines = []
lines.append('%YAML 1.1')
lines.append('%TAG !u! tag:unity3d.com,2011:')
lines.append('--- !u!114 &11400000')
lines.append('MonoBehaviour:')
lines.append('  m_ObjectHideFlags: 0')
lines.append('  m_CorrespondingSourceObject: {fileID: 0}')
lines.append('  m_PrefabInstance: {fileID: 0}')
lines.append('  m_PrefabAsset: {fileID: 0}')
lines.append('  m_GameObject: {fileID: 0}')
lines.append('  m_Enabled: 1')
lines.append('  m_EditorHideFlags: 0')
lines.append('  m_Script: {fileID: 11500000, guid: 934c1ef0cd8351447bfb2eeb8c16fb23, type: 3}')
lines.append('  m_Name: ' + chr(34) + '白骨精（第一幕）' + chr(34))
lines.append('  m_EditorClassIdentifier:')
lines.append('  dialoguePieces:')

for speaker, content, has_end in dialogue:
    lines.append('  - speaker: ' + chr(34) + speaker + chr(34))
    lines.append('    content: ' + chr(34) + content + chr(34))
    lines.append('    portrait: {fileID: 0}')
    lines.append('    nextIndex: 0')
    if has_end:
        lines.append('    choices:')
        lines.append('    - choiceText: ' + chr(34) + '结束对话' + chr(34))
        lines.append('      targetIndex: -1')
        lines.append('      broadcastEvent: 0')
    else:
        lines.append('    choices: []')
    lines.append('    broadcastOnShow: 0')
    lines.append('    broadcastEventKey: ')

lines.append('  title: ')
lines.append('  author: ')

asset_path = os.path.join(asset_dir, '白骨精（第一幕）.asset')
with open(asset_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines) + '\n')
print('Created: ' + asset_path)

meta_lines = []
meta_lines.append('fileFormatVersion: 2')
meta_lines.append('guid: d70a53085b6941e681e24a8b7015695c')
meta_lines.append('NativeFormatImporter:')
meta_lines.append('  externalObjects: {}')
meta_lines.append('  mainObjectFileID: 11400000')
meta_lines.append('  userData: ')
meta_lines.append('  assetBundleName: ')
meta_lines.append('  assetBundleVariant: ')

meta_path = asset_path + '.meta'
with open(meta_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(meta_lines) + '\n')
print('Created: ' + meta_path)
