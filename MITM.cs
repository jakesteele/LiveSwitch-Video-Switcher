using System;
using System.Threading;
using System.Threading.Tasks;
using FM.LiveSwitch;

namespace MITM
{
    public static class Config
    {
        //Edit these values first.
        public static string applicationId = "";
        public static string gatewayURL = "https://cloud.liveswitch.io/";
        public static string sharedSecret = "";

        // TODO: 
        // | ChannelLeft | -> MITM -> | ChannelRight |
        // ACTUAL:
        // | Main Channel Cameras | -> MITM -> Main Channel Broadcast Feed
        public static string channelLeft = "testchannel";
        //public static string channelRight = "rightchannel";

        public static string outputMediaId = "mainfeed";
        public static string inputMediaIdBse = "camera-";
        public static int numberOfCameras = 2;
    }

    public static class Extensions
    {
        public static Channel FindById(this FM.LiveSwitch.Channel[] array, string needle)
        {
            for (int i = 0, l = array.Length; i < l; i++)
            {
                Channel c = array[i];
                if (String.Equals(c.Id, needle, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }
            return default(Channel);
        }
    }

    public class MITM
    { 
        public Task<Channel[]> RegisterFakeClient()
        {
            var client = new Client(Config.gatewayURL, Config.applicationId);
            var token = Token.GenerateClientRegisterToken(client
                , new ChannelClaim[]
                {
                    new ChannelClaim(Config.channelLeft),
                    //new ChannelClaim(Config.channelRight)
                }
                , Config.sharedSecret);
            return client.Register(token).AsTask();
        }

        public VideoTrack GetDecodeVideoPipeline()
        {
            // I am only going to use VP8 for this example.

            var sink = new NullVideoSink(VideoFormat.I420);
            return new FM.LiveSwitch.VideoTrack(new FM.LiveSwitch.Vp8.Depacketizer())
                .Next(new FM.LiveSwitch.Vp8.Decoder())
                .Next(sink);
        }

        public VideoTrack GetEncodeVideoPipeline()
        {
            var source = new NullVideoSource(VideoFormat.I420);
            var track = new FM.LiveSwitch.VideoTrack(source)
                .Next(new FM.LiveSwitch.Vp8.Encoder())
                .Next(new FM.LiveSwitch.Vp8.Packetizer());
            source.Start();
            return track;
        }

        public async Task<SfuDownstreamConnection> OpenDownstreamConnection(Channel channel, VideoTrack track, string mediaId)
        {
            var videoStream = new VideoStream(null, track);
            var conn = channel.CreateSfuDownstreamConnection(mediaId, videoStream);
            await conn.Open();
            return conn;
        }

        public async Task<SfuUpstreamConnection> OpenUpstreamConnection(Channel channel, Mixer m)
        {
            var track = GetEncodeVideoPipeline();
            m.SetOutput(track);
            var videoStream = new VideoStream(track, null);
            var conn = channel.CreateSfuUpstreamConnection(videoStream, Config.outputMediaId);
            await conn.Open();
            return conn;
        }

        public async Task Run()
        {
            FM.LiveSwitch.Log.RegisterProvider(new FM.LiveSwitch.ConsoleLogProvider(LogLevel.Debug));

            var channels = await RegisterFakeClient();
            // I know the order of the channels, Left is 0 and Right is 1, but I will loop just to pretend I don't.

            // The left channel we receive from.
            var leftChannel = channels.FindById(Config.channelLeft);
            // The right channel we send to.
            //var rightChannel = channels.FindById(Config.channelRight);

            // Create our mixer class.
            var mixer = new Mixer();

            // Open the Upstream.
            var upstreamConnection = OpenUpstreamConnection(leftChannel, mixer);


            // Open the Downstream(s).
            (int, SfuConnection)[] cameraConnections = new (int, SfuConnection)[Config.numberOfCameras];
            for (int i = 0, l = Config.numberOfCameras; i < l; i++)
            {
                var track = GetDecodeVideoPipeline();
                var name = $"{Config.inputMediaIdBse}{i}";
                mixer.AddTrack(name, track);
                cameraConnections[i] = new(i, await OpenDownstreamConnection(leftChannel, track, name));
            }

            bool _run = true;
            int _activeCamera = 0;
            Thread thread = new Thread(() => {
                while (_run)
                {
                    
                    Console.WriteLine("Spacebar to  switch cameras, ESC to exit.");
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Spacebar)
                    {
                        _activeCamera++;
                        if (_activeCamera >= Config.numberOfCameras)
                        {
                            _activeCamera = 0;
                        }
                        Console.WriteLine($"Switching camera to: { Config.inputMediaIdBse}{ _activeCamera}");
                        mixer.SetInput($"{Config.inputMediaIdBse}{_activeCamera}");
                    }
                    else if (key == ConsoleKey.Escape)
                    {
                        _run = false;
                    }
                }
            });
            thread.Start();
            
        }
    }
}
