---
title: Cleaning up Microsoft Exchange System Objects - part 2
date: 2014-03-08 00:00:00
tags:
- Exchange Server
- Public Folders
---

In a post last month, called [Cleaning up Microsoft Exchange System Objects (MESO)](/2014/01/11/Cleaning-Up-Microsoft-Exchange-System-Objects), I described how to determine which objects can be eliminated from the **MESO** container if you have completely removed public folders from your environment. But what if you still have public folders?

As I mentioned in my previous post, **you only need MESO objects for mail-enabled public folders**. When you mail-enable a public folder, Exchange creates a directory object for it, and when you mail-disable or delete the folder, Exchange is supposed to delete the directory object. Unfortunately, that doesn't always work like it should, and you can end up with a lot of public folder objects in the **MESO** container that don't point to any existing folder.

To make matters worse, it's not very easy to figure out which directory objects point to an actual folder. You can't assume much from the name itself - you could have dozens of public folders all named "Team Calendar" in different parts of the hierarchy, so which directory object points to which folder?

When you send email to a mail-enabled public folder, Exchange uses the **legacyExchangeDN** attribute on the directory object to look up the folder in the public folder database (or public folder mailbox in the case of Exchange 2013). However, the **legacyExchangeDN** property on the public folder in the database is an internal property - you can't see it, even using tools like **MFCMapi**. So matching them up that way is not an option.

However, you *can* go in the other direction. Rather than taking a directory object and trying to find the store object, you can start with the store object and find the corresponding directory object easily. This is because if you look at the **MAPI** property **PR\_PF\_PROXY** on the folder, the store finds the correct directory object and returns its **objectGUID**. This is essentially what happens when you run `Get-PublicFolder \Some\Folder | Get-MailPublicFolder` in Exchange Management Shell.

Thus, in order to figure out which public folder directory objects are not linked to anything, you would need to retrieve all the directory objects that exist and then determine which ones are linked to folders based on **PR\_PF\_PROXY** or the Powershell cmdlets. After you eliminate those, you know that any public folder directory objects left over are not linked to anything, and they can be deleted.

There are a few ways you could go about this. One would be to use a client API such as **Exchange Web Services** to enumerate the public folders and check the property that way. While I do use **EWS** in a lot of my scripts, there is one big drawback to using it for this sort of operation - the fact that there is no way to use admin rights via **EWS**. As I explained in an old post called [Public Folder Admin Permissions Versus Client Permissions](http://blogs.technet.com/b/bill_long/archive/2010/04/28/public-folder-admin-permissions-versus-client-permissions.aspx), it doesn't matter what admin rights your user account has when you're using a client like Outlook. Outlook never attempts to pass admin flags at logon, so if you don't have client permissions to a public folder, you won't be able to see that public folder, even if you're logged on as an org admin. **EWS** works the same way - there is no way to pass admin flags via **EWS**. This means that if you use **EWS**, you might not see all the public folders, so you might erroneously delete public folder directory objects that are actually still in use.

You could work around this limitation by granting yourself client permissions to all the public folders. Another option is to use **MAPI**, where you can pass admin flags. Of course, writing a **MAPI** tool is not trivial.

A better approach is to just use **Exchange Management Shell**. While this can be slower than **EWS**, the management shell uses your admin rights, so you will be able to see all public folders in the hierarchy, even if you don't have client permissions to them.

However, there is one other caveat to be aware of. Sometimes, public folders can have directory objects when the public folder is not flagged as mail-enabled. This is described in [KB 977921](http://support.microsoft.com/kb/977921). If the folder is in this state, email sent to the folder will succeed, even though the management shell says the folder is not mail-enabled. You should be sure your folders are not in this state before you start making decisions about what to delete based on what **Exchange Management Shell** says, or else you might delete a directory object for a folder that is actually functioning as a mail-enabled folder.

That said, I created a simple script that demonstrates how you can check for unneeded public folder directory objects using **Exchange Management Shell**. Note that this script only *identifies* the unneeded directory objects. I'll leave the actual deletion of them as an exercise for the reader. Hint: The $value in the loop at the end is the distinguishedName of the directory object. It's probably a good idea to sanity check the results, and you might want to export the directory objects before you start deleting things.

[Download the script](https://gist.github.com/bill-long/9441617/raw/Find-UnlinkedPFProxies.ps1) (You may need to right click->save as)

{% gist 9441617 %}
