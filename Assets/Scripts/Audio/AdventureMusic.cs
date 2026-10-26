using UnityEngine;
namespace TreasureRunner
{
    // Original score: eight instrument palettes, 80 deterministic arrangements.
    public static class AdventureMusic
    {
        public static int Seed(string value){unchecked{int hash=17;foreach(char c in value)hash=hash*31+c;return hash&0x7fffffff;}}
        public static AudioClip Compose(int id)
        {
            int seed=id<0 ? 927 : id*7919+1237;var random=new System.Random(seed);
            int world=id<0 ? 0 : CampaignCatalog.World(id%80),stage=id<0 ? 0 : CampaignCatalog.Stage(id%80);
            float bpm=104+world*3+stage%4*4,beat=60/bpm;const int rate=22050;
            int count=Mathf.RoundToInt(beat*64*rate);var samples=new float[count];
            int[] scale={0,2,4,7,9,12,14,16};int[] phrase=new int[16];for(int i=0;i<16;i++)phrase[i]=scale[random.Next(scale.Length)];
            int[] progression={0,5,7,0,9,5,7,7};int key=48+(world*5+stage*2)%12;
            float[] melody=new float[128],bass=new float[16],third=new float[16],fifth=new float[16];
            for(int i=0;i<128;i++)melody[i]=Hz(key+12+phrase[(i+(i/32)%3)%16]+(i/16%4==3 ? -12 : 0));
            for(int i=0;i<16;i++){int root=key+progression[i%8];bass[i]=Hz(root-12);third[i]=Hz(root+4);fifth[i]=Hz(root+7);}
            uint noise=12345;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate,within=t%(beat*.5f),p=t%beat;int step=Mathf.Min(127,(int)(t/(beat*.5f))),bar=Mathf.Min(15,(int)(t/(beat*4)));
                float lead=Wave(melody[step],within,world)*Mathf.Exp(-within*(world==1 ? 8 : 12))*.115f;
                float low=Mathf.Sin(2*Mathf.PI*bass[bar]*p)*Mathf.Exp(-p*5)*.13f;
                float chord=(Mathf.Sin(2*Mathf.PI*third[bar]*t)+Mathf.Sin(2*Mathf.PI*fifth[bar]*t))*.025f;
                float kick=Mathf.Sin(2*Mathf.PI*(65*p+35*(1-Mathf.Exp(-p*30))/30))*Mathf.Exp(-p*32)*.13f;
                noise=noise*1664525+1013904223;float hat=((noise>>8)/8388607f-1)*Mathf.Exp(-within*75)*.025f;
                float fade=Mathf.Min(1,Mathf.Min(t/.06f,(count/(float)rate-t)/.06f));samples[i]=(lead+low+chord+kick+hat)*fade;
            }
            var clip=AudioClip.Create("TreasureRunner original · "+id+" · "+bpm+" BPM",count,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static float Hz(int midi)=>440*Mathf.Pow(2,(midi-69)/12f);
        static float Wave(float f,float t,int world)=>Mathf.Sin(2*Mathf.PI*f*t)+Mathf.Sin(4*Mathf.PI*f*t)*(.12f+world*.02f);
    }
}
