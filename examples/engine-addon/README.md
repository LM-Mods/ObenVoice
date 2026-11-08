# Minimal engine add-on example

Copy this folder to `ObenseuerTTS\engine-addons\sample` and keep the folder ID and manifest ID identical.

`engine.py` is deliberately silent and contains no model. Replace its `speak` implementation with a locally packaged engine while preserving `stop` and `close`. The factory receives the language code, resolved add-on folder and resolved `ObenseuerTTS` folder.

Add-on code runs locally with the user's privileges, so packages must document their source and licences.
