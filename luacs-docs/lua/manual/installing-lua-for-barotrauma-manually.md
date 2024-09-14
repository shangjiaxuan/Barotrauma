# Installing LuaCs For Barotrauma Manually

The manual installation of the mod involves the simple task of just replacing some files in your game files, so lets get into how to do that.

## Server patch vs Client patch

- The client patch is for when you need to install client-side + server-side support, installing the client patch will allow you to use the mod in singleplayer, provide features that require client-side in multiplayer, and allow you to host servers with LuaCs.

- The server patch only affects the server files, this patch is more useful for dedicated servers, since they have don't have the client part.

- Both patches will replace your server Vanilla executable (the one you use by default when hosting) with the LuaCs one, this allows you to host servers with LuaCs without having the  content package enabled or having to select the server executable.

### Do NOT select the server executable after installing the patch, leave it as Vanilla, the patch replaces the Vanilla server executable. You also don't need the content package at all anymore.

If you don't know which one to use, download **luacsforbarotrauma_patch_xxxxxxxxx_client.zip**. (xxxxxxxxx is your operating system)

## How to apply a patch to my game files/my dedicated server
First you need to download the correct patch that corresponds to your platform, head over to `https://github.com/evilfactory/LuaCsForBarotrauma/releases/tag/latest` and choose which patch you want to install. For example, if i want to install both client-side and server-side on my windows machine, I'll download `luacsforbarotrauma_patch_windows_client.zip`.

After downloading the patch file that you want to use, you will need to now find where your game files are located, you can do that by:

1. Going to your steam library

2. Right clicking Barotrauma

3. Clicking Manage -> Browse Local Files

4. Your file explorer should open with where your game files are located.

5. (MacOS) When going to the game files in MacOS, you will probably notice you aren't in the actual game files, you have to right click the Barotrauma application, and click "Show Package Contents", then navigate to Contents -> MacOS, this is where you will put the patch files.


Now, all you have to do is extract the patch files you downloaded, and paste their files into your game files folder (or in case of dedicated servers, where your server is installed), this should prompt you to replace a bunch of .dll files.
After that, it's done, you can test if it was successfully installed by testing the debug console (F3) commands: `cl_reloadluacs` (for client-side) or `reloadluacs` (for server-side, you need to be hosting a server)

If you are confused, there's this video showing how to install client-side manually on Windows:

<iframe width="560" height="315" src="https://www.youtube.com/embed/1T0srKPp5BI" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>


## Builds

Builds are full builds of Barotrama without the Content folder, those generally are not recommended unless you know what you are doing, but all you have to do to get them working is download the one that matches your platform, and copy the original Content folder from your game to the folder where you extract your build, then you just need to run the game executable.