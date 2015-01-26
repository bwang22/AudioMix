using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioMix
{
    public class MixerControls
    {
        public static Dictionary<int, List<int>> ToDict(List<int[]> fclus)
        {
            Dictionary<int, List<int>> cluspos = new Dictionary<int, List<int>>();
            for(int a = 0; a < fclus.Count; a++)
            {
                if(!cluspos.ContainsKey(fclus[a][1]))
                {
                    cluspos.Add(fclus[a][1], new List<int>());
                    cluspos[fclus[a][1]].Add(fclus[a][0]);
                }
                else
                {
                    cluspos[fclus[a][1]].Add(fclus[a][0]);
                }
            }
            return cluspos;
        }
        public static long Jump(Dictionary<int, List<int>> cluspos, long currentpos, List<int> peaks, int[] peakclus, ref int nextvalue, ref int currentposs)
        {
            long startpos = currentpos / 2;
            int pos = (int)startpos / 512;
            if (nextvalue < peaks[currentposs])
                nextvalue = peaks[currentposs];
            if(nextvalue - pos > 0)
            {
                if(nextvalue - pos < 50)//pretty close
                {
                    pos = nextvalue;
                }
            }
            else
            {
                currentposs++;
            }
            int index = peaks.IndexOf(pos);
            
            if (index != -1)
            {
                index = peakclus[index];
                Random r = new Random();
                if (r.Next(0, 100) > 25)//25% jump
                {
                    int random = r.Next(0, cluspos[index].Count);
                    int loc = cluspos[index][random];
                    currentposs = peaks.IndexOf(loc);
                    return (long)(loc * 512 * 2);
                }
            }
            return currentpos;
        }
    }
}
