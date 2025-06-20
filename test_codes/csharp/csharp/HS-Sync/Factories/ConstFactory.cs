using Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trees;

namespace csharp.HS_Sync.Factories
{
    internal class ConstFactory
    {
        private static Random random = new Random();

        public static ConstantNode GetRandomConstNode(int height, TreeNode parent)
        {
            var selected = random.NextDouble();
            return new ConstantNode(selected, parent, height);
        }
    }
}
