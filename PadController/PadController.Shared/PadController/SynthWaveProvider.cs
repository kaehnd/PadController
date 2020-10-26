using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Utils;
using AudioSynthesis.Synthesis;
using AudioSynthesis.Sequencer;

namespace DirectSoundDemo
{
    public class SynthWaveProvider : IWaveProvider
    {
        public enum MessageType { Synth, Midi };
        public enum PlayerState { Stopped, Paused, Playing };
        public struct Message
        {
            public MessageType type;
            public int channel;
            public int command;
            public int data1;
            public int data2;

            public override string ToString()
            {
                return "Type: " + Enum.GetName(typeof(MessageType), type) + " Channel: " + channel + " Command: " + command + " P1: " + data1 + " P2: " + data2;
            }
        }

        public volatile PlayerState state;
        public Queue<Message> msgQueue = new Queue<Message>();
        public volatile object lockObj = new object();
        private CircularBuffer circularBuffer;
        private byte[] sbuff;
        private Synthesizer synth;
        private MidiFileSequencer mseq;

        public delegate void UpdateTime(int current, int max);
        public event UpdateTime TimeUpdate;
        public delegate void UpdateTrackBars(int[] ccList);
        public event UpdateTrackBars UpdateMidiControllers;

        public SynthWaveProvider(Synthesizer synth, MidiFileSequencer mseq)
        {
            this.synth = synth;
            this.mseq = mseq;

            //WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(synth.SampleRate, synth.AudioChannels);

            WaveFormat = new WaveFormat(synth.SampleRate, 16, synth.AudioChannels);
            int bufferSize = (int)Math.Ceiling((2.0 * WaveFormat.AverageBytesPerSecond) / synth.RawBufferSize) * synth.RawBufferSize;
            circularBuffer = new CircularBuffer(bufferSize);
            sbuff = new byte[synth.RawBufferSize];
            state = PlayerState.Stopped;
        }
        public WaveFormat WaveFormat { get; }

        public int Read(byte[] buffer, int offset, int count)
        {
            //samples processing


            if (state != PlayerState.Playing)
            {
                if (state == PlayerState.Stopped)
                    return 0;
                else
                {
                    Array.Clear(buffer, offset, buffer.Length - offset);
                    return count;
                }
            }
            var ccList = new int[16];
            while (circularBuffer.Count < count)
            {
                lock (lockObj)
                {
                    if (msgQueue.Count > 0)
                    {
                        while (msgQueue.Count > 0)
                            processMessage(msgQueue.Dequeue());
                    }
                    synth.GetNext(sbuff);
                    circularBuffer.Write(sbuff, 0, sbuff.Length);
                }
            }
            if (TimeUpdate != null)
                TimeUpdate(mseq.CurrentTime, mseq.EndTime);
            if (UpdateMidiControllers != null)
                UpdateMidiControllers(ccList);
            return circularBuffer.Read(buffer, offset, count);
        }
        public void Reset()
        {
            circularBuffer.Reset();
        }
        private void processMessage(Message msg)
        {
            if (msg.type == MessageType.Synth)
            {
                switch (msg.command)
                {
                    case 10:
                        synth.MasterVolume = msg.data1 / (float)msg.data2;
                        break;
                    case 15:
                        mseq.Seek(new TimeSpan(0, 0, msg.data1));
                        break;
                    case 20:
                        mseq.SetMute(msg.channel, true);
                        break;
                    case 21:
                        mseq.SetMute(msg.channel, false);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                synth.ProcessMidiMessage(msg.channel, msg.command, msg.data1, msg.data2);
            }
        }
    }
}
