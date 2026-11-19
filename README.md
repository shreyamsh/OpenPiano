# Open Piano

A free, open-source piano app for Windows 10/11, built with WinUI 3 and C#.

## Status
Early prototype: a two-octave touch/mouse keyboard with a placeholder synth sound.

## Build
Requirements: Windows 10 (1809+) or 11, Visual Studio 2022 with the ".NET desktop development" and "Windows App SDK C# templates" components, .NET 8 SDK.

```
dotnet restore OpenPiano.sln -r win-x64
dotnet build OpenPiano.sln -c Release -p:Platform=x64
```
Or open `OpenPiano.sln` in Visual Studio and press F5.

## Roadmap
- [ ] Real piano samples (freely licensed, license noted in `ASSETS.md`)
- [ ] Sustain pedal, velocity, full 88 keys with scrolling
- [ ] Computer-keyboard and MIDI input
- [ ] Microsoft Store release

## License
Code: MIT (see `LICENSE`). Audio samples and artwork carry their own licenses, listed in `ASSETS.md`.
