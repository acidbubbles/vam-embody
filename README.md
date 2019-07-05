# Virt-A-Mate Improved POV Plugin

Improved POV handling so that possession actually feels right.

## Why

Currently, the possession mode (point-of-view) in Virt-A-Mate places the camera in _front_ of the head. That feels like you're floating in front of someone's body.

We want the camera to be exactly where the eyes are, with the same eye distance. The simple solution is to simply place the camera in the "right spot", and increase the minimum clipping distance, but that creates other problems, such as visual artefacts when moving the head, and close objects clipping. This plugin solves all of this:

- No visual artefacts when moving the head, and no clipping
- You can see your hair in front of your eyes as you move your head around
- Possession with scaled models won't reveal the "possessor" white spheres
- When you look down to look at your body when lying down or sitting on a chair, your chest won't be abnormally close
- If you touch your lips or your face, you can have them match so that physical contact matches what you see
- Settings will be kept as part of your scenes, so you don't have to set them every time

## How to use

1. Download the `ImprovedPoV.cs` file from the [latest release's Assets](https://github.com/acidbubbles/vam-improved-pov/releases), and save it somewhere (e.g. `(VaM Install)/Saves/Scripts/Acidbubbles/ImprovedPoV.cs`)
2. Select the Person you want to possess, and in Plugins, add the `ImprovedPoV.cs` file
3. Possess the model
4. Optionally Configure the plugin (camera depth, show or hide hair, etc.) to adjust to your taste
5. Adjust the world scale until your hand in relationship to your body matches. Close your eyes, put your index finger horizontally at the level of your mouth, and look at where it shows up. Increase or decrease the scale so that your "proportions" fit! Or you can activate the auto-scale option.

## How does it work

- The possessor model is moved so the head is offset in relationship to the camera (gives the same result as moving the camera itself)
- The face shaders are replaced with one that supports transparency
- The script attaches to `onPreRender` and `onPostRender` events to dynamically change the alpha channel of all materials
- For Sim V2 hair, the hair width in the shader is set to zero, which achieves the same result as hiding it. Older hairs simply reduces the alpha adjust)

## Known issues

- This script won't work with Sim and Sim2 hair.

## Credits

[ShortRecognition](https://www.reddit.com/user/ShortRecognition/) for the [original HeadPossessDepthFix](https://www.reddit.com/r/VAMscenes/comments/9z9b71/script_headpossessdepthfix/) from which this plugin took heavy inspiration.

Thanks to Marko, VeeRifter, VAMDeluxe, LFE, Spacedog and MeshedVR for your help on Discord.

And, obviously, MeshedVR for [Virt-A-Mate](https://www.patreon.com/meshedvr/posts), which is a technological wonder worth exploring.

## License

[MIT](LICENSE.md)
