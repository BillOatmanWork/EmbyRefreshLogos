# EmbyRefreshLogos

Command line application to force Emby to refresh the Live Tv guide logos.
```
EmbyRefresh filePath API_KEY {Emby server IP Address} {port}
```
FilePath extention can be .m3u, .xml, or .xmltv
API_KEY - Mandatory Emby server API key. To get Emby api key go to dashboard>advanced>security and generate one
server  - Optional Emby server IP address.  Not required if running on the Emby server machine.
port    - Optional Emby server port.  Not required if the server is using the default 8096.

Example running on Emby server box with default port
EmbyRefreshLogos C:\m3u\Emby.m3u api-key

Example not running on Emby server box
EmbyRefreshLogos C:\m3u\Emby.xmltv api-key 192.168.50.50 8000

If the provided file fails to set the logo, the program will try the other file type.
