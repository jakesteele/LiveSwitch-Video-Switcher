using System;
using System.Collections.Generic;
using FM.LiveSwitch;
using FM.LiveSwitch.Sdp;

namespace MITM
{
    public class Mixer
    {

        List<(string, VideoTrack)> tracks;

        private string _activeTrack = string.Empty;

        private NullVideoSource _activeSource = null;
        private NullVideoSink _activeSink = null;

        public void AddTrack(string name, VideoTrack track)
        {
            tracks.Add((name, track));
            if (tracks.Count == 1)
            {
                SetInput(track);
            }
        }

        public void RemoveTrack(string name)
        {
            if (String.Equals(name, _activeTrack, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("You can not remove the active track.");
            }
            for (int i = 0, l = tracks.Count; i < l; i++)
            {
                var track = tracks[i];
                if (String.Equals(track.Item1, name, StringComparison.OrdinalIgnoreCase))
                {
                    tracks.Remove(track);
                }
            }

        }

        public VideoTrack GetTrackByName(string name)
        {
            for (int i = 0, l = tracks.Count; i < l; i++)
            {
                var track = tracks[i];
                if (String.Equals(track.Item1, name, StringComparison.OrdinalIgnoreCase))
                {
                    return track.Item2;
                }
            }
            return default(VideoTrack);
        }

        public void SetOutput(VideoTrack  track)
        {
            SetOutput((NullVideoSource)track.Source);
        }

        public void SetOutput(NullVideoSource source)
        {
            _activeSource = source;
        }

        public void SetInput(NullVideoSink sink)
        {
            // Dewire the existing events.
            if (_activeSink != null)
            {
                _activeSink.OnProcessFrame -= ProcessFrame;
            }

            _activeSink = sink;
            _activeSink.OnProcessFrame += ProcessFrame;
            _needsKeyFrame = true;
        }

        public void SetInput(string name)
        {
            if (name == _activeTrack)
            {
                return;
            }
            var track = GetTrackByName(name);
            if (track != null)
            {
                SetInput((NullVideoSink)track.Sink);
            }
        }


        public bool _needsKeyFrame = false;
        public void ProcessFrame(VideoFrame frame)
        {
            if (_needsKeyFrame)
            {
                ((FM.LiveSwitch.Vp8.Encoder)_activeSource.Output).ForceKeyFrame = true;
                _needsKeyFrame = false;
            }
            if (_activeSource != null)
            {
                _activeSource.ProcessFrame(frame);
            }
        }

        public void SetInput(VideoTrack track)
        {
            if (track != null)
            {
                SetInput((NullVideoSink)track.Sink);
            }
        }

        public Mixer()
        {
            tracks = new List<(string, VideoTrack)>();
        }
        
    }
}
