# eSpeak NG / phonemizer redistribution record

This project bundles the following separately licensed third-party components
for Kokoro's English out-of-vocabulary fallback. They are not covered or
relicensed by the Obenseuer Dialogue TTS MIT licence.

## Components

- **eSpeak NG 1.52.0**, Windows x86-64 shared library and data, reported at
  runtime by `espeak_Info()` as `1.52.0`.
- **espeakng-loader 0.2.4**, Windows x86-64 package.
- **phonemizer-fork 3.3.2**, Python package.

The release-preparation baseline uses these exact versions without modifying
their source, binary, dictionary or data files. Public release assets must
preserve their package metadata, licence texts and corresponding-source links.

## Licence and copyright

- eSpeak NG is distributed under GNU GPL version 3, with separately licensed
  upstream files identified in its source tree. It is based on the eSpeak
  engine created by Jonathan Duddington and maintained by eSpeak NG
  contributors.
- phonemizer-fork is GNU GPL version 3 or later, copyright 2015-2021 Mathieu
  Bernard.
- Full GPL text is included at
  `licenses/eSpeak-NG-1.52.0-GPL-3.0.txt` and
  `licenses/Phonemizer-Fork-3.3.2-GPL-3.0-or-later.txt`.
- The eSpeak NG tag's Apache-2.0, BSD-2-Clause, and Unicode Data licence
  texts/notices are also included under `licenses/eSpeak-NG-1.52.0-*`.
- phonemizer's original licence file and package metadata remain in
  `ObenseuerTTS/runtime/Lib/site-packages/phonemizer_fork-3.3.2.dist-info`.

## Exact corresponding-source locations

eSpeak NG 1.52.0:

- Release/tag: https://github.com/espeak-ng/espeak-ng/releases/tag/1.52.0
- Tagged source: https://github.com/espeak-ng/espeak-ng/tree/1.52.0
- Source archive: https://github.com/espeak-ng/espeak-ng/archive/refs/tags/1.52.0.tar.gz
- Release commit shown upstream: `4870adf`

phonemizer-fork 3.3.2:

- Release and source distribution: https://pypi.org/project/phonemizer-fork/3.3.2/
- Source archive SHA-256 recorded by PyPI:
  `10e16e827d0443b087062e21b55e805c00989cf1343b2e81e734cae5f6c0cf69`
- Upstream repository: https://github.com/bootphon/phonemizer

espeakng-loader 0.2.4:

- Exact Windows x86-64 release: https://pypi.org/project/espeakng-loader/0.2.4/
- Wheel SHA-256 recorded by PyPI:
  `41f1e08ac9deda2efd1ea9de0b81dab9f5ae3c4b24284f76533d0a7b1dd7abd7`
- The loader's Python source (`espeakng_loader/__init__.py`) is included in the
  installed package.

No written source offer is substituted for these direct, version-pinned source
locations. Redistributors should preserve this record, the licence texts, and
the component package metadata.
