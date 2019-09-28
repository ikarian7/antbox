using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class TreeNode
{
    public Ant value;
    public TreeNode parent;
    public List<TreeNode> children = new List<TreeNode>();

    private static List<TreeNode> allNodes = new List<TreeNode>();

    public TreeNode(Ant val)
    {
        this.value = val;
        this.parent = null;
        allNodes.Add(this);
    }

    public void Add(TreeNode node)
    {
        node.parent = this;

        this.children.Add(node);
        allNodes.Add(node);
    }

    public TreeNode FindByValue(Ant ant)
    {
        return allNodes.Find(x => ant == x.value);
    }
}

