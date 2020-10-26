using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AudioSynthesis.Synthesis;

namespace TestSynthThings
{
    public class SynthTransitioner
    {
        private Thread activeFadeInThread;
        private ConcurrentDictionary<Thread, Synthesizer> fadeOutThreads;

        private Synthesizer activeSynthesizer;

        public SynthTransitioner()
        {
            fadeOutThreads = new ConcurrentDictionary<Thread, Synthesizer>();
        }



        public void SetActiveSynth(Synthesizer newSynth)
        {
            var fadeIn = new Thread(FadeIn);

            activeFadeInThread = fadeIn;

            //if synth is being faded out, stop that
            foreach (var value in fadeOutThreads.Where(x => x.Value == newSynth))
            {
                fadeOutThreads.Remove(value.Key, out _);
            }

            if (activeSynthesizer != null)
            {
                var fadeOut = new Thread(FadeOut);

                fadeOut.Start(new FadeParams()
                {
                    Synthesizer = activeSynthesizer
                });

                fadeOutThreads.TryAdd(fadeOut, activeSynthesizer);
            }

            fadeIn.Start(new FadeParams()
            {
                Synthesizer = newSynth
            });

            activeSynthesizer = newSynth;
        }


        //todo implement speed of fading
        private void FadeOut(object fadeParams)
        {
            var fp = (FadeParams)fadeParams;

            if (fp.Synthesizer.MasterVolume.Equals(0)) return;



            var initialVolume = fp.Synthesizer.MasterVolume;

            for (var i = 0; i < 1000 *(1-initialVolume)   && fadeOutThreads.ContainsKey(Thread.CurrentThread); i++)
            {
                fp.Synthesizer.MasterVolume = initialVolume - i / 1000f;
                Thread.Sleep(5);
            }

            fadeOutThreads.Remove(Thread.CurrentThread, out _);
        }

        private void FadeIn(object fadeParams)
        {
            var fp = (FadeParams)fadeParams;

            if (fp.Synthesizer.MasterVolume.Equals(1)) return;

            var initialVolume = fp.Synthesizer.MasterVolume;

            for (var i = 0; i < 1000 * (1-initialVolume) && activeFadeInThread == Thread.CurrentThread; i++)
            {
                fp.Synthesizer.MasterVolume = initialVolume + i / 1000f;
                Thread.Sleep(5);
            }
        }


        private class FadeParams
        {
            public Synthesizer Synthesizer { get; set; }
        }
    }
}
