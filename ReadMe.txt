
Telemetry Control v1.2.1-f1 Build 01 (works with 1.2.2xx though)
------------------------------------

Purpose:
Basically it lets you control what information the game sends back to Paradox.
By default everytime you start the game, open a map, load a game, end a game, quit the application or do a few other things it will send information to Paradox \ Colassal Order. The mod allows you to selectively control what information you want to allow to be sent.

Can't I just block this via my software based or hardware based firewall\networking device?
Yes, you absolutely can, and still should if you like.

So why use this mod?
If you are blocking via the url or ip method it's all or nothing, maybe you don't mind sending certain information. Also doing so could produce errors in your logs, requests still get sent and processes have to wait for a responses that will not come, why not just stop said requests from happening in the first place. Also maintaining the other methods could be pain if depending on the particulars of the situation. Maybe you want to see what's trying to be sent, if so there is a setting for that in the mod.

What can be blocked:
 - See the screenshot, they are mostly self-explanatory and tool tips when you hover over each in the mod give you some detail. 

What can't be blocked without additional steps:
Game boot \ startup event
Unique system information sent on startup

Why can't those two items be blocked by this mod?
They happen too early in the start-up process for a mod to stop. A mod can't stop actions that take place before the mod itself is loaded. To block those items you'll need another method or a patched Assembly-CSharp.dll game file. It actually makes sense though, they'd need those two items if that's as far as your game actually got in terms of loading and then crashed. 

Why are they spying on me?
They aren't really trying to spy on you, well maybe a litte, but 90% of the data the goes back to them is likely very helpful in chasing down bugs, particularly ones that effect a large number of people. Additionally it provides them insight into the machine specs of the people who are actively playing thier game, how many mods are in use, number of certain 'things' in your maps,etc.
That said, while I actually think the default for sending this info should probably be set to 'on' they've provided no 'off' setting, which very much annoys me. Additionally while in some edge cases like matching certain info with a specific open pdx support case, it also seems excessive to me to  associate my steamid and pdx account information with every thing I do.

What's with this disable workshop ad option?
Cause I don't like that 'feed' i'm sure most people love it, personally I don't want too see it and it had no off-switch. So it comes along for the ride in the mod.



Additional options to block those two items
----------------------------------------
Using a patched dll

You will need to replace one of your game dll's with a patched version. The patched version forces that data to be logged to your log file instead of sent to PDX. If using a patched dll is not something you want to do, or you don't trust me that's cool I fully understand and this method is not for you then. In fact I'm not going to provide instructions, if you have a clue you'll know what to do with with the patched dll that you can find in bin folder of github repository, if you don't, then again this method is not for you. 

Hosts file or other SW\HW blocking tool
Add 127.0.0.1 opstm.paradoxplaza.com to your hosts file will redirect all traffic to your local machine. Again, you could just use this instead of the mod, but you might get some http response timeout type errors in your logs, or maybe not. Be aware using a host file will stop anything trying to communicate with that address. If you have something (software or hardware) that can block based on full url you'll want to feed it https://opstm.paradoxplaze.com/cities.
