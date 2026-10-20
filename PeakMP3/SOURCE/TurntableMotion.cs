using System;
namespace PeakMp3Win7
{
    // Independent visual mechanics. Audio position and samples are never modified here.
    internal sealed class TurntableMotion
    {
        internal double Rotation,Speed,ArmAngle=-16,Lift=1;
        internal string Phase="parked";
        private string track="";
        private double previousProgress;
        private bool returnFirst;
        internal bool ReadyForAudio {get{return Phase=="tracking";}}
        internal bool Animating {get{return Speed>.02||Phase!="parked"&&Phase!="tracking";}}
        internal double Rock {get{return Math.Sin(Rotation*Math.PI/180)*1.0*Math.Min(1,Speed/200);}}
        internal void Advance(double dt,bool requested,double progress,string key)
        {
            dt=Math.Max(0,Math.Min(.1,dt));progress=Math.Max(0,Math.Min(1,progress));
            if(key!=track){track=key;returnFirst=true;Phase="lifting";}
            if(requested&&Math.Abs(progress-previousProgress)>.035&&Phase=="tracking")Phase="lifting-seek";
            previousProgress=progress;
            double targetSpeed=requested?200:0,rate=requested?420:360;
            Speed=Approach(Speed,targetSpeed,rate*dt);Rotation=(Rotation+Speed*dt)%360;
            if(!requested&&Phase!="parked"&&Phase!="lifting"&&Phase!="returning")Phase="lifting";
            if(Phase=="parked"&&requested){returnFirst=false;Phase="positioning";}
            if(Phase=="lifting"||Phase=="lifting-seek")
            {Lift=Approach(Lift,1,7*dt);if(Lift>=1){if(Phase=="lifting-seek"&&requested&&!returnFirst)Phase="positioning";else Phase="returning";}}
            else if(Phase=="returning")
            {ArmAngle=Approach(ArmAngle,-16,80*dt);if(ArmAngle<=-16){returnFirst=false;Phase=requested?"positioning":"parked";}}
            else if(Phase=="positioning")
            {double target=5+19*progress;ArmAngle=Approach(ArmAngle,target,85*dt);if(Math.Abs(ArmAngle-target)<.01)Phase="lowering";}
            else if(Phase=="lowering")
            {Lift=Approach(Lift,0,6*dt);if(Lift<=0)Phase="tracking";}
            else if(Phase=="tracking")
            {ArmAngle=Approach(ArmAngle,5+19*progress,30*dt);Lift=0;}
        }
        internal void BeginSeek(){if(Phase=="tracking")Phase="lifting-seek";}
        private static double Approach(double a,double b,double amount){return a<b?Math.Min(b,a+amount):Math.Max(b,a-amount);}
    }
}
