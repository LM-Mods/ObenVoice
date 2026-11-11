# eSpeak NG / phonemizer redistribution record

This project bundles the following separately licensed third-party components
for Kokoro's English out-of-vocabulary fallback. They are not covered or
relicensed by the Obenseuer Dialogue TTS MIT licence.

## Components

- **eSpeak NG 1.52.0**, Windows x86-64 shared library and data, reported at
  runtime by `espeak_Info()` as `1.52.0`.
- **espeakng-loader 0.2.4**, Windows x86-64 package.
- **phonemizer-fork 3.3.2**, Python package.

The published v1.0.2-beta.1 Core uses these exact versions without modifying
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
  `ObenVoice/runtime/Lib/site-packages/phonemizer_fork-3.3.2.dist-info`.
  Existing legacy installations may retain the equivalent `ObenseuerTTS`
  runtime path.

## Exact corresponding-source locations

eSpeak NG 1.52.0:

- Release/tag: https://github.com/espeak-ng/espeak-ng/releases/tag/1.52.0
- Tagged source:
  https://github.com/espeak-ng/espeak-ng/tree/4870adfa25b1a32b4361592f1be8a40337c58d6c
- Source archive: https://github.com/espeak-ng/espeak-ng/archive/refs/tags/1.52.0.tar.gz
- Release commit: `4870adfa25b1a32b4361592f1be8a40337c58d6c`

phonemizer-fork 3.3.2:

- Release and source distribution: https://pypi.org/project/phonemizer-fork/3.3.2/
- Exact source archive:
  https://files.pythonhosted.org/packages/42/fa/9294d2f11890ca49d0bdac7a4da60cbe5686629bfd4987cae0ad75e051cc/phonemizer_fork-3.3.2.tar.gz
- Source archive SHA-256 recorded by PyPI:
  `10e16e827d0443b087062e21b55e805c00989cf1343b2e81e734cae5f6c0cf69`
- Upstream repository: https://github.com/bootphon/phonemizer

espeakng-loader 0.2.4:

- Exact Windows x86-64 release: https://pypi.org/project/espeakng-loader/0.2.4/
- Wheel SHA-256 recorded by PyPI:
  `41f1e08ac9deda2efd1ea9de0b81dab9f5ae3c4b24284f76533d0a7b1dd7abd7`
- The loader's Python source (`espeakng_loader/__init__.py`) is included in the
  installed package.
- PyPI publishes wheels but no source distribution for 0.2.4. The exact
  upstream version-bump commit is
  `146599e29be31bf17d99f0bcb7dbb2f92aef3d95`:
  https://github.com/thewh1teagle/espeakng-loader/tree/146599e29be31bf17d99f0bcb7dbb2f92aef3d95
- Source archive for that commit:
  https://github.com/thewh1teagle/espeakng-loader/archive/146599e29be31bf17d99f0bcb7dbb2f92aef3d95.tar.gz

## Other LGPL components

- **num2words 0.5.14** is installed as Python source and retains its LGPL-2.1
  text. Its exact source distribution is listed at
  https://pypi.org/project/num2words/0.5.14/ with SHA-256
  `b066ec18e56b6616a3b38086b5747daafbaa8868b226a36127e0451c0cf379c6`.
- **libsndfile 1.2.2** is carried as the dynamically loaded Windows DLL used by
  SoundFile 0.14.0. The package retains its LGPL-2.1 text. Exact source and
  build instructions are available from
  https://github.com/libsndfile/libsndfile/releases/tag/1.2.2 and
  https://github.com/libsndfile/libsndfile/archive/refs/tags/1.2.2.tar.gz.

## Network-source arrangement

ObenVoice distributes these components through a network download. GPLv3
section 6(d) permits Corresponding Source on another server when equivalent
access is available and clear directions are kept next to the object-code
download. The distributor remains responsible for keeping those sources
available for as long as it distributes the object code. See
https://www.gnu.org/licenses/gpl-3.0.html#section6 and the GNU licensing FAQ:
https://www.gnu.org/licenses/gpl-faq.html#SourceAndBinaryOnDifferentSites.

The Core release description must link directly to this record. The upstream
locations above were accessible and version-specific on 11 November 2026.
They must be checked periodically; an upstream link disappearing does not end
the distributor's source-availability responsibility. Mirroring the exact
source archives beside the binary is a more robust future option, but was not
part of the frozen v1.0.2-beta.1 asset set.

No written source offer is substituted for these direct, version-pinned source
locations. Redistributors should preserve this record, the licence texts and
component package metadata. This record documents the evidence reviewed; it is
not a formal legal certification.
