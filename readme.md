BlackFlag is an app designed to streamline downloading and converting video files from different sources utilizing ffmpeg. 
When necessary (eg. specific header requirements) BlackFlag may spin up an http proxy and run all requests through itself. 

The proxy will always listen to a random port, this allows BlackFlag to bind session info to that port without relying on other external data being passed in.


**TwitCasting**

TwitCasting is one of the supported platforms by BlackFlag, when a TwitCasting url is provided, BlackFlag will prompt the user to authenticate in order to be able to access user-bound resources. TwitCasting's cookies will then be bound to the current download session.~~~~
