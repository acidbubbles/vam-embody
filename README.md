# Virt-A-Mate Snug

Better VR controller alignement in possession mode

## What does it do

Identify the VR model proportions, and displace the VR hands so they fit your own body. For example, touching your navel in reality should also touch the VR model navel, even though it may have different proportions.

This should provide improved realism when touching your own body while possessing a person in Virt-A-Mate.

## How to use

1. Add `Snug.cs` to a Person atom
2. In the plugin UI, check the `Show Visual Cues` checkbox. Circles will appear and you will see crosses in your hands and wrist.
3. For each Anchor:
   1. Adjust the "Virtual" scale and offset until it matches the model on the screen tightly.
   2. Adjust the "Physical" scale and offset until it matches your physical hand in space (this can be harder to do) .
4. Adjust the world scale until your extended arms match the model's arms.
5. Possess the model, but not the hands.
6. Check the `Possess Hands` checkbox.

You can also made adjustments to the hands, so that they better align with yours. You can move your hands in front of your eyes and put your headset on and off until the position matches.

## License

[MIT](LICENSE.md)
