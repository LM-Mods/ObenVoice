# Packaging workspace contract

Release packages are assembled outside Git from protected, verified inputs. Do not copy bundled runtimes or models into this repository.

Expected install roots:

```text
Core/BepInEx/plugins/ObenseuerDialogueTTS/
Core/ObenseuerTTS/
British-Voices/ObenseuerTTS/voice-packs/kokoro/british-test/
```

Every final asset requires a clean staging directory, inventory, licence/privacy review, archive checksum and installation/rollback test before publication.
