---
title: Public Folder Replication fails with TNEF violation status 0x00008000
date: 2014-01-16 00:00:00
tags:
- Exchange Server
- Public Folders
- Powershell
---

**Edit 2014-06-02: For an update on this issue, please see [this post](/2014/06/02/TNEF-property-problem-update/).**

**Edit 2015-06-09: For the most recent update, please see [this post](/2015/06/09/Use-MAPIFolders-for-the-TNEF-issue/). We now have a much better solution.**

In Exchange 2010, you may find that public folder replication is failing between two servers. If you enable Content Conversion Tracing as described in my [Replication Troubleshooting Part 4](http://blogs.technet.com/b/exchange/archive/2008/01/10/3404629.aspx) post, you may discover the following error:

```
Microsoft.Exchange.Data.Storage.ConversionFailedException: The message content has become corrupted. ---> Microsoft.Exchange.Data.Storage.ConversionFailedException: Content conversion: Failed due to corrupt TNEF (violation status: 0x00008000)
```

There are other types of TNEF errors, but in this case we're specifically interested in **0x00008000**.  This means **UnsupportedPropertyType**.

What we've found is that certain TNEF properties that are not supposed to be transmitted are making it into public folder replication messages anyway. These properties are **0x12041002** and **0x12051002**.

To fix the problem, you can manually remove those properties from the problem items using MFCMapi, or you can use the following script.

The script accesses the public folder via EWS, so you must have client permissions to the folder in order for this to work (just being an administrator is not sufficient). Also, it requires EWS Managed API 2.0. Be sure to change the path of the Import-Module command if you install the API to a different path.

The syntax is:

.\Delete-TNEFProps.ps1 -FolderPath "\SomeFolder" -HostName casserver.contoso.com -UserName administrator@contoso.com

With this syntax, the script only checks for problem items in the specified folder. If you want it to fix those items, you must add **-Fix $true** to the command. Optionally, you can also add the **-Verbose** switch if you want it to output the name of every item as it checks it.

Edit: Moved the script to gist.github.com - easier to maintain that way

Edit: Updated the script to automatically recurse subfolders if desired. To do so, add **-Recurse $true**. For example, to process every single public folder, pass **-Recurse $true** with no folder path:

.\Delete-TNEFProps.ps1 -HostName casserver.contoso.com -UserName administrator@contoso.com -Recurse $true

[Download the script](https://gist.githubusercontent.com/bill-long/8573790/raw/Delete-TNEFProps.ps1) (You may need to right click->save as)

{% gist 8573790 %}
