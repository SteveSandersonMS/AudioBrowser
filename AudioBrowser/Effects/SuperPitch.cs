// .NET port of Super-pitch JS effect included with Cockos REAPER
using System;
using System.Diagnostics;
using System.Threading;

namespace SkypeVoiceChanger.Effects
{
    public class SuperPitch : Effect
    {
        int bufsize;
        float xfade;
        int bufloc0;
        int buffer0;
        float pitch;

        float denorm;
        bool filter;
        float v0;
        float h01, h02, h03, h04;
        float a1, a2, a3, b1, b2;
        float t0;
        float drymix;
        float wetmix;
        float[] buffer = new float[64000];

        public SuperPitch()
        {
            AddSlider(0, -100, 100, 1, "Pitch adjust (cents)");
            AddSlider(-10, -12, 12, 1, "Pitch adjust (semitones)");
            AddSlider(0, -8, 8, 1, "Pitch adjust (octaves)"); // mrh 12 octaves up causes issues, reigning this in a bit
            AddSlider(50, 1, 200, 1, "Window size (ms)"); // mrh minimum window size set to 1 as 0 seems to cause issues
            AddSlider(20, 0.05f, 50, 0.5f, "Overlap size (ms)");
            AddSlider(0, -120, 6, 1, "Wet mix (dB)");
            AddSlider(-120, -120, 6, 1, "Dry mix (dB)");
            Slider filterSlider = AddSlider(1, 0, 1, 1, "Filter"); // {NO,YES}filter

            filterSlider.DiscreteValueText.Add("Off");
            filterSlider.DiscreteValueText.Add("On");

        }

        public override string Name
        {
            get { return "SuperPitch"; }
        }

        public override void Init()
        {
            bufsize = (int)SampleRate; // srate|0;
            xfade = 100;
            bufloc0 = 10000;

            buffer0 = bufloc0;
            pitch = 1.0f;
            denorm = pow(10, -20);
            base.Init();
        }

        protected override void Slider()
        {
            filter = slider8 > 0.5;
            int bsnew = (int)(Math.Min(slider4, 1000) * 0.001 * SampleRate);
            //   bsnew=(min(slider4,1000)*0.001*srate)|0;
            if (bsnew != bufsize)
            {
                bufsize = bsnew;
                v0 = buffer0 + bufsize * 0.5f;
                if (v0 > bufloc0 + bufsize)
                {
                    v0 -= bufsize;
                }
            }

            xfade = (int)(slider5 * 0.001 * SampleRate);
            if (xfade > bsnew * 0.5)
            {
                xfade = bsnew * 0.5f;
            }

            float npitch = pow(2, ((slider2 + slider1 * 0.01f) / 12 + slider3));
            if (pitch != npitch)
            {
                pitch = npitch;
                float lppos = (pitch > 1.0f) ? 1.0f / pitch : pitch;
                if (lppos < (0.1f / SampleRate))
                {
                    lppos = 0.1f / SampleRate;
                }
                float r = 1.0f;
                float c = 1.0f / tan(PI * lppos * 0.5f);
                a1 = 1.0f / (1.0f + r * c + c * c);
                a2 = 2 * a1;
                a3 = a1;
                b1 = 2.0f * (1.0f - c * c) * a1;
                b2 = (1.0f - r * c + c * c) * a1;
                h01 = h02 = h03 = h04 = 0;
            }

            drymix = pow(2, (slider7 / 6));
            wetmix = pow(2, (slider6 / 6));
        }

        protected override void Sample(ref float spl0)
        {
            int iv0 = (int)(v0);
            float frac0 = v0 - iv0;
            int iv02 = (iv0 >= (bufloc0 + bufsize - 1)) ? iv0 - bufsize + 1 : iv0 + 1;

            float ren0 = (buffer[iv0 + 0] * (1 - frac0) + buffer[iv02 + 0] * frac0);
            float vr = pitch;
            float tv, frac, tmp, tmp2;
            if (vr >= 1.0)
            {
                tv = v0;
                if (tv > buffer0) tv -= bufsize;
                if (tv >= buffer0 - xfade && tv < buffer0)
                {
                    // xfade
                    frac = (buffer0 - tv) / xfade;
                    tmp = v0 + xfade;
                    if (tmp >= bufloc0 + bufsize) tmp -= bufsize;
                    tmp2 = (tmp >= bufloc0 + bufsize - 1) ? bufloc0 : tmp + 1;
                    ren0 = ren0 * frac + (1 - frac) * (buffer[(int)tmp + 0] * (1 - frac0) + buffer[(int)tmp2 + 0] * frac0);
                    if (tv + vr > buffer0 + 1) v0 += xfade;
                }
            }
            else
            {// read pointer moving slower than write pointer
                tv = v0;
                if (tv < buffer0) tv += bufsize;
                if (tv >= buffer0 && tv < buffer0 + xfade)
                {
                    // xfade
                    frac = (tv - buffer0) / xfade;
                    tmp = v0 + xfade;
                    if (tmp >= bufloc0 + bufsize) tmp -= bufsize;
                    tmp2 = (tmp >= bufloc0 + bufsize - 1) ? bufloc0 : tmp + 1;
                    ren0 = ren0 * frac + (1 - frac) * (buffer[(int)tmp + 0] * (1 - frac0) + buffer[(int)tmp2 + 0] * frac0);
                    if (tv + vr < buffer0 + 1) v0 += xfade;
                }
            }


            if ((v0 += vr) >= (bufloc0 + bufsize)) v0 -= bufsize;

            float os0 = spl0;
            if (filter && pitch > 1.0)
            {

                t0 = spl0;
                spl0 = a1 * spl0 + a2 * h01 + a3 * h02 - b1 * h03 - b2 * h04 + denorm;
                h02 = h01; h01 = t0;
                h04 = h03; h03 = spl0;
            }


            buffer[buffer0 + 0] = spl0; // write after reading it to avoid clicks

            spl0 = ren0 * wetmix;

            if (filter && pitch < 1.0)
            {
                t0 = spl0;
                spl0 = a1 * spl0 + a2 * h01 + a3 * h02 - b1 * h03 - b2 * h04 + denorm;
                h02 = h01; h01 = t0;
                h04 = h03; h03 = spl0;
            }

            spl0 += os0 * drymix;

            if ((buffer0 += 1) >= (bufloc0 + bufsize)) buffer0 -= bufsize;

        }
    }
}