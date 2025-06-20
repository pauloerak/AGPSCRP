using Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csharp.HS_Sync.Factories
{
    internal class FeatureFactory
    {
        /// <summary>
        /// Factory for creating FeatureNodes 
        /// </summary>
        private static Random random = new Random();

        private static List<(Func<Block, Stack, long,  double> Func, string Name)> allowedFeatures = new List<(Func<Block, Stack, long, double>, string)>
        {
            ((block,stack,now) => Convert.ToDouble(block.Ready), "Block_Ready"),
            ((block,stack,now) => Convert.ToDouble(block.Due.MilliSeconds/1000), "Block_Due"),
            ((block,stack,now) => Convert.ToDouble(block.Due.MilliSeconds/1000 - now/1000), "Block_Time"),
            ((block,stack,now) => Convert.ToDouble((block.Due.MilliSeconds/1000 - now/1000) < 90), "Due_below"),  //less than 90 seconds until container has to be out
            ((block,stack,now) => Convert.ToDouble(now/1000), "Time"),
            ((block,stack,now) => Convert.ToDouble(stack.MaxHeight), "Stack_Height"),
            ((block,stack,now) => Convert.ToDouble(stack.Count), "Stack_Count"),
            ((block,stack,now) => Convert.ToDouble(stack.MaxHeight-stack.Count), "Stack_FreeSpace"),
            ((block,stack,now) => Convert.ToDouble(stack.ContainsReady), "Stack_ContainsReady"),
            //((block,stack,now) => Convert.ToDouble(stack.Id), "Stack_Id"),

    };

        private static List<(Func<Block, Stack, long, double> Func, string Name)> allFeatures = new List<(Func<Block, Stack, long, double>, string)>
        {
            ((block,stack,now) => Convert.ToDouble(block.Ready), "Block_Ready"),
            ((block,stack,now) => Convert.ToDouble(block.Due.MilliSeconds/1000), "Block_Due"),
            ((block,stack,now) => Convert.ToDouble(now - block.Due.MilliSeconds/1000), "Block_Time"),
            ((block,stack,now) => Convert.ToDouble(block.Due.MilliSeconds/1000 - now/1000 < 90), "Due_below"),  //less than 90 seconds until container has to be out
            ((block,stack,now) => Convert.ToDouble(now), "Time"),
            ((block,stack,now) => Convert.ToDouble(stack.MaxHeight), "Stack_Height"),
            ((block,stack,now) => Convert.ToDouble(stack.Count), "Stack_Count"),
            ((block,stack,now) => Convert.ToDouble(stack.MaxHeight-stack.Count), "Stack_FreeSpace"),
            ((block,stack,now) => Convert.ToDouble(stack.ContainsReady), "Stack_ContainsReady"),
            ((block,stack,now) => Convert.ToDouble(stack.Id), "Stack_Id"),
        };

        public static FeatureNode GetRandomFeatureNode(int height, TreeNode parent)
        {
            var selected = allowedFeatures[random.Next(allowedFeatures.Count)];
            return new FeatureNode(selected.Func,parent,height) { Name = selected.Name };
        }

        public static FeatureNode GetFeatureNode(string feature, TreeNode parent)
        {
            var selected = allFeatures.FirstOrDefault(x => x.Name == feature);
            if (selected.Func == null)
            {
                Console.WriteLine("I can only give you features from this list:");
                foreach ((Func<Block, Stack, long, double>, string) p in allFeatures)
                {
                    Console.WriteLine(p.Item2);
                }
                Console.WriteLine("\n");
                return null;
            }
            return new FeatureNode(selected.Func, parent,0) { Name = selected.Name };
        }
    }
}
