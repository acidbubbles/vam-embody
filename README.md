# Virt-A-Mate Improved POV Plugin

Improved POV handling so that possession actually feels right.

## Why

The issue is that currently, the possession mode in VaM places the camera in _front_ of the head. That feels like your body is weirdly offset behind you, or like you're floating in front of someone's body.

We want the camera to be exactly where the eyes are. The only solution right now involves increasing the minimum clipping distance, which creates it's own problems. This plugin solves all of this:

- See the model's hairs in front of your eyes as you move the head around
- Avoid seeing eyeballs and eyelashes when you move your head too fast
- Use possession with scaled models without seeing the "possessor" white spheres
- When you look down while your body is tilted backwards (e.g. sitting on a chair), your chest won't be clipped; same thing if you move your fingers near your face
- If you touch your lips or your face, you can have them match so that physical contact matches what you see
- Keep your settings as part of your scenes so you don't have to set them every time

## How to use

Add the `ImprovedPoV.cs` plugin to the Person you want to possess. Then, add the `ImprovedPoVMirrorReflection` on all mirrors you have in your scene.

You can use the recess and up/down controls to adjust for your specific morphs, and optionally use the "Materials Enabled" option if you don't have a mirror. It will be very slightly more performant.

To adjust the face, you should play with the World Scale. Close your eyes, put your index finger horizontally at the level of your mouth, and look at where it shows up. Increase or decrease the scale so that your "proportions" fit!

## Limitations

- Once applied, the mirror settings won't work anymore. The only way to change the mirror settings again will be to remove the mirror plugin, save, exit VaM, load your scene again and change the mirror settings. You can then add the plugin again. I did not figure out how `MirrorReflectionUI` was connected to `MirrorReflection`.

## How does it work

To allow for moving the camera further back into the eyes, we need to hide some face materials (the skin, eyeballs, eylashes, etc.) from showing up, as well as something called the "possessor", which is a green capsule with two white spheres. To achieve this, there are two strategies. Either use the `materialsEnabled` in the `DazSkinV2`, which is very easy to toggle but won't applied fast enough for use during the mirror render, or change the shaders in the `GPUmaterials` list for one that has transparency.

When possession is active, we can swap all shaders to a "close" one that supports transparency, and change the alpha of the material to make it fully transparent. We store the "previous" values in a temporary GameObject so they can be acquired by the mirrors later. Note that we broadcast `OnApplicationFocus` to force invalidating the skin shader cache, which is a workaround but it works. We also broadcast a message to all atoms so mirrors can also invalidate their cache.

Mirrors are somewhat complicated, because they actually have a script deeper in the GameObject structure, and they generate GameObjects at the scene's root level. To inject our own pre and post render code, we need to first find those gameobjects, and destroy them along with the original script. We can then inject ours, which has our additional behavior.

The mirror will find all references to all characters, and based on whether a materials cache exists for that character, build a list of these materials. At this point, the only thing it has to do is restore alpha before rendering, and set alpha to zero again after rendering.

## How it could be simpler

Well, it would be much simpler if it was built-in in VaM! With direct access to that code, this strategy could be achieved with a simple singleton who is aware of all characters in the scene, and which has to be hidden.

If there was a way to assign a culling layer to the skin material, and the VR camera was able to skip these materials, we could achieve a similar result.

## Credits

[ShortRecognition](https://www.reddit.com/user/ShortRecognition/) for the [original HeadPossessDepthFix](https://www.reddit.com/r/VAMscenes/comments/9z9b71/script_headpossessdepthfix/) from which this plugin took heavy inspiration.

Thanks for Marko, VAMDeluxe, LFE and MeshedVR for your previous help on Discord.

And, obviously, MeshedVR for Virt-a-Mate, which is a technological wonder worth exploring.
