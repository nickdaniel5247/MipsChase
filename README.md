# Mips Chase
The hop algorithm consists of first trying to hop directly opposite from the player. If this is not possible, then a hop radius is created around the target and its intersections with the wall are noted. The intersection thats furthest from the player is chosen. Alongside, to prevent the target from hugging the wall, sometimes a slight interpolation from the angle of the chosen intersection point to the angle towards the center of the screen is made; this puts the target off a direct course of the wall but still avoids the player.

## Example Gameplay

https://youtu.be/uUMpCuUShOE
