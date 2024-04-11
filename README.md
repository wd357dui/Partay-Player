# Partay Player

A [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)-based media player that plays not only videos but also images, text files, and even flash files (using [ruffle](https://ruffle.rs)) in a folder that you choose, as a playlist

# Usage

 - Upon start, a Folder Browser Dialog will pop up to let you choose the folder you will be playing, if no folder is selected, the program will use the current folder.
 - By default, the program will go to the next file after the current file ends playing. If the current file is flash it will not automatically go to the next file. If the current file is not video audio or flash, then (by default) a 7-second playtime (delay) is used.
 - <kbd>F5</kbd> - Refresh files list. (it will try to stay on the current file; if the current file is gone it will go to the first file instead)
 - <kbd>F11</kbd> - Switch fullscreen
 - <kbd>Space</kbd> or <kbd>Page Down</kbd> or <kbd>⏭</kbd> or <kbd>→</kbd> or <kbd>↓</kbd> - Go to next file
 - <kbd>Page Up</kbd> or <kbd>⏮</kbd> or <kbd>←</kbd> or <kbd>↑</kbd> - Go to previous file
 - <kbd>F</kbd> - Select a new file to set as current file
 - <kbd>C</kbd> - Enable/Disable **controls** for video and audio files and reload the current page
 - <kbd>L</kbd> - Enable/Disable **looping** for video and audio files and reload the current page (when looping is enabled, it will not automatically go to the next file)
 - <kbd>S</kbd> - Enable/Disable **7-second delay** on non-video/audio/flash files and reload the current page

# Installation

Download the [latest release](https://github.com/wd357dui/Partay-Player/releases/latest/download/PartayPlayer.zip) (or you can clone and build it yourself)
