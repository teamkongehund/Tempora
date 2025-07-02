# Tempora
An application designed to simplify and speed up the process of synchronizing a recorded piece of music to a digital metronome. 

This is achieved via a simple-to-use GUI that lets you manipulate timing points and visualize how the audio aligns with the current timing. The program does not alter the audio file like when you use quantization/warping. The program is mainly designed for rhythm games but may have other use cases such as music production. 

See [this video](https://www.youtube.com/watch?v=4_UN0Rxjv-w) for a presentation of Tempora, including a link to a tutorial in the video description.

## Supports
- osu! (.mp3 and .ogg)
- Beat Saber (.ogg)

## Community Discord
The project has an active discord community where you can share projects, get help with timing, be notified of new releases, etc.

[Join Discord server](https://discord.com/invite/A4XnXtz6Nb)

## Running Debug from source
- Download Godot (.Net verson): https://godotengine.org/download/windows/
- Clone the repository
- Open the Godot launcher and import project.godot from the chosen repository directory
- Open the project in Godot and run (F5)

You can also build and run the project from an IDE such as Visual Studio. This makes it possible to debug with breakpoints. For Visual Studio:
- In Launch Profile, locate the Godot executable (i.e. "Godot_v4.4-stable_mono_win64.exe")
- Use command line arguments "--path . --verbose"
- Use working directory "."

## Building from source
- Use Godot's Export functionality to build Tempora to your OS of choice

## Using Tempora
### Audio
- Open an .mp3 or .ogg file via `File -> Open...` or by dragging the file into Tempora from a File Explorer.
- Navigate the audio by scrolling and hovering over the audio with the mouse.
- Playback the audio by right-clicking the audio.
- Stop playback by pressing space.

### Timeline basics
The musical timeline is represented by measures and measure divisions (i.e. 4th or 8th notes). 
Each row of audio represents a measure. By default, the metronome plays a sound on every 4th note (beat).
Each measure division is represented by a vertical line overlayed the audio. By default, only the downbeat (first beat of a measure) is displayed.
- The desired measure division is controlled by the `Grid` bar in the top of the UI.
- The metronome can be set to follow the chosen measure division in the `Options` menu in the top of the UI.
- The `Rows`, `Offset` and `Overlap` bars in the top of the UI can be used to configure the timeline.

### Timing points basics
Tempora works by associating points of time in the music to the musical timeline via timing points.
- Add a timing point by clicking on a point of time in the music. The point will snap to the nearest measure division.
- Delete a timing point by double-clicking
- Click and drag a timing point to change its position on the musical timeline (music position)
- Control + click and drag a timing point to change its offset in the audio (offset in seconds)
- Control + scroll when hovering the audio to change the offset of the nearest timing point.

### Other features
- Select multiple timing points with Alt+Clicking and dragging
- Undo/Redo with Ctrl+Z/Y
- Change the playback rate in the top of the UI
- Modify the time signature in the left-hand side of each audio block
- Save and load project via the `File` menu, and with Ctrl+S
- Exporting to .osz (osu! map format) via the `File` menu

...and more!
