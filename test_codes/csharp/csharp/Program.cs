using NetMQ;
using NetMQ.Sockets;
using System;
using System.Text;
using RewardsClass;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Design;

namespace DynStacking
{
    public enum OptimizerType
    {
        RuleBased,
        ModelBased
    }
    public enum ProblemType
    {
        HotStorage,
        RollingMill,
        CraneScheduling
    }

    interface IPlanner
    {
        byte[] PlanMoves(byte[] worldData, OptimizerType opt);
        void EndSounded(byte[] worldData);
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Requires 3 arguments: SOCKET SIM_ID PROBLEM");
                return;
            }

            string Continue = "";
            bool test = false;
            bool handbuilt = false;
            if(args.Length > 3)
            {
                if (args[3] == "--handbuilt")
                {
                    handbuilt = true;
                }
                else
                {
                    Continue = args[3];
                    if (args.Length > 4)
                    {
                        if (args[4] == "--test")
                        {
                            test = true;
                        }
                    
                    }
                }
                
            }

            var socketAddr = args[0];
            var identity = new UTF8Encoding().GetBytes(args[1]);
            //IPlanner planner = args[2] == "HS" ? new HotStorage.Planner() : new RollingMill.Planner();
            IPlanner planner = args[2] switch
            {
                "HS" => new csharp.HS_Sync.SyncHSPlanner(Continue,args[1],test,handbuilt),
                "RM" => new RollingMill.Planner(),
                "CS" => new CraneScheduling.Planner(),
                _ => null
            };

            if (planner == null)
            {
                Console.WriteLine($"Invalid problem: {args[2]}");
                return;
            }

            OptimizerType optType;
            if (args.Length > 2)
            {
                optType = OptimizerType.RuleBased;
            }
            else
            {
                optType = OptimizerType.ModelBased;
            }

            Console.WriteLine(optType);

            using (var socket = new DealerSocket())
            {
                socket.Options.Identity = identity;
                socket.Connect(socketAddr);
                Console.WriteLine("Connected");

                while (true)
                {
                    //Console.WriteLine("Waiting for request...");
                    var request = socket.ReceiveMultipartBytes();

                    string type = ParseMessage(request[1]);
                    if (type == "error")
                    {
                        Console.WriteLine("Error: wrong heading in message.");
                    }
                    else if (type == "end")
                    {
                        planner.EndSounded(request[2]);
                    }
                    else
                    {
                        var answer = planner.PlanMoves(request[2], optType);

                        var msg = new NetMQMessage();
                        msg.AppendEmptyFrame();
                        msg.Append("crane");
                        if (answer != null)
                        {
                            msg.Append(answer);
                            //Console.WriteLine("Sending something");
                        }
                        else
                            msg.AppendEmptyFrame();

                        socket.SendMultipartMessage(msg);
                    }
                }
            }
        }

        public static string ParseMessage(byte[] message)
        {
            byte[] worldBytes = { 119, 111, 114, 108, 100}; // "world"
            byte[] world1Bytes = { 119, 111, 114, 108, 100, 119 }; // "worldw"
            byte[] world2Bytes = { 119, 111, 114, 108, 100, 111 }; // "worldo"
            byte[] endBytes = { 101, 110, 100 }; // "end"

            if (IsMatch(message, worldBytes))
            {
                return "world";
            }
            else if (IsMatch(message, world1Bytes))
            {
                return "world1";
            }
            else if (IsMatch(message, world2Bytes))
            {
                return "world2";
            }
            else if (IsMatch(message, endBytes))
            {
                return "end";
            }
            else
            {
                return "error";
            }
        }

        private static bool IsMatch(byte[] input, byte[] expected)
        {
            if (input.Length != expected.Length) return false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != expected[i]) return false;
            }
            return true;
        }
    }
}
