# Virt-A-Mate Improved POV Plugin

Improved POV handling so that possession actually feels right.

## Status

Still under development. A short list of what's done or not:

- [x] Move the camera at the right position
- [ ] Hide the face mesh
- [ ] Hide the eyeballs (or move them out of view)
- [ ] Hide the mouth and lips (not really useful, but it's less scary when seen in a mirror)
- [ ] Hide the possessor geometry (MonitorRig or CenterEye)
- [ ] If using materials to hide stuff, make sure we only hide our face, not other models'
- [ ] Only hide the face mesh when in possession
- [ ] Import a nose (without collision) and make it follow the camera
- [ ] Render the face material in mirrors (the monitor rig does the reverse thing right now)
- [ ] Sensible defaults when importing
- [ ] Test for Oculus, Vive and more (see the Leap Possess plugin for ovr condition)
- [ ] Test loading scenes with it, with and without possession, adding and removing
- [ ] Add options for what to hide, as well as a few tips

## Credits

[ShortRecognition](https://www.reddit.com/user/ShortRecognition/) for the [original HeadPossessDepthFix](https://www.reddit.com/r/VAMscenes/comments/9z9b71/script_headpossessdepthfix/) from which this plugin took heavy inspiration.
