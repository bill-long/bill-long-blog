---
title: Directory Name Must Be Less Than 248 Characters
date: 2014-07-09 00:00:00
tags:
- Powershell
---

Over the holiday weekend, I was deleting some old projects out of my coding projects folder when Powershell returned an error stating, "The specified path, file name, or both are too long. The fully qualified file name must be less than 260 characters, and the directory name must be less than 248 characters." I found that attempting to delete the folder from **explorer** or a **DOS** prompt also failed.

{% asset_img sshot-12.png Image %}

This error occurred while I was trying to remove a directory structure that was created by the **yeoman**/**grunt**/**bower** web development tools. Apparently **npm** or **bower**, or both, have no problem creating these deep directory structures on Windows, but when you later try to delete them, you can't.

A little searching turned up several blog posts and a [Stack Overflow question](http://stackoverflow.com/questions/8745215/best-way-to-resolve-file-path-too-long-exception). The workaround of appending "\\\\?\\" to the beginning of the path didn't seem to work for me.

I found some tools that claimed to be able to delete these files, but as usual, I was annoyed at the idea of having to install a tool or even just download an exe to delete some files.

Edit: Thanks to **[AlphaFS](https://github.com/alphaleonis/AlphaFS)**, this is much easier now. I've removed the old script. With **AlphaFS**, you can delete the folder with a single Powershell command. First, you need to install the **AlphaFS** module into Powershell, and the easiest way to do that is with **[PsGet](http://psget.net/)**.

So first, if you don't have **PsGet**, run the command shown on their site:

{% codeblock lang:powershell %}
(new-object Net.WebClient).DownloadString("http://psget.net/GetPsGet.ps1") | iex
{% endcodeblock %}

Once it's installed, import the **PsGet** module, and use it to install **AlphaFS**. Note the following command refers to what is currently the latest release of **AlphaFS**, but you might want to check for a later one:

{% codeblock lang:powershell %}
Import-Module PsGet

Install-Module -ModuleUrl "https://github.com/alphaleonis/AlphaFS/releases/download/v2.0.1/AlphaFS.2.0.1.zip"
{% endcodeblock %}

Now you can use **AlphaFS** to delete the directory. You only need to point it the top folder, and it will automatically recurse:

{% codeblock lang:powershell %}
 [Alphaleonis.Win32.Filesystem.Directory]::Delete("C:\some\directory", $true, $true)
{% endcodeblock %}

Here's how these commands look when you run them in the shell:

{% asset_img sshot-14.png Image %}

This is a lot simpler than the original script I posted using the Experimental IO Library. Thanks **AlphaFS**!
