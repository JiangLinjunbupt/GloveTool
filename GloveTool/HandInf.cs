using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloveLib;
using Newtonsoft.Json;

namespace GloveTool
{
    public class Finger
    {
        public float[] Length { get; set; }
        public float Mcp_x { get; set; }
        public float Mcp_z { get; set; }
        public float Pip { get; set; }
        public float Dip { get; set; }

        
        public Finger()
        {
            Length = new float[4];
            for(int i = 0;i<4;i++)
            {
                Length[i] = 0;
            }
            Mcp_x = 0;
            Mcp_z = 0;
            Pip = 0;
            Dip = 0;
        }

    }

    public class HandInf
    {
        public float global_x { get; set; }
        public float global_y { get; set; }
        public float global_z { get; set; }
        public float global_yaw_z { get; set; }
        public float global_pitch_y { get; set; }
        public float global_roll_x { get; set; }
        public Finger[] fingers { get; set; }

        private static HandInf handinf;
        public HandInf()
        {
            fingers = new Finger[5];
            for(int i = 0;i<5;i++)
            {
                fingers[i] = new Finger();
            }
            global_x = 0;
            global_y = 0;
            global_z = 0;
            global_roll_x = 0;
            global_pitch_y = 0;
            global_yaw_z = 0;
        }
        public static HandInf GetSingleton()
        {
            if (handinf == null)
            {
                handinf = new HandInf();
            }
            return handinf;
        }

        public string ToJson()
        {
            if(handinf != null)
            {
                return JsonConvert.SerializeObject(handinf);
            }
            else
            {
                handinf = new HandInf();
                return JsonConvert.SerializeObject(handinf);
            }
            
        }
    


    }
}
