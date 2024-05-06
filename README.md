<div align="center">
    <a href="https://github.com/liris-xr/PLUME">
        <picture>
            <source media="(prefers-color-scheme: dark)" srcset="Resources~/Images/plume_viewer_dark.png">
            <source media="(prefers-color-scheme: light)" srcset="Resources~/Images/plume_viewer_light.png">
            <img alt="PLUME banner." src="Resources~/Images/plume_viewer_light.png" width="350">
        </picture>
    </a><br/><br/>
    <p>
        <a href="https://opensource.org/license/gpl-3-0"><img alt="License badge" src="https://img.shields.io/badge/license-GPLv3-blue.svg"/></a>
        <a href="https://discord.gg/c3evqEWMge"><img alt="Discord badge" src="https://img.shields.io/discord/1151165491767935107?logo=discord&logoColor=white&label=discord"/></a>
    </p>
</div>
<p align="center">
    Charles Javerliat, Sophie Villenave, Pierre Raimbaud, Guillaume Lavoué
    <br />
    <em>IEEE Conference on Virtual Reality and 3D User Interfaces (Journal Track)</em>
    <br />
    <a href="https://www.youtube.com/watch?v=_6krSw7fNqg"><strong>Video »</strong></a>&emsp;
    <a href="https://hal.science/hal-04488824"><strong>Paper »</strong></a>&emsp;
    <a href="https://liris-xr.github.io/PLUME/"><strong>Explore the docs »</strong></a>
    <br />
</p>

PLUME Viewer is a standalone application for viewing and analyzing PLUME record files generated with PLUME Recorder, independently of the Unity project. It offers analysis modules such as interactions analysis, 3D trajectories, in-context physiological signals tracks, position and eye gaze heatmaps. Heatmaps can be exported as point clouds with the scalar field embedded. PLUME Viewer is useful to review a recorded experiment, like you would a video in a media player, but you can explore the 3D scene. PLUME Viewer does not require the original Unity Project and only needs the record files and its associated asset bundle (built with PLUME Recorder).

## Getting Started

PLUME Viewer can be installed on Windows using the [latest release](https://www.github.com/liris-xr/PLUME-Viewer/releases/). Unzip the archive and launch the `PLUME-Viewer.exe` file. You will be prompted to select the record file and its associated asset bundle you want to replay. For more information, please refer to the [guide on how to replay a record](https://liris-xr.github.io/PLUME/viewer/replay/).

If you need more global information about PLUME, please refer to the [getting started guide](https://liris-xr.github.io/PLUME/get-started/).

## Documentation

The full documentation is available at [liris-xr.github.io/PLUME/](https://liris-xr.github.io/PLUME/). It includes a detailed description of the installation process, the file format specifications, the usage of the different tools, etc.

[![Button Docs]][Explore the docs]

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**. You can [open an issue](https://github.com/liris-xr/PLUME-Recorder/issues) to report a bug, request a feature, or submit a pull request.

## Contact

Discord server **(Recommended)** <a href="https://discord.gg/c3evqEWMge">
            <img alt="Discord badge" src="https://img.shields.io/discord/1151165491767935107?logo=discord&logoColor=white&label=discord"/>
        </a>

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

[Button Docs]: https://img.shields.io/badge/Explore%20the%20docs-%E2%86%92-brightgreen
[Explore the docs]: https://liris-xr.github.io/PLUME/