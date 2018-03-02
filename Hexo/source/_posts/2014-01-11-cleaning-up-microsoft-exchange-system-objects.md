---
title: Cleaning Up Microsoft Exchange System Objects (MESO)
date: 2014-01-11 00:00:00
tags:
- Exchange Server
- Public Folders
- Powershell
---

Someone recently posted a question on an old blog post of mine:

{% blockquote %}
Bill,

We have eliminated our public folders, and I would like to clean out the MESO folder. There are still hundreds of objects that probably serve no purpose, but I don't see a way of determining which are still necessary.
 
Some examples:
 
- EventConfig_Servername (where the server is long gone) 
- globalevents (also globalevents-1 thru 29) 
- internal (also internal-1 thru 29) 
- OAB Version 2-1 (and 2-2 and 2-3) 
- Offline Address Book Storage group name 
- OWAScratchPad{GUID} (30 of them) 
- Schedule+ Free Busy Information Storage group name 
- StoreEvents{GUID} (31 of them) 
- SystemMailbox{GUID} (over 700 of them) 
 
Most of the SystemMailboxes are Type: msExchSystemMailbox, but 3 are Type: User. I found one that was created last month. Apart from the SystemMailboxes, most everything else has a whenChanged date of 2010. What to do?
 
Thanks, Mike
{% endblockquote %}

When it comes to public folders, **you only need MESO objects for mail-enabled folders, and a folder only needs to be mail-enabled if people are going to send email to it.** No one ever needs to send email to any of the system folders that are part of your public folder tree.

Everything in Mike's list except the very last item is a directory object for a system folder, and even if the public folders were still present in the environment, these objects would serve absolutely no purpose. It is fine to delete them whenever you want, though if the folders themselves are still present, you might want to do it gracefully with Disable-MailPublicFolder.

The SystemMailbox objects are trickier. Each SystemMailbox corresponds to a database, and the database is identified by the GUID between the curly braces. To determine if the SystemMailbox object can be safely deleted, you need to determine if that database still exists. This is easy to do with a simple Powershell command:

{% codeblock lang:powershell %}
{% raw %}
([ADSI]("LDAP://<GUID=whatever>")).distinguishedName
{% endraw %}
{% endcodeblock %}

Here's an example from one of my labs. You can see that the first command I typed returned nothing, because the GUID didn't resolve (I purposely changed the last digit). The second one did resolve, returning the DN of the database.

{% asset_img sshot-1.png Image %}

You could also use a simple script to check all the SystemMailbox objects in a particular MESO container and tell you which ones don't resolve:

{% codeblock lang:powershell %}

	# Check-SystemMailboxGuids.ps1
	# 
	# This script checks for SystemMailbox objects that have GUIDs
	# which correspond to nonexistent databases.
	
	#####
	#
	# Change this to the MESO container you want to check
	# 
	
	$mesoDN = "CN=Microsoft Exchange System Objects,DC=bilong,DC=test"
	
	#
	#####
	
	$mesoContainer = [ADSI]("LDAP://" + $mesoDN)
	$sysMbxFinder = new-object System.DirectoryServices.DirectorySearcher
	$sysMbxFinder.SearchRoot = $mesoContainer
	$sysMbxFinder.PageSize = 1000
	$sysMbxFinder.Filter = "(cn=SystemMailbox*)"
	$sysMbxFinder.SearchScope = "OneLevel"
	
	$sysMbxResults = $sysMbxFinder.FindAll()
	"Found " + $sysMbxResults.Count + " System Mailboxes. Checking GUIDs..."
	
	foreach ($result in $sysMbxResults)
	{
		$cn = $result.Properties.cn[0]
		$guidStartIndex = $cn.IndexOf("{")
		$guidString = $cn.Substring($guidStartIndex + 1).TrimEnd("}")
		$guidEntry = [ADSI]("LDAP://<GUID=" + $guidString + ">")
		if ($guidEntry.distinguishedName -eq $null)
		{
			"Guid does not resolve: " + $cn
		}
	}
	
	"Done!"

{% endcodeblock %}
