# URP Anaglyph3D
 Anaglyph 3D (red/cyan) render feature for Unity's URP

![Sample Image](images~/sample.jpg)

## Requirements
Anaglyph3D [v2](https://github.com/ryanslikesocool/Anaglyph3D/releases/tag/v2.1.1) requires Unity 2021.3 with URP 12.1.8 or later.\
Anaglyph3D [v3](https://github.com/ryanslikesocool/Anaglyph3D/releases/tag/v3.1.0) requires Unity 2022.1 or later with URP 13.1.7 or later.\
Anaglyph3D [v4](https://github.com/ryanslikesocool/Anaglyph3D/releases/tag/v4.0.0-pre.1) requires Unity 2022.3 or later with URP 14.0.9 or later.

## Install
**Recommended Installation** (Unity Package Manager)
- "Add package from git URL..."
- `https://github.com/ryanslikesocool/Anaglyph3D.git`

**Alternate Installation** (not recommended)
- Get the latest [release](https://github.com/ryanslikesocool/Anaglyph3D/releases)
- Import into your project's Plugins folder

## Usage
In your Forward Renderer asset, add the "Anaglyph Feature" render feature and change settings as desired.

| Property | Information |
| ----- | ----- |
| `Render Pass Event` | When should the effect render? |
| `Queue` | What kinds of objects should the effect render? |
| `Layer Mask` | Which layers should be rendered? |
| `Spacing` | The spacing between the red and cyan channels.<br/>A value of `0` will ignore the focal point.  This is useful for orthographic cameras.<br/>A negative value will swap the red and cyan. |
| `Focal Point` | The point `<x>` units in front of the camera where the red and cyan channels meet. |

## Notes
- Rendering with this effect may be expensive, since the whole screen must be rendered up to three times every frame.