using csharp.HS_Sync;
using csharp.HS_Sync.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Trees;

namespace Nodes
{
    public abstract class TreeNode
    {
        /// <summary>
        /// abstract class for tree nodes that take Stacks
        /// </summary>
        public abstract double Evaluate(Block container, Stack stack, long now);
        public abstract int Depth { get; set; }
        public abstract TreeNode DeepCopy(TreeNode parent);
        public abstract int UpdateDepth(int depth);
        public abstract TreeNode Parent { get; set; }
        public abstract int DepthOfChildren { get; set; }
        public abstract string PrefixNotation();

    }

    public class ConstantNode : TreeNode
    {
        /// <summary>
        /// A tree node that contains a constant value
        /// </summary>
        public double Value;
        public override int Depth { get; set; }

        public override TreeNode Parent { get; set; }
        public override int DepthOfChildren { get; set; }

        public ConstantNode(double value, TreeNode parent, int depth) { 
            Value = value;
            Depth = depth;
            Parent = parent;
            DepthOfChildren = 0;
        }

        public override TreeNode DeepCopy(TreeNode parent)
        {
            return new ConstantNode(Value,parent,Depth);
        }

        public override double Evaluate(Block container, Stack stack, long now)
        {
            return Value;
        }

        public override string ToString()
        {
            return $"Depth:{Depth} {Value}";
        }

        public override int UpdateDepth(int depth)
        {
            Depth = depth;
            return DepthOfChildren;
        }

        public override string PrefixNotation()
        {
            return Value.ToString();
        }
    }

    public class FeatureNode : TreeNode
    {
        /// <summary>
        /// A tree node that can access a param from a block
        /// </summary>
        public Func<Block, Stack, long,  double> Accessor;
        public string Name;

        public override int Depth { get; set; }

        public override TreeNode Parent { get; set; }
        public override int DepthOfChildren { get; set; }

        public FeatureNode(Func<Block, Stack, long, double> accessor, TreeNode parent, int height)
        {
            Accessor = accessor;
            Depth = height;
            Parent = parent;
            DepthOfChildren = 0;
        }

        public override TreeNode DeepCopy(TreeNode parent)
        {
            return new FeatureNode(Accessor, parent, Depth) { Name = Name};
        }

        public override double Evaluate(Block container, Stack stack, long now)
        {
            return Accessor(container, stack, now);
        }

        public override string ToString()
        {
            return $"Depth:{Depth} {Name}";
        }

        public override int UpdateDepth(int depth)
        {
            Depth = depth;
            return DepthOfChildren;
        }

        public override string PrefixNotation()
        {
            return Name;
        }
    }

    public class OpNode : TreeNode
    {
        /// <summary>
        /// A node that performs an operation over two values.
        /// </summary>
        public TreeNode Left;
        public TreeNode Right;
        public Func<double, double, double> Op;
        public string Name;
        public override int Depth { get; set; }

        public override TreeNode Parent { get; set; }
        public override int DepthOfChildren { get; set; }

        public OpNode(Func<double, double, double> op, TreeNode parent, int height)
        {
            Op = op;
            Depth = height;
            Parent = parent;
            DepthOfChildren = 1;
        }

        public override TreeNode DeepCopy(TreeNode parent)
        {
            OpNode copy = new OpNode(Op, parent, Depth) { Name = Name, DepthOfChildren = DepthOfChildren}; 
            copy.Left = Left.DeepCopy(copy);
            copy.Right = Right.DeepCopy(copy);
            return copy;
        }

        public override double Evaluate(Block container, Stack stack, long now)
        {
            return Op(Left.Evaluate(container,stack, now), Right.Evaluate(container,stack, now));
        }

        public override string ToString()
        {
            return $"Depth:{Depth} DOC:{DepthOfChildren} {Name}({Left.ToString()}, {Right.ToString()})";
        }

        public override int UpdateDepth(int depth)
        {
            Depth = depth;
            DepthOfChildren = Math.Max(Left.UpdateDepth(Depth + 1), Right.UpdateDepth(Depth + 1))+1;
            return DepthOfChildren;
        }

        public override string PrefixNotation()
        {
            return $"{Name} {Left.PrefixNotation()} {Right.PrefixNotation()}";
        }
    }

    
    public static class NodeDictionary                              //please keep this updated if you decide to make any new operations or features
    {
        private static List<string> OPs = new List<string>() { "Add", "Subtract", "Multiply", "Divide", "Max", "Min"};
        private static List<string> functions = new List<string>() { "Block_Ready", "Block_Due", "Stack_Height", "Stack_FreeSpace", "Stack_ContainsReady", "Stack_Count", "Stack_Id", "Block_Time", "Time" };

        public static TreeNode Manifest(List<string> record, TreeNode parent)
        {
            TreeNode node = null;
            if (OPs.Contains(record[0]))
            {
                node = OpFactory.GetOperationNode(record[0], parent);
                record.RemoveAt(0);
                ((OpNode)node).Left = Manifest(record,parent);
                ((OpNode)node).Right = Manifest(record, parent);
            }
            else if (functions.Contains(record[0])) 
            { 
                node = FeatureFactory.GetFeatureNode(record[0], parent);
                record.RemoveAt(0);
            }
            return node;
        }


    }
}
