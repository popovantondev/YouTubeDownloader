// Meter-only DSP. Does not change the export or the audio processing graph.
using System;
namespace PeakMp3Win7
{
    // Classic analog meter: amplitude-domain movement and piecewise angle calibration.
    // Kept independent from audio DSP and tested against the user's reference component.
    internal sealed class AnalogMeterChannel
    {
        internal const float MinLevel=.1f, ZeroLevel=1f, MaxLevel=1.4125375747680664f;
        internal float Angle, Level, Delta, Target;
        internal void Tick(float peak)
        {
            if(float.IsNaN(peak)||float.IsInfinity(peak))peak=0;
            peak=Math.Max(MinLevel,Math.Min(MaxLevel,peak));
            float epsilon=Math.Max(1f,Math.Min(Math.Abs(peak),Math.Abs(Target)))*1e-4f;
            if(Math.Abs(peak-Target)>epsilon)
            {
                Target=peak;
                Delta=(float)(Math.Exp((double)peak-1.0)*(peak<Level?-(double).05f:(double).1f));
            }
            Level=Delta>0?Math.Min((float)(Level+Delta),Target):Math.Max((float)(Level+Delta),Target);
            float bounded=Math.Max(MinLevel,Math.Min(MaxLevel,Level));
            Angle=bounded<ZeroLevel
                ?(float)(72.0*(bounded-(double)MinLevel)/(ZeroLevel-(double)MinLevel)-52.0)
                :(float)(32.0*(bounded-(double)ZeroLevel)/(MaxLevel-(double)ZeroLevel)+20.0);
        }
    }
    internal sealed class MeterEnvelope
    {
        internal const int FramesPerSecond=32;
        internal static int SamplesInFrame(int number)
        {return (int)(((long)number+1)*44100/FramesPerSecond-(long)number*44100/FramesPerSecond);}
        private readonly AnalogMeterChannel left=new AnalogMeterChannel(),right=new AnalogMeterChannel();
        internal double Left { get { return left.Level; } }
        internal double Right { get { return right.Level; } }
        internal double AngleLeft { get { return left.Angle; } }
        internal double AngleRight { get { return right.Angle; } }
        internal void Push(double peakL,double energyL,double peakR,double energyR,double seconds)
        {
            // Each visual frame uses channel peaks; RMS belongs only to the unchanged audio engine.
            left.Tick((float)peakL);right.Tick((float)peakR);
        }
        internal static double Db(double amplitude){return 20*Math.Log10(Math.Max(1e-9,amplitude));}
        internal static double Interpolate(double from,double to,double fraction)
        {return from+(to-from)*Math.Max(0,Math.Min(1,fraction));}
    }
    internal sealed class MeterFrame
    {
        internal long Start, End;
        internal float[] Samples;
    }
    internal static class ReferencePeakWindow
    {
        internal const int SampleCount=512;
        // Absolute channel maxima of 512 consecutive frames, without RMS weighting.
        internal static void Measure(float[] stereo,int offset,int frames,ref float left,ref float right)
        {
            int end=Math.Min(stereo.Length,2*(offset+frames));
            for(int i=2*offset;i<end;i+=2)
            {
                left=Math.Max(left,Math.Abs(stereo[i]));
                right=Math.Max(right,Math.Abs(stereo[i+1]));
            }
        }
    }
}
