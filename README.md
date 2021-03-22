# Virt-A-Mate Embody (Improved PoV, Passenger and Snug)

> Under development!

Improve Virt-A-Mate's possession experience (includes Improved PoV, Passenger and Snug).

> Looking for documentation? [Check out the wiki!](https://github.com/acidbubbles/vam-embody/wiki)

Currently, the possession mode (point-of-view) in Virt-A-Mate places the camera in _front_ of the head. That feels like you're floating in front of someone's body.

We want the camera to be exactly where the eyes are, with the same eye distance. The simple solution is to simply place the camera in the "right spot", and increase the minimum clipping distance, but that creates other problems, such as visual artefacts when moving the head, and close objects clipping. This plugin solves all of this:

- No visual artefacts when moving the head, and no clipping
- You can see your hair in front of your eyes as you move your head around
- Possession with scaled models won't reveal the "possessor" white spheres
- When you look down to look at your body when lying down or sitting on a chair, your chest won't be abnormally close
- If you touch your lips or your face, you can have them match so that physical contact matches what you see
- Settings will be kept as part of your scenes, so you don't have to set them every time

There's also the **Passenger** mode.

Instead of possessing a model that follows your movement, YOU will be following the model's movements.

Note that for some people, it may induce terrible nausea...

Finally, there's **Snug**, which lets you match your real-life body proportions with the VR model's, allowing you to "touch yourself" despite body proportion differences.

## License

[GNU GPLv3](LICENSE.md)
