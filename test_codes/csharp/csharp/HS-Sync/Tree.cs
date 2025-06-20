using csharp;
using csharp.HS_Sync;
using csharp.HS_Sync.Factories;
using Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Trees
{
    public class Tree
    {
        /// <summary>
        /// Tree class that holds the sintax tree
        /// </summary>
        public TreeNode root;
        public int maxDepth;
        private double chanceForConst = 0;

        public Tree(int depth = 5)
        {
            /// <summary>
            /// Random generation of a tree
            /// </summary>
            Random random = new Random();
            maxDepth = depth;
            root = GenerateRandomTree(random, null, 0);
        }

        public Tree(List<string> record, int depth)
        {
            /// <summary>
            /// Generates a tree from an input
            /// </summary>
            root = NodeDictionary.Manifest(record,null);
            UpdateHeights();
            maxDepth = depth;
        }

        public Tree(TreeNode root, int depth)
        {
            /// <summary>
            /// Generates a tree from an root
            /// </summary>
            this.root = root;
            maxDepth = depth;
        }

        //razmisli možda prebaciti ovo u TreeFactory
        private TreeNode GenerateRandomTree(Random random, TreeNode parent, int depth)
        {
            TreeNode node = null;
            if (depth < maxDepth - 1)
            {
                int selected = random.Next(0, maxDepth-depth+1);
                if (selected > 0)
                {
                    OpNode temp = OpFactory.GetRandomOperationNode(depth,parent);
                    temp.Left = GenerateRandomTree(random, temp ,depth + 1);
                    temp.Right = GenerateRandomTree(random, temp ,depth + 1);
                    temp.DepthOfChildren = Math.Max(temp.Left.DepthOfChildren,temp.Right.DepthOfChildren)+1;
                    node = temp;
                }
                else {         
                    if (random.NextDouble() < chanceForConst) node = ConstFactory.GetRandomConstNode(depth, parent);
                    else node = FeatureFactory.GetRandomFeatureNode(depth, parent);
                }
            }
            else
            {
                if (random.NextDouble() < chanceForConst) node = ConstFactory.GetRandomConstNode(depth, parent);
                else node = FeatureFactory.GetRandomFeatureNode(depth, parent);

            }
            return node;
        }

        public override string ToString() 
        {
            return root.ToString();
        }

        public void PrintTree() 
        {
            Console.WriteLine(this.ToString());
        }

        public double Evaluate(Block block, Stack stack, long now) 
        {
            return root.Evaluate(block, stack, now);
        }

        public Tree DeepCopyTree() 
        {
            return new Tree(root.DeepCopy(null), maxDepth);
        }

        public void UpdateHeights() 
        {
            root.UpdateDepth(0);
        }

        public void Swap(TreeNode firstSpot, TreeNode secondSpot, Tree second) 
        {
            TreeNode temp; // helping node 
            if (firstSpot.Parent == null) // in case we picked a root node of the this tree
            {
                root = secondSpot; // second node becomes the root of the this tree
                temp = secondSpot.Parent;
                secondSpot.Parent = null;
            }
            else
            {
                temp = secondSpot.Parent;
                secondSpot.Parent = firstSpot.Parent; // connect the second node to the this one's parent
                if (((OpNode)firstSpot.Parent).Left == firstSpot)
                {
                    ((OpNode)firstSpot.Parent).Left = secondSpot;
                }
                else 
                {
                    ((OpNode)firstSpot.Parent).Right = secondSpot;
                }
            }
            if (temp == null) // in case that we picked a root of the other tree
            {
                second.root = firstSpot; // this node becomes the root of the second tree
                firstSpot.Parent = null;
            }
            else 
            {
                firstSpot.Parent = temp; // connect the this node to the second one's parent
                if (((OpNode)firstSpot.Parent).Left == secondSpot)
                {
                    ((OpNode)firstSpot.Parent).Left = firstSpot;
                }
                else
                {
                    ((OpNode)firstSpot.Parent).Right = firstSpot;
                }
            }
            UpdateHeights(); // we have to re-evaluate depths.
            second.UpdateHeights();
        }

        public void SwapOPGene(OpNode firstSpot, OpNode secondSpot, Tree second)
        {
            TreeNode temp; // helping node 
            if (firstSpot.Parent == null) // in case we picked a root node of the this tree
            {
                root = secondSpot; // second node becomes the root of the this tree
                temp = secondSpot.Parent;
                secondSpot.Parent = null;
            }
            else
            {
                temp = secondSpot.Parent;
                secondSpot.Parent = firstSpot.Parent; // connect the second node to the this one's parent
                if (((OpNode)firstSpot.Parent).Left == firstSpot)
                {
                    ((OpNode)firstSpot.Parent).Left = secondSpot;
                }
                else
                {
                    ((OpNode)firstSpot.Parent).Right = secondSpot;
                }
            }
            if (temp == null) // in case that we picked a root of the other tree
            {
                second.root = firstSpot; // this node becomes the root of the second tree
                firstSpot.Parent = null;
            }
            else
            {
                firstSpot.Parent = temp; // connect the this node to the second one's parent
                if (((OpNode)firstSpot.Parent).Left == secondSpot)
                {
                    ((OpNode)firstSpot.Parent).Left = firstSpot;
                }
                else
                {
                    ((OpNode)firstSpot.Parent).Right = firstSpot;
                }
            }

            temp = secondSpot.Left;                     // save the second's left child  
            firstSpot.Left.Parent = secondSpot;         // the second is the parent of the first's left child 
            secondSpot.Left = firstSpot.Left;           // the second's left child is first's left child
            if ( temp != null)
            {
                temp.Parent = firstSpot;                    // the saved child's parent is first
                firstSpot.Left = temp;                      // first's left child is the saved child
            }
            else firstSpot.Left = FeatureFactory.GetRandomFeatureNode(firstSpot.Depth+1,firstSpot);
            

            temp = secondSpot.Right;
            firstSpot.Right.Parent = secondSpot;
            secondSpot.Right = firstSpot.Right;
            if (temp != null)
            {
                temp.Parent = firstSpot;
                firstSpot.Right = temp;
            }
            else firstSpot.Right = FeatureFactory.GetRandomFeatureNode(firstSpot.Depth + 1, firstSpot);

            UpdateHeights(); // we have to re-evaluate depths.
            second.UpdateHeights();
        }

        public string PrefixNotation()
        {
            return root.PrefixNotation();
        }
    }   
}