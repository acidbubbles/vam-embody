# Virt-A-Mate Embody (Improved PoV, Passenger and Snug)

> Under development!

Plugins to improve possession experience (include Improved PoV, Passenger and Snug).

## Improved POV Plugin

Improved POV handling so that possession actually feels right.

> Check out [Improved PoV on Virt-A-Mate Hub](https://hub.virtamate.com/resources/improved-pov.102/)

### Why

Currently, the possession mode (point-of-view) in Virt-A-Mate places the camera in _front_ of the head. That feels like you're floating in front of someone's body.

We want the camera to be exactly where the eyes are, with the same eye distance. The simple solution is to simply place the camera in the "right spot", and increase the minimum clipping distance, but that creates other problems, such as visual artefacts when moving the head, and close objects clipping. This plugin solves all of this:

- No visual artefacts when moving the head, and no clipping
- You can see your hair in front of your eyes as you move your head around
- Possession with scaled models won't reveal the "possessor" white spheres
- When you look down to look at your body when lying down or sitting on a chair, your chest won't be abnormally close
- If you touch your lips or your face, you can have them match so that physical contact matches what you see
- Settings will be kept as part of your scenes, so you don't have to set them every time

### How to use

1. Download the `ImprovedPoV.cs` file from the [latest release's Assets](https://github.com/acidbubbles/vam-improved-pov/releases), and save it somewhere (e.g. `(VaM Install)/Saves/Scripts/Acidbubbles/ImprovedPoV.cs`)
2. Select the Person you want to possess, and in Plugins, add the `ImprovedPoV.cs` file
3. Possess the model
4. Optionally Configure the plugin (camera depth, show or hide hair, etc.) to adjust to your taste
5. Adjust the world scale until your hand in relationship to your body matches. Close your eyes, put your index finger horizontally at the level of your mouth, and look at where it shows up. Increase or decrease the scale so that your "proportions" fit! Or you can activate the auto-scale option.

### How does it work

- The possessor model is moved so the head is offset in relationship to the camera (gives the same result as moving the camera itself)
- The face shaders are replaced with one that supports transparency
- The script attaches to `onPreRender` and `onPostRender` events to dynamically change the alpha channel of all materials
- For Sim V2 hair, the hair width in the shader is set to zero, which achieves the same result as hiding it. Older hairs simply reduces the alpha adjust)

### Known issues

- This script won't work with Sim and Sim2 hair.

### Credits

- MalMorality for the plugin name!

## Passenger Plugin

> Check out [Passenger on Virt-A-Mate Hub](https://hub.virtamate.com/resources/passenger.103/)

Instead of possessing a model that follows your movement, YOU will be following the model's movements.

Note that for some people, it may induce terrible nausea...

## Snug Plugin

Better VR controller alignement in possession mode

> Check out [Snug on Virt-A-Mate Hub](https://hub.virtamate.com/resources/snug.104/)

### What does it do

Identify the VR model proportions, and displace the VR hands so they fit your own body. For example, touching your navel in reality should also touch the VR model navel, even though it may have different proportions.

This should provide improved realism when touching your own body while possessing a person in Virt-A-Mate.

![vam-snug default ring size](screenshots/vam-snug-anchors.png)

### Videos

Snug standing tutorial:

[![Snug Standing Tutorial](https://img.youtube.com/vi/luZpxPGnYhg/0.jpg)](https://www.youtube.com/watch?v=luZpxPGnYhg)

Snug sitting tutorial:

[![Snug Sitting Tutorial](https://img.youtube.com/vi/ljJgaQqVssk/0.jpg)](https://www.youtube.com/watch?v=ljJgaQqVssk)

### How to use

Add `Snug.cs` to a Person atom. You should see rings appear. Those are "anchors", which represents the virtual model proportions.

Play with the "virtual offset" and "virtual scale" values until the rings fit (does't need to be ultra precise).

First, try possessing the model normally and play with the world scale until your arms stretch horizontally naturally (the model shouldn't look like the arms are being teared of, nor be too bended when yours are extended). I also recommend [vam-improved-pov](https://github.com/acidbubbles/vam-improved-pov) to help with the eyes position, otherwise the body will always feel too far behind.

Now to adjust to your own body proportions. For each anchor, adjust the "physical" scale and offset until it matches where your actual hand is. For example, if you touch your own navel, the abdomen physical anchor (white) should touch your controller position.

You should see a yellow line connecting your controller, then the "adjusted" controller position, and finally reach the center of the model. This is only to help visualize the displacement.

You can now proceed with possessing the model. First, uncheck the `Show Visual Cues` checkbox in the plugin UI. Then, possess the person, but do not possess the hands. Finally, check the `Possess Hands` checkbox in the Snug plugin interface.

You can make some finer adjustments to the physical scale and offset, as well as your hands, so that they better align with yours using the Hand Offset and Rotation sliders. You can move your hands in front of your eyes and put your headset on and off until the position matches.

You should now be able to interact with your own body, and have the virtual model do the same thing even though your proportions differ!

### Known issues

- There is a slight jitter; this is because VaM does all updates in Update, including drawing the lasers and updating the camera position. This means we are always one frame late.


## License

[GNU GPLv3](LICENSE.md)
