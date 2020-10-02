# Hello WebGPU.Net

This an example of how to use WebGPU on .Net (Windows and Browser), leveraging the [WaveEngine WebGPU bindings](https://github.com/WaveEngine/WebGPU.NET).

![https://webgpu.z28.web.core.windows.net/](webgpu-demo.gif)

Check [https://webgpu.z28.web.core.windows.net/](https://webgpu.z28.web.core.windows.net/) for a live preview.

WebGPU on the browser is only currently supported on Chrome/Edge Canary versions, and Firefox Nightly.

- Firefox: `about:config` -> `dom.webgpu.enabled: true`
- Chrome/Edge: `chrome://flags/#enable-unsafe-webgpu` -> "Enabled"

## Run it locally

### Set-Up environment

Right now compilation must be finished on linux. Follow the _dotnet-setup.sh_
script to install necessary dependencies (tested on Ubuntu 18 LTS).

### Compile & Link

First, compile the project with
MSBuild, then go to Linux if you are on windows (Ubuntu 18 LTS - WSL1) and run
_package.sh_ with the specific arguments of your project:

`./package.sh [PATH_TO_PROJECT]/HelloWebGPUNet/HelloWebGPUNet.Web/bin/Release/netstandard2.0 $HOME/.build HelloWebGPUNet.Web.dll -a`

`-a` option builds the project on AOT mode (the only one supported for now due to a bug in mono).
Check the full list of arguments at _package.sh_

After compilation and linking are successfull, serve the folder bin/\$(Configuration)/wasm with a static server.

#### libWebGPU.o

TODO
