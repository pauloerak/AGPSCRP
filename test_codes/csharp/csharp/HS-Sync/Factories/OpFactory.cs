 using Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trees;

namespace csharp.HS_Sync.Factories
{
    internal static class OpFactory
    {
        /// <summary>
        /// for creating OPNodes 
        /// </summary>
        private static Random random = new Random();

        private static List<(Func<double, double, double> Op, string Name)> operations = new List<(Func<double, double, double>, string)>
        {
            ((a,b) => a+b, "Add"),
            ((a,b) => a-b, "Subtract"),
            ((a,b) => a*b, "Multiply"),
            ((a,b) => (a/(Math.Abs(b)+1e-6)), "Divide"),
            ((a,b) => Math.Max(a,b), "Max"),
            ((a,b) => Math.Min(a,b), "Min")
        };

        public static OpNode GetRandomOperationNode(int height, TreeNode parent)
        {
            var selected = operations[random.Next(operations.Count)];
            return new OpNode(selected.Op,parent,height) { Name = selected.Name };
        }

        public static OpNode GetOperationNode(string operation, TreeNode parent)
        {
            var selected = operations.FirstOrDefault(x => x.Name == operation);
            if (selected.Op == null)
            {
                Console.WriteLine("I can only give you operations from this list:");
                foreach ((Func<double, double, double>, string) p in operations)
                {
                    Console.WriteLine(p.Item2);
                }
                Console.WriteLine("\n");
                return null;
            }
            return new OpNode(selected.Op, parent, 0) { Name = selected.Name };
        }
    }
}
