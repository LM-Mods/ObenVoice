# Final release-candidate gameplay record

> **Historical v1.0.0-beta.1 evidence.** The DLL hash and results below apply
> only to that release. For v1.0.2-beta.1, see
> [the current release asset record](RELEASE-ASSETS-v1.0.2-beta.1.md).

The path-sanitised v1.0.0-beta.1 DLL was tested by the user on 8 November 2026.

| Check | Result |
|---|---|
| BepInEx 5 | PASS |
| BepInEx 6 | PASS |
| NPC dialogue | PASS |
| Player dialogue | PASS |
| F8/F9 functionality | PASS |
| Steam shutdown on BepInEx 5 | PASS |
| Steam shutdown on BepInEx 6 | PASS |

Tested DLL SHA-256:

```text
59A0B0F5BE6BCD9C55029CCE84E279038B1DFFAE0C950B161D38AA781C5AF265
```

The same DLL previously matched the frozen 4.1.1 implementation across 447
assembly, reference, type, field and method/IL comparison records.

Very short utterances such as `Hmh` and `Ahhh` can be imperfect or skipped.
This is a known nonblocking pronunciation limitation deferred to a future
update.
