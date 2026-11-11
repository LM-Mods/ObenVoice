# Minimal engine add-on example

Copy this folder to `ObenVoice\engine-addons\sample` and keep the folder ID and manifest ID identical. The older `ObenseuerTTS\engine-addons` path remains readable for existing installations.

`engine.py` is deliberately silent and contains no model. Replace its `speak` implementation with a locally packaged engine while preserving `stop` and `close`. The factory receives the language code, resolved add-on folder and resolved ObenVoice data folder.

Add-on code runs locally with the user's privileges, so packages must document their source and licences.
