<a name="readme-top"></a>
<div align="center">
    <a href="https://github.com/liris-xr/PLUME">
        <picture>
            <source media="(prefers-color-scheme: dark)" srcset="/Documentation~/Images/plume_banner_dark.png">
            <source media="(prefers-color-scheme: light)" srcset="/Documentation~/Images/plume_banner_light.png">
            <img alt="PLUME banner." src="/Documentation~/Images/plume_banner_light.png">
        </picture>
    </a>
    <br />
    <br />
    <p align="center">
        <strong>PLUME: Record, Replay, Analyze and Share User Behavior in 6DoF XR Experiences</strong>
        <br />
        Charles Javerliat, Sophie Villenave, Pierre Raimbaud, Guillaume Lavoué
        <br />
        <em>(Journal Track) IEEE Conference on Virtual Reality and 3D User Interfaces</em>
        <br />
        <a href="https://www.youtube.com/watch?v=_6krSw7fNqg"><strong>Video »</strong><a>
        <a href="https://hal.science/hal-04488824"><strong>Paper »</strong></a>
        <a href="https://github.com/liris-xr/PLUME/wiki/"><strong>Explore the docs »</strong></a>
        <br />
        <br />
        <a href="https://github.com/liris-xr/PLUME/issues">Report Bug</a>
        ·
        <a href="https://github.com/liris-xr/PLUME/issues">Request Feature</a>
    </p>
</div>

<details>
    <summary>Table of Contents</summary>
    <ol>
        <li>
            <a href="#about-plume-viewer">About PLUME Viewer</a>
        </li>
        <li>
            <a href="#getting-started">Getting Started</a>
            <ul>
                <li><a href="#prerequisites">Prerequisites</a></li>
                <li><a href="#installation">Installation</a></li>
                <li><a href="#development">Development</a></li>
            </ul>
        </li>
        <li><a href="#usage">Usage</a></li>
            <ul>
                <li><a href="#start-the-viewer">Start the Viewer</a></li>
                <li><a href="#interactive-replay">Interactive Replay</a></li>
                <li><a href="#analysis">Analysis</a></li>
                <li><a href="#markers">Markers</a></li>
            </ul>
        <li><a href="#roadmap">Roadmap</a></li>
        <li><a href="#contributing">Contributing</a></li>
        <li><a href="#license">License</a></li>
        <li><a href="#contact">Contact</a></li>
        <li><a href="#citation">Citation</a></li>
    </ol>
</details>

## About PLUME Viewer

PLUME Viewer is a standalone application for viewing and analyzing PLUME record files generated with PLUME Recorder, independently of the Unity project. It offers analysis modules such as interactions analysis, 3D trajectories, in-context physiological signals tracks, position and eye gaze heatmaps. Heatmaps can be exported as point clouds with the scalar field embedded. PLUME Viewer is useful to review a recorded experiment, like you would a video in a media player, but you can explore the 3D scene. PLUME Viewer does not require the original Unity Project and only needs the record files and its associated asset bundle (built with PLUME Recorder).

## Getting Started

### Prerequisites
PLUME Viewer only runs on Windows platforms.

### Installation
1. Download the latest release of the built application (`.zip` extension).
2. Uncompress the archive in the folder of your choice.

### Development
1. Create a new project with Unity 2022 or later.
2. Clone or download the repository inside the Packages folder of your Unity project.
3. Unity will import the package into your project.
4. Drag the PLUME Viewer prefab inside an empty Unity Scene.
5. You can now edit the source code to adapt it to your needs. Feel free to contribute to this repository !


## Usage

### Start the Viewer
Once the release downloaded, launch the PLUME Viewer.exe

### Interactive Replay
#### Media Toolbar
Use the media toolbar to play, pause, stop the replay. You can also use the `space bar` to pause/play.

Use the `-` to slow the speed of the replay down to 0.25x. Use the `+` to accelerate the speed of the replay up to
2x.

Click the `maximize` button on the far right to hide the side panels and show the replay in fullscreen. Click
again to go back to windowed view of the replay.

Use the `camera dropdown` menu to change the camera used for visualization.

#### Free Camera
Free 3D navigation inside the virtual environment.

To navigate : get focus on the replay panel (a blue rectangle shows around the panel); maintain `right-click` and use `WASD + Q/E` to move around.

#### Top-view Camera
Navigable orthogonal projection from the top of the virtual environment.

To navigate : get focus on the replay panel (a blue rectangle shows around the panel); use `WASD` to move up / down and left / right; use `Q` to lower the camera near plane; use `E` to higher the camera near plane.

#### Main Camera
Replays from the point of view of the camera used during the record.

#### Hierarchy
Displays the recorded hierarchy of the gameobjects in the scene, it is updated during the replay. To copy an object GUID to the clipboard, select the object and press `CTRL+C`, doing so on multiple objects will create a list of GUIDs separated by a comma.

#### Timeline
Displays the replay timeline. There are two methods to navigate through the timeline: 
1. Click on the timescale of the timeline to jump to desired time;
2. Change the current time in the textfield on the left. The timeline is scrollable using the scroll bar at the bottom. The scrollbar can be used to change the timeline scale by dragging the left and right sides of the scrollbar.

The timeline can contains tracks for physiological signals. Those signals can recorded using the Lab Streaming Layer integration from the PLUME Recorder. Information about the LSL stream's name and nominal frequency are displayed on the left, along with the maximum and minimum value of the signal, and the nearest value to the time cursor (last value before the cursor).

### Analysis
#### General use of analysis modules
Analysis module can be found on the top-right corner of the viewer.

For fields that require identifiers of objects, get the identifier by going to the hierarchy, selecting the object, copying its GUID by pressing `CTRL+C` and pasting it inside the field using `CTRL+V`.

For every analysis module, a dedicated time range can be set to only generate the result for this specific
interval.

Click generate to create the visualization. Inside each module appears the list of generated results. They can
be hidden / shown using the eye icon button. They can be deleted using the red bin button.

#### Trajectory
1. **Object Id** : GUID of the object used to compute the trajectory.
2. **Markers** : List of string that represent recorded markers. Indicated markers are shown above the computed trajectory inside the virtual environment.
3. **Teleportation Tolerance** : Represents (in meter) the maximal distance between 2 points before considering the gap to be a teleportation.
4. **Teleportation Segments** : Enable the drawing of teleportation segments as dash lines.
5. **Decimation Tolerance** : Minimum distance between 2 points. Any points inside the distance will be discarded to simplify the trajectory.
6. **Include Rotations** : Show the orientation of the object above the computed trajectory inside the virtual environment.

#### Interactions Highlight
1. **Interactor(s)** : List of GUID of the interactors within the virtual environment. Can be left empty to select every interactor in the virtual environment.
2. **Interactable(s)** : List of GUID of the interactors within the virtual environment. Can be left empty to select every interactable in the virtual environment.
3. **Interaction** : Change the type of interaction to visualize using the dropdown.

#### Position Heatmap
1. **Projection Caster** : GUID of the object used to compute the heatmap. Its position is orthogonally projected towards the ground.
2. **Projection Receivers** : List of GUID of the object that will receive the projection and be used to compute occlusion.
3. **Include Children** : Recursively include children gameobjects of the listed projection receivers objects.

#### Eye Gaze Heatmap
1. **Projection Caster** : GUID of the object used to compute the heatmap. Its position is orthogonally projected towards the ground.
2. **Projection Receivers** : List of GUID of the object that will receive the projection and be used to compute occlusion.
3. **Include Children** : Recursively include children gameobjects of the listed projection receivers objects.

### Markers
List of all the recorded markers that can be found on the bottom-right of the viewer. Markers appear as coloured vertical lines in the timeline. 

Click on the checkbox to show or hide them in the timeline.

Click on the left arrow to jump to the nearest marker to the left of the time cursor.

Click on the right arrow to jump to the nearest marker to the right of the time cursor.

## Roadmap
See the [open issues](https://github.com/Plateforme-VR-ENISE/PLUME/issues) for a full list of proposed features (and
known issues).

## Contributing
Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any
contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also
simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

Distributed under the <a rel="license" href="https://github.com/liris-xr/PLUME/blob/master/LICENSE">GPLv3 License</a>.

## Contact

Charles JAVERLIAT - charles.javerliat@gmail.com

Sophie VILLENAVE - sophie.villenave@ec-lyon.fr

## Citation
```
@article{javerliat_plume_2024,
	title = {{PLUME}: {Record}, {Replay}, {Analyze} and {Share} {User} {Behavior} in {6DoF} {XR} {Experiences}},
	url = {https://ieeexplore.ieee.org/document/10458415},
	doi = {10.1109/TVCG.2024.3372107},
	journal = {IEEE Transactions on Visualization and Computer Graphics},
	author = {Javerliat, Charles and Villenave, Sophie and Raimbaud, Pierre and Lavoué, Guillaume},
	year = {2024},
	note = {Conference Name: IEEE Transactions on Visualization and Computer Graphics},
	pages = {1--11}
}
```