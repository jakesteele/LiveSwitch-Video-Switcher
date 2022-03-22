# LiveSwitch-Video-Switcher
LiveSwitch Console Based Video Switcher. Teaches you how to make a man-in-the-middle liveswich console application that can take multiple feeds in and switch between them to a single output. 

In my testing this adds around 120ms of latency from my location, however this can be minimized by locating the application in AWS in the same region as LiveSwitch Cloud. 

## How it works:

1. LiveSwitch SDK(s) -> pushing feeds to a channel (SFU).
2. Application connects to the same channel and pulls down these feeds.
3. Application switches between the feeds and outputs them to a single broadcast feed (SFU / Media ID).


