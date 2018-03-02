---
title: Automating data collection with Powershell
date: 2014-12-06 00:00:00
tags:
- Exchange Server
- Powershell
---

One of the challenges with analyzing complex Exchange issues is data collection. Once the server goes into the failed state, any data collection at that point only shows us what the failed state looks like. It doesn't show us how it went from working to failing, and sometimes, that's what we need to see in order to solve the problem.

Certain types of data collection are fairly easy to just leave running all the time so that you can capture this transition from the working state to the failing state. For instance, you can typically start a perfmon and let it run for days until the failure occurs. Similarly, event logs can easily be set to a size that preserves multiple days worth of events.

Other types of data are not so easy to just leave running. Network traces produce so much data that the output needs to be carefully managed. You can create a circular capture, but then you have to be sure to stop the trace quickly at the right time before it wraps. The same applies to ExTRA traces, LDAP client traces, etc.

In several cases over the past year, I've solved this problem with a Powershell script. My most recent iteration of the script appears below, but I usually end up making small adjustments for each particular case.

In its current version, running the script will cause it to:

* Start a chained nmcap. Note that it expects 3.4 to be present so it can use the high performance capture profile.
* Start a circular ExTRA trace.
* Start a circular LDAP client trace.
* Wait for the specified events to occur.

While it waits, it watches the output folder and periodically deletes any cap files beyond the most recent 5. When the event in question occurs, it then:

* Collects a procdump.
* Stops the nmcap.
* Stops the LDAP client trace.
* Stops the ExTRA trace.
* Saves the application and system logs.

All of these features can be toggled at the top of the script. You can also change the number of cap files that it keeps, the NIC you want to capture, the PID you want to procdump, etc.

The script will almost certainly need some slight adjustments before you use it for a particular purpose. I'm not intending this to be a ready-made solution for all your data collection needs. Rather, I want to illustrate how you can use Powershell to make this sort of data collection a lot easier, and to give you a good start on automating the collection of some common types of logging that we use for Exchange.

Enjoy!

{% gist bill-long/3d6f36f7361ce7d83586 %}
