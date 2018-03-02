---
title: DsQuerySitesByCost and public folder referrals
date: 2015-03-25 00:00:00
thumbnail: ruby02.jpg
tags:
- Exchange Server
- Active Directory
---

{% asset_img ruby02.jpg Picture of Ruby %}
In Exchange 2010 and older, when you mount a public folder database, the Information Store service asks Active Directory for the costs from this site to every other site that contains a public folder database. This is repeated about every hour in order to pick up changes. If a client tries to access a public folder which has no replica in the local site, Exchange uses the site cost information to decide where to send the client. This means that, as with so many other features, public folder referrals will not work properly if something is wrong with AD.

There are several steps involved in determining these costs.

1. Determine the name of the site we are in, via **[DsGetSiteName](https://msdn.microsoft.com/en-us/library/ms675992%28v=vs.85%29.aspx)**.
2. Determine the names of all other sites that contain PFs.
2. Bind to the Inter-Site Topology Generator, via **[DsBindToISTG](https://msdn.microsoft.com/en-us/library/ms675933%28v=vs.85%29.aspx)**.
3. Send the list of site names for which we want cost info, via **[DsQuerySitesByCost](https://msdn.microsoft.com/en-us/library/ms676020%28v=vs.85%29.aspx)**.
4. From the sites in the response, we will only refer clients to those between cost 0 and 500.

This gives us a lot of opportunities to break. For example:

* Can't determine the local site name.
* Can't bind to the ISTG.
* The costs returned are either infinite (-1) or greater than 500.

I recently had a case where we were fighting one of these issues, and I could not find a tool that would let me directly test **DsQuerySitesByCost**. So, I created one. The code lives on GitHub, and you can download the binary by going to the Release tab and clicking DsQuerySitesByCost.zip:

[https://github.com/bill-long/DsQuerySitesByCost](https://github.com/bill-long/DsQuerySitesByCost)

Typically, you would want to run this from an Exchange server with a public folder database, so that you can see the site costs from that database's point of view. The tool calls **DsGetSiteName**, **DsBindToISTG**, and **DsQuerySitesByCost**, so it should expose any issues with these calls and make it easy to test the results of configuration changes.

You can run the tool with no parameters to return costs for all sites, or you can pass each site name you want to cost as a separate command-line argument.

{% asset_img sshot-13.png Image %}

Thanks to the [Active Directory Utils](http://activedirectoryutils.codeplex.com/) project, which got me most of the way there.
