using System;
using System.Collections.Generic;
using AudioSynthesis.Sequencer;
using AudioSynthesis.Synthesis;
using DirectSoundDemo;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TestSynthThings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public class PadController : IDisposable
    {
        private int sampleRate = 32000;

        private const string bankPath =
            @".\Assets\Patches\TestBank\testbank.bank";

        private Synthesizer[] synths;

        private int currentSynth;

        private MixingSampleProvider mixer;

        private SynthTransitioner synthTransitioner;

        private IWavePlayer soundOut;
        
        public PadController()
        {
            synths = new Synthesizer[4];

            var inputs = new List<ISampleProvider>();
            synthTransitioner = new SynthTransitioner();

            for (int i = 0; i < synths.Length; i++)
            {
                synths[i] = new Synthesizer(sampleRate, 1);
                synths[i].LoadBank(bankPath);

                var provider = new SynthWaveProvider(synths[i], new MidiFileSequencer(synths[i]));
                provider.state = SynthWaveProvider.PlayerState.Playing;

                inputs.Add(provider.ToSampleProvider());
            }

            mixer = new MixingSampleProvider(inputs);

#if WINDOWS_UWP
            soundOut = new WasapiOutRT(AudioClientShareMode.Shared, 10);
            soundOut.Init(mixer.ToWaveProvider());
#else
            soundOut = new WasapiOut();
            soundOut.Init(mixer);
#endif
        }

        public void StartPad(int key)
        {
            if (soundOut.PlaybackState != PlaybackState.Playing)
            {
                soundOut.Play();
            }

            var newSynth = synths[currentSynth];

            newSynth.NoteOffAll(false);
            newSynth.MasterVolume = 0;

            newSynth.Programs[0] = 0;

            newSynth.NoteOn(0, key, 127);

            synthTransitioner.SetActiveSynth(newSynth);

            if (currentSynth++ == synths.Length) currentSynth = 0;
        }

        public void Dispose()
        {
            soundOut.Stop();
            soundOut.Dispose();

            foreach (var synth in synths)
            {
                synth.UnloadBank();
            }
        }
    }
}
