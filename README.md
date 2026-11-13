<h1 align="center">
    <img src="./dist/branding/bloomstrap-256.png" height="96" alt="Bloomstrap logo"/><br/>
    Bloomstrap
</h1>

<p align="center">
    A Roblox bootstrapper for Linux that launches the game through <a href="https://sober.vinegarhq.org">Sober</a>.
</p>

## What it does

- Launches Sober (`org.vinegarhq.Sober`) and keeps it up to date, whether it was installed system-wide or per user.
- Discord rich presence, server location and uptime, game history with one-click rejoin, and a region selector, all read from Sober's logs.
- Settings for Sober, Roblox's global basic settings, and FastFlags, plus mods and custom bootstrapper themes.
- A frosted "bloom" glass interface in Catppuccin Mocha colours, made to sit on a blurring Wayland compositor such as niri.

### Launch stability on Linux

- Sober is started through `systemd-run --user`, so it gets fresh resource limits. Launching from a browser such as Zen or Firefox would otherwise pass on a realtime CPU limit that kills Sober a few seconds into a game.
- Sober's output goes to `sober-output.log` in the log folder, not to a pipe that can close underneath it.
- On NVIDIA, the shader cache is kept instead of being trimmed, so shaders aren't recompiled mid-game.
- Sober starts windowed, so a fullscreen compositor doesn't lock it to the monitor's refresh rate.

## Building

You need the .NET 10 SDK and stable Rust (`cargo`).

```sh
dotnet publish Froststrap/Froststrap.csproj -c Release -r linux-x64 -o publish/linux-x64
```

The result is a single native (AOT) binary, `publish/linux-x64/froststrap`, with its native libraries in `libs/`. Copy the folder somewhere permanent, for example `~/.local/lib/bloomstrap`, and launch `froststrap --settings` once: it registers itself as the `roblox://` link handler.

A Debug build (`dotnet build`) works for development, but it is much slower and uses more memory.

### niri

Bloomstrap's window uses the class `bloomstrap`. `dist/linux/niri-bloomstrap.kdl` has window rules for it and for Sober.

The native Wayland backend can be tried with `BLOOMSTRAP_FORCE_WAYLAND=1`. It runs without problems, but Avalonia can't set an app-id under Wayland yet, so compositor rules that match `bloomstrap` won't apply to it.

## Credits

Bloomstrap is maintained by Neo Berent.

It is based on [Froststrap](https://github.com/Froststrap/Froststrap), which in turn comes from Fishstrap and Bloxstrap. Their copyright notices are kept in the source files.

## License

[MPL-2.0](LICENSE). Bloomstrap is not affiliated with Roblox Corporation or Vinegar HQ.
