using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloveLib;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Media.Media3D;

namespace GloveTool
{
    public class GloveModel
    {
        private static byte[] result = new byte[4096];
        private static int myProt = 10200;//端口
        public static Socket serverSocket;
        public IPAddress ip = IPAddress.Parse("127.0.0.1");

        private String chuankou;
        private Boolean Socketclose = false;
        public GloveController gc;
        private DataWarehouse dh;
        private SkeletonCalculator sc;
        private Rehabilitation rhb;
        //private Timer pullDataTimer;
        public SkeletonJson fram;
        public string skeletonJson;
        public HandInf handinformation;

        public HandInf OptimizedData;

        public static HandType handType = HandType.Right; //左右手

        private static GloveModel Instance;
        public static GloveModel GetSingleton()
        {
            if (Instance == null)
            {
                Instance = new GloveModel();
            }
            return Instance;
        }
        public static void DestoryInstance()
        {
            Instance = null;
        }
        public String Mychuankou
        {
            get
            {
                return chuankou;
            }

            set
            {
                chuankou = value;
            }

        }

        public Boolean CloseSocket
        {
            get
            {
                return Socketclose;
            }

            set
            {
                Socketclose = value;
            }
        }

        public GloveModel()
        {

            gc = GloveController.GetSingleton(ModelType.HandOnly);
            //            var mLogger = Logger.GetInstance(mw.txt_log);
            //            gc.RegisterLogger(mLogger);

            //if (!gc.IsConnected((int)handType)) //接入手套
            //{
            //    gc.Connect(chuankou, 0);       //连接手套和串口
            //}
            rhb = Rehabilitation.GetSingleton();
            dh = DataWarehouse.GetSingleton();
            sc = SkeletonCalculator.GetSingleton("");
            handinformation = HandInf.GetSingleton();

            OptimizedData = HandInf.GetSingleton();

            fram = new SkeletonJson();
            Mychuankou = "COM3";

            
            //pullDataTimer = new Timer(500);
            //pullDataTimer.Elapsed += pullDataTimer_Tick;
            //pullDataTimer.Start();

        }
        public void Conected()
        {
            if (!gc.IsConnected((int)handType)) //接入手套
            {
                gc.Connect(chuankou, 0);       //连接手套和串口
            }
        }
        public void GetData()
        {

            try
            {
                //send right
                var f_r = dh.GetFrameData(handType, Definition.MODEL_TYPE);

                //        public enum NodeType
                //{
                //    Wrist,
                //    Upperarm,
                //    Index_0,
                //    Index_1,
                //    Middle_0,
                //    Middle_1,
                //    Ring_0,
                //    Ring_1,
                //    Little_0, // 0 for near one, 1 for far one
                //    Little_1,
                //    Thumb_0,
                //    Thumb_1,
                //    Forearm,
                //    Palm
                //}


                //for (int i = 0; i < Definition.SENSOR_COUNT; i++)
                //{
                //    Console.WriteLine("{0} Node 'W is {1}", (NodeType)i, f_r.Nodes[i].X);
                //}


                //Console.WriteLine("{0} Node 'x is {1}", (NodeType)11, f_r.Nodes[11].X); 

                var s = sc.UpdateRaw(f_r);

                fram = s.ToSkeletonJson();
                skeletonJson = s.ToJson();

                handinformation = SkeletonJsonToHandinf(fram);
                //Console.WriteLine("the skeleton json is :");
                //Console.WriteLine(skeletonJson);
                //Console.WriteLine("the skeleton string is :");
                //Console.WriteLine(s.ToString());

                //Console.WriteLine(fram.Joints[(int)JointType.Hand].W);

                //Console.WriteLine(s.ToString());\


            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                return;
            }
        }
       
        public void SetSocket()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, myProt));  //绑定IP地址：端口  
            serverSocket.Listen(10);    //设定最多10个排队连接请求  
            Console.WriteLine("启动监听{0}成功", serverSocket.LocalEndPoint.ToString());
            Thread myThread = new Thread(SendSkeleton);
            myThread.Start();
           

            //clientSocket = serverSocket.Accept();
        }

        public void CloseGloveModel()
        {
            System.Environment.Exit(0);
        }
        public void SendSkeleton()
        {
            Socket clientSocket = serverSocket.Accept();
            while (true && !Socketclose)
            {
                try
                {
                    //GetData();
                    //sm.Send(skeletonJson);
                    clientSocket.Send(Encoding.ASCII.GetBytes(skeletonJson+ "<EOF>"+handinformation.ToJson() + "<EOF1>" + OptimizedData.ToJson() + "<EOF2>"));

                    //clientSocket.Send(Encoding.ASCII.GetBytes(skeletonJson + "<EOF>"));
                    //clientSocket.Send(Encoding.ASCII.GetBytes("hello world"));
                    System.Threading.Thread.Sleep(10);

                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
                   
            }
            clientSocket.Close();
        }

        public void SendHandInf()
        {
            if(handinformation != null)
            {
                //clientSocket.Send(Encoding.ASCII.GetBytes(handinformation.ToJson() + "<EOF>"));
            }
        }
        public Vector3D QtoE(Quaternion q)
        {
            var euler1 = new Vector3D();
            euler1.Z = Math.Atan2(2 * (q.W * q.Z + q.X * q.Y), 1 - 2 * (q.Z * q.Z + q.X * q.X));
            euler1.X = Math.Asin(2 * (q.W * q.X - q.Z * q.Y));
            euler1.Y = Math.Atan2(2 * (q.W * q.Y + q.X * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y));
            return Internal_MakePositive(euler1 * 180/3.14);
        }

        public Quaternion EtoQ(double ex,double ey,double ez)
        {
            float PIover180 = 0.0174532925f;
            Quaternion q = new Quaternion();

            ex = ex * PIover180 / 2.0f;
            ey = ey * PIover180 / 2.0f;
            ez = ez * PIover180 / 2.0f;

            q.X = Math.Sin(ex) * Math.Cos(ey) * Math.Cos(ez) + Math.Cos(ex) * Math.Sin(ey) * Math.Sin(ez);
            q.Y = Math.Cos(ex) * Math.Sin(ey) * Math.Cos(ez) - Math.Sin(ex) * Math.Cos(ey) * Math.Sin(ez);
            q.Z = Math.Cos(ex) * Math.Cos(ey) * Math.Sin(ez) - Math.Sin(ex) * Math.Sin(ey) * Math.Cos(ez);
            q.W = Math.Cos(ex) * Math.Cos(ey) * Math.Cos(ez) + Math.Sin(ex) * Math.Sin(ey) * Math.Sin(ez);

            return q;

        }
        public Vector3D QtoE_glove_to_model(Quaternion q)
        {
            var euler1 = new Vector3D();
            euler1.Z = Math.Asin(2 * (q.W * q.Z - q.Y * q.X));
            euler1.X = Math.Atan2(2 * (q.W * q.X + q.Y * q.Z), 1 - 2 * (q.Z * q.Z + q.X * q.X));
            euler1.Y = Math.Atan2(2 * (q.W * q.Y + q.Z * q.X), 1 - 2 * (q.Y * q.Y + q.Z * q.Z));
            return Internal_MakePositive(euler1 * 180 / 3.14);
        }

        private  Vector3D Internal_MakePositive(Vector3D euler)
        {
            float num = -0.005729578f;
            float num2 = 360f + num;
            if (euler.X < num)
            {
                euler.X += 360f;
            }
            else if (euler.X > num2)
            {
                euler.X -= 360f;
            }
            if (euler.Y < num)
            {
                euler.Y += 360f;
            }
            else if (euler.Y > num2)
            {
                euler.Y -= 360f;
            }
            if (euler.Z < num)
            {
                euler.Z += 360f;
            }
            else if (euler.Z > num2)
            {
                euler.Z -= 360f;
            }
            return euler;
        }

        private float RestrictAngle(float angle, float min, float max)
        {
            angle = angle > 180 ? angle - 360 : angle;
            if (angle <= max && angle >= min)
            {
                return angle;
            }
            var m = (min + max) / 2;//middle point
            if (m > 0)
            {//middle point on right
                if (angle >= m - 180 && angle < min)
                {
                    return min;
                }
                else
                    return max;
            }
            else
            {
                if (angle <= m + 180 && angle > max)
                {
                    return max;
                }
                else
                    return min;
            }
        }

        //public HandInf SkeletonJsonToHandinf(SkeletonJson fram)
        //{
        //    var handinf = HandInf.GetSingleton();
        //    //order psi->theta->phi
        //    // order z->y->x
        //    //handinf.global_roll_x = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).X;
        //    //handinf.global_pitch_y = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).Y;
        //    //handinf.global_yaw_z = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).Z;



        //    handinf.global_roll_x = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).Y;
        //    handinf.global_pitch_y = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).X;
        //    handinf.global_yaw_z = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).Z;

        //    for (int i = 0;i<5;i++)
        //    {
        //        if(i==0)
        //        {
        //            //因为这个地方的传感器原来对应着大拇指，只有2和3才有值。
        //            handinf.fingers[i].Mcp_x = (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 2].ToQuaternion()).Z - 180;
        //            handinf.fingers[i].Mcp_z = (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 2].ToQuaternion()).Y > 300? (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 2].ToQuaternion()).Y -360: (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 2].ToQuaternion()).Y;
        //            handinf.fingers[i].Pip = 180 - (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 3].ToQuaternion()).Z;
        //            handinf.fingers[i].Dip = 180 - (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 3].ToQuaternion()).Z;

        //            if(handinf.fingers[i].Mcp_x <= 90 && handinf.fingers[i].Mcp_x >=0 )
        //            {
        //                ;
        //            }
        //            else
        //            {
        //                if(handinf.fingers[i].Mcp_x >90)
        //                {
        //                    handinf.fingers[i].Mcp_x = 90;
        //                }
        //                else
        //                {
        //                    handinf.fingers[i].Mcp_x = 0;
        //                }
        //            }

        //            if (handinf.fingers[i].Mcp_z <= 10 && handinf.fingers[i].Mcp_z >= -30)
        //            {
        //                ;
        //            }
        //            else
        //            {
        //                if (handinf.fingers[i].Mcp_z > 10)
        //                {
        //                    handinf.fingers[i].Mcp_z = 10;
        //                }
        //                else
        //                {
        //                    handinf.fingers[i].Mcp_z = -30;
        //                }
        //            }

        //            if (handinf.fingers[i].Pip <= 90 && handinf.fingers[i].Pip >= 0)
        //            {
        //                ;
        //            }
        //            else
        //            {
        //                if (handinf.fingers[i].Pip > 90)
        //                {
        //                    handinf.fingers[i].Pip = 90;
        //                }
        //                else
        //                {
        //                    handinf.fingers[i].Pip = 0;
        //                }
        //            }

        //            if (handinf.fingers[i].Dip <= 90 && handinf.fingers[i].Dip >= 0)
        //            {
        //                ;
        //            }
        //            else
        //            {
        //                if (handinf.fingers[i].Dip > 90)
        //                {
        //                    handinf.fingers[i].Dip = 90;
        //                }
        //                else
        //                {
        //                    handinf.fingers[i].Dip = 0;
        //                }
        //            }

        //            if(handinf.fingers[i].Mcp_x > 50)
        //            {
        //                handinf.fingers[i].Mcp_z = 0;
        //            }

        //        }
        //        else
        //        {
        //            handinf.fingers[i].Mcp_x = (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 1].ToQuaternion()).Z - 180;
        //            handinf.fingers[i].Mcp_z = (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 1].ToQuaternion()).Y >300 ? (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 1].ToQuaternion()).Y - 360: (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 1].ToQuaternion()).Y;
        //            handinf.fingers[i].Pip = 180 - (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 2].ToQuaternion()).Z;
        //            handinf.fingers[i].Dip = 180 - (float)QuaternionConverter.Quat2Euler(fram.Joints[i * 3 + 3].ToQuaternion()).Z;


        //            if (handinf.fingers[i].Mcp_x <= 90 && handinf.fingers[i].Mcp_x >= 0)
        //            {
        //                ;
        //            }
        //            else
        //            {
        //                if (handinf.fingers[i].Mcp_x > 90)
        //                {
        //                    handinf.fingers[i].Mcp_x = 90;
        //                }
        //                else
        //                {
        //                    handinf.fingers[i].Mcp_x = 0;
        //                }
        //            }

        //           if(i == 2 )
        //            {
        //                if (handinf.fingers[i].Mcp_z <= 10 && handinf.fingers[i].Mcp_z >= -10)
        //                {
        //                    ;
        //                }
        //                else
        //                {
        //                    if (handinf.fingers[i].Mcp_z > 10)
        //                    {
        //                        handinf.fingers[i].Mcp_z = 10;
        //                    }
        //                    else
        //                    {
        //                        handinf.fingers[i].Mcp_z = -10;
        //                    }
        //                }
        //            }
        //           else if(i == 3)
        //            {
        //                if (handinf.fingers[i].Mcp_z <= 30 && handinf.fingers[i].Mcp_z >= -10)
        //                {
        //                    ;
        //                }
        //                else
        //                {
        //                    if (handinf.fingers[i].Mcp_z > 30)
        //                    {
        //                        handinf.fingers[i].Mcp_z = 30;
        //                    }
        //                    else
        //                    {
        //                        handinf.fingers[i].Mcp_z = -10;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                ;
        //            }

        //            if (handinf.fingers[i].Pip <= 90 && handinf.fingers[i].Pip >= 0)
        //            {
        //                ;
        //            }
        //            else
        //            {
        //                if (handinf.fingers[i].Pip > 90)
        //                {
        //                    handinf.fingers[i].Pip = 90;
        //                }
        //                else
        //                {
        //                    handinf.fingers[i].Pip = 0;
        //                }
        //            }

        //            if (handinf.fingers[i].Dip <= 90 && handinf.fingers[i].Dip >= 0)
        //            {
        //                ;
        //            }
        //            else
        //            {
        //                if (handinf.fingers[i].Dip > 90)
        //                {
        //                    handinf.fingers[i].Dip = 90;
        //                }
        //                else
        //                {
        //                    handinf.fingers[i].Dip = 0;
        //                }
        //            }

        //            if(i != 4)
        //            {
        //                if (handinf.fingers[i].Mcp_x > 50)
        //                {
        //                    handinf.fingers[i].Mcp_z = 0;
        //                }
        //            }

        //        }

        //    }

        //    return handinf;
        //}

        public HandInf SkeletonJsonToHandinf(SkeletonJson fram)
        {
            var handinf = HandInf.GetSingleton();
            //order psi->theta->phi
            // order z->y->x
            //handinf.global_roll_x = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).X;
            //handinf.global_pitch_y = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).Y;
            //handinf.global_yaw_z = (float)QuaternionConverter.Quat2Euler(fram.Joints[0].ToQuaternion()).Z;




            //handinf.global_roll_x = (float)QtoE_glove_to_model(fram.Joints[0].ToQuaternion()).Z;
            //handinf.global_pitch_y = - (float)QtoE_glove_to_model(fram.Joints[0].ToQuaternion()).X;
            //handinf.global_yaw_z = - (float)QtoE_glove_to_model(fram.Joints[0].ToQuaternion()).Y;

            Quaternion q = EtoQ(0, -90, 0);
            Quaternion q_inverse = new Quaternion();
            q_inverse.W = q.W;
            q_inverse.X = -q.X;
            q_inverse.Y = -q.Y;
            q_inverse.Z = -q.Z;

            handinf.global_roll_x = -(float)QtoE_glove_to_model(fram.Joints[0].ToQuaternion()* q_inverse).X;
            handinf.global_pitch_y = -(float)QtoE_glove_to_model(fram.Joints[0].ToQuaternion() * q_inverse).Z;
            handinf.global_yaw_z = -(float)QtoE_glove_to_model(fram.Joints[0].ToQuaternion() * q_inverse).Y;




            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                {
                    //因为这个地方的传感器原来对应着大拇指，只有2和3才有值。
                    Quaternion qy = new Quaternion(0, 1, 0, 0);
                    var bend = (float)QtoE(fram.Joints[i * 3 + 2].ToQuaternion()*qy).Z;
                    var strech = 360- (float)QtoE_glove_to_model(fram.Joints[i * 3 + 2].ToQuaternion()*qy).Y;
                    var bend1 = (float)QtoE(fram.Joints[i * 3 + 3].ToQuaternion()).Z;

                    bend = RestrictAngle(bend, -10, 98);
                    bend1 = RestrictAngle(bend1, 0, 90);
                    strech = RestrictAngle(strech, -20, 10);
                    if (bend <= 30)
                    {
                        strech = strech;
                    }
                    else if(bend <=50)
                    {
                        strech = RestrictAngle(strech, -10, 10);
                    }
                    else
                    {
                        strech = 0;
                    }

                    handinf.fingers[i].Mcp_x = bend;
                    handinf.fingers[i].Mcp_z = strech;
                    handinf.fingers[i].Pip = bend1;
                    handinf.fingers[i].Dip = bend1;

                }
                else
                {
                    if(i == 4)
                    {
                        Quaternion q1 = fram.Joints[i * 3 + 1].ToQuaternion();
                        Quaternion q2 = new Quaternion();
                        q2.W = 0;
                        q2.X = 0;
                        q2.Y = 1;
                        q2.Z = 0;
                        Quaternion q3 = q1 * q2;


                        handinf.fingers[i].Mcp_x = (float)QtoE_glove_to_model(q3).Z;
                        handinf.fingers[i].Mcp_z = - (float)QtoE_glove_to_model(q3).Y;
                        handinf.fingers[i].Pip = RestrictAngle((float)QtoE(fram.Joints[i * 3 + 2].ToQuaternion()).Z,0,90);
                        handinf.fingers[i].Dip = - (float)QtoE_glove_to_model(q3).X;

                    }
                    else
                    {
                        Quaternion qy = new Quaternion(0, 1, 0, 0);
                        var bend = (float)QtoE(fram.Joints[i * 3 + 1].ToQuaternion()*qy).Z;
                        var strech = 360 - (float)QtoE_glove_to_model(fram.Joints[i * 3 + 1].ToQuaternion()*qy).Y;
                        var bend1 = (float)QtoE(fram.Joints[i * 3 + 2].ToQuaternion()).Z;

                        bend = RestrictAngle(bend, -10, 98);
                        bend1 = RestrictAngle(bend1, 0, 90);

                        if (i == 1)
                        {
                            strech = RestrictAngle(strech, -15, 15);
                        }
                        else if (i == 2)
                        {
                            strech = RestrictAngle(strech, -10, 10);
                        }
                        else
                        {
                            strech = RestrictAngle(strech, -20, 30);
                        }

                        if (bend <= 30)
                        {
                            strech = strech;
                        }
                        else if(bend <= 50)
                        {
                            strech = RestrictAngle(strech, -10, 10);
                        }
                        else
                        {
                            strech = 0;
                        }
                        

                        handinf.fingers[i].Mcp_x = bend;
                        handinf.fingers[i].Mcp_z = strech;
                        handinf.fingers[i].Pip = bend1;
                        handinf.fingers[i].Dip = bend1;
                    }
                }

            }

            return handinf;
        }


    }
}
