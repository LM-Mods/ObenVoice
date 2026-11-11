# Third-party notices

ObenVoice combines MIT-licensed original project code with separately licensed
components. The project MIT licence does not relicense libraries, models,
voices, game assets or the separately licensed character database.

## Core speech stack

| Component | Recorded version/revision | Recorded licence |
|---|---|---|
| Kokoro Python package | 0.9.4 | Apache-2.0 |
| Misaki G2P | 0.9.4 | Apache-2.0 |
| Kokoro-82M model | `f3ff3571791e39611d31c381e3a41a3af07b4987` | Apache-2.0 repository declaration |
| Bundled Kokoro voices | same pinned model revision | Apache-2.0 repository declaration |
| eSpeak NG | 1.52.0 | GPL-3.0 plus identified component licences |
| phonemizer-fork | 3.3.2 | GPL-3.0-or-later |
| espeakng-loader | 0.2.4 | separately packaged loader/source |
| Python embedded runtime | 3.12.10 | PSF licence |
| Loguru | 0.7.3 | MIT; exact upstream notice in [LOGURU-0.7.3-MIT.txt](docs/LOGURU-0.7.3-MIT.txt) |
| Hugging Face Tokenizers | 0.23.3 | Apache-2.0; treatment in [TOKENIZERS-0.23.3-NOTICE.md](docs/TOKENIZERS-0.23.3-NOTICE.md) |

The Core release also contains PyTorch, NumPy, Transformers, Hugging Face Hub,
spaCy, SoundDevice, SoundFile, libsndfile and transitive packages under their
own licences. The generated inventory is in
[RUNTIME-DEPENDENCIES.md](docs/RUNTIME-DEPENDENCIES.md). Release assets must
preserve package metadata, collected licence/NOTICE files and
[the GPL/LGPL source-access record](docs/ESPEAK-NG-REDISTRIBUTION.md).

## Game and modding ecosystem

Obenseuer, its characters and associated trademarks belong to their respective owners. ObenVoice is unofficial. Game and Unity assemblies are local build references and are not included in this source repository or intended release packages. BepInEx, Harmony, Obenseuer Modding Framework, Unity and Pixel Crushers Dialogue System remain under their respective terms.

`characters.json` is an offline identification database compiled from a local
Obenseuer 0.5.19 character-registry scan. It is separately distributed under
CC BY-SA 4.0 compilation terms, to the extent applicable, with provenance and
data boundaries in
[data/LICENSE.md](data/LICENSE.md).

## Optional packages

External voice packs are separate distributions and require package-specific
notices. The optional British voice pack's two unmodified files match Kokoro-82M revision
`f3ff3571791e39611d31c381e3a41a3af07b4987` and retain Apache-2.0 attribution.
Stalburg Voices Vol. 1 contains modified Kokoro-compatible tensors and ships
its Apache-2.0 text, NOTICE, generator source and per-file provenance record.

This repository contains no third-party model or voice weights.
