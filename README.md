# Fastly Image Viewer .NET9
Fast and simple open source image viewer written in .NET 9 WPF under the [GNU General Public License v2.0](https://github.com/Rebzzel/Fastly-Image-Viewer/blob/master/LICENSE)

Forked for the sole purpose of creating a quick-to-develop, lightweight, image viewer that can finally open HEIC images without paying [$1](https://apps.microsoft.com/detail/9nmzlz57r3t7) to Microsoft.

I'll update more things as time goes by, but be sure that the hate of paying for viewing an image file in the default editor will make me continue working on it.

<br/>

## To Do
  - [ ] Rewrite the UI & controls; fix windows
  - [x] Expand zoom functionality 
  - [ ] Installer / portable folder
  - [x] Improve performance / parallelize 
 
<br/>

## How to
 - **Build**: Just clone the repo. 
 - **Install**: Someday I'll provide an installer.
 - **In both cases, the final step**: right click on a .heic file > Properties. On the "Opens with:" section click on `Change ...` button. Browse for ***fastly-image-viewer-net9.exe*** and click OK. Now the app will open your .heic files!
 
Target platforms: Windows 7 and above

<br/>

## Thanks to
[Rebzzel](https://github.com/Rebzzel) - original repo author\
[Magick.NET](https://about.openize.com/) - Magick.NET NuGet Package authors (seriously, what unearthly optimization do you do to decode HEICs?)


### License
This repository is a fork of [Fastly Image Viewer](https://github.com/Rebzzel/Fastly-Image-Viewer) which is licensed under the GNU General Public License v2.0.\
All modifications and this repository as a whole are distributed under the GPL-2.0 license. See [LICENSE](./LICENSE) for details.\
This project also uses third-party libraries, including Magick.NET, licensed under a Apache 2 license-2.0. Please see [THIRD_PARTY_LICENSES.md](./THIRD_PARTY_LICENSES) for more information.
