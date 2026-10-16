# Return to Sender

A small Dalamud plugin box that shows who has recently emoted at you, and lets you send it back in one click.

![icon](images/icon.png)

## Features

- **Sees every emote aimed at you**, including motion-only emotes and ones whose chat messages are hidden. It reads the emote itself, not the chat log.
- **Unreturned emotes are highlighted** in gold, and the box title shows how many are waiting.
- **One-click Return**: targets that person and uses the same emote back (or a fixed emote you choose). Optionally switches back to your previous target afterwards.
- **Choose what to track**: all emotes, **dotes only**, or **your own list** (type a name or command like `hug` or `/pet` and pick from suggestions). Emotes outside the filter aren't recorded.
- Repeats from the same person collapse into one line (`Dote ×3`).
- Click a name to target them. Right-click for *Mark as returned* / *Remove*.
- The list lives in memory only and is cleared when you log out.

Returning only ever happens when you click the button. Nothing is sent automatically.

## Commands

- `/rts` show or hide the box
- `/rts config` settings
- `/rts clear` empty the list
- `/rts filter all|dotes|custom` choose which emotes to track (handy in macros)

## Install

In game: `/xlsettings` → **Experimental** → **Custom Plugin Repositories**, add:

```
https://raw.githubusercontent.com/wilklersticks/DalamudPlugins/main/pluginmaster.json
```

Then find **Return to Sender** in `/xlplugins`.

## Releases

Builds and releases are automatic on every commit. Start a commit title with `feat:` for a minor version bump or `major:` for a major one; anything else is a patch.
