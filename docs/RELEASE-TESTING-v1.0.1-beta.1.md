# v1.0.1-beta.1 release-candidate verification

Internal build: **4.1.2**

Candidate DLL SHA-256:

```text
A67DF8D9AB82EF7225D23D2836D0AE4FE89B84C3B746F6D39A16BAC4002D5DB5
```

## Completed

- Focused C# regression tests: passed.
- Mock backend owned shutdown and same-port restart: passed.
- Default .NET Framework 4.7.2 release build: passed with no warnings.
- BepInEx 5 reference build: passed with no warnings.
- Default and BepInEx 5 reference builds: byte-identical shared DLL.
- Final 4.1.2 BepInEx 6 gameplay testing: passed.

## Compatibility note

- BepInEx 5 has build/reference compatibility for this release; it has
  not received a new gameplay pass. Do not describe 4.1.2 as newly
  gameplay-tested on BepInEx 5 unless that check is completed.

Pocket is not part of the candidate or public package.
