/*
 * LaneTreeNode.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.Web.WebServices;

public class LaneTreeNode
{
	public DBLane Lane;
	public List<DBHostLane> HostLanes = new List<DBHostLane> ();
	public List<LaneTreeNode> Children = new List<LaneTreeNode> ();

	public LaneTreeNode (DBLane lane)
		: this (lane, null)
	{
	}

	public LaneTreeNode (DBLane lane, IEnumerable<DBHostLane> hostlanes){
		this.Lane = lane;
		if (hostlanes != null && lane != null) {
			foreach (DBHostLane hl in hostlanes) {
				if (hl.lane_id != lane.id)
					continue;
				HostLanes.Add (hl);
			}
		}
	}

	public void ForEach (Action<LaneTreeNode> action)
	{
		action (this);
		foreach (LaneTreeNode n in Children)
			n.ForEach (action);
	}

	public int Leafs
	{
		get
		{
			int result = 0;
			if (Children.Count == 0) {
				result = Math.Max (1, HostLanes.Count);
			} else {
				foreach (LaneTreeNode n in Children)
					result += n.Leafs;
			}
			return result;
		}
	}

	public int Descendents
	{
		get
		{
			int result = Children.Count;
			foreach (LaneTreeNode n in Children)
				result += n.Descendents;
			return result;
		}
	}

	public int Depth
	{
		get
		{
			int result = 0;
			if (Children.Count != 0) {
				foreach (LaneTreeNode n in Children)
					result = Math.Max (result, n.Depth);
				result += 1;
			}
			return result;
		}
	}

	public LaneTreeNode Find (Predicate<LaneTreeNode> predicate)
	{
		LaneTreeNode result;

		if (predicate (this))
			return this;

		foreach (LaneTreeNode node in Children) {
			result = node.Find (predicate);

			if (result != null)
				return result;
		}

		return null;
	}

	public static LaneTreeNode BuildTree (IEnumerable<DBLane> lanes, IEnumerable<DBHostLane> host_lanes)
	{
		// we need to create a tree of the lanes
		LaneTreeNode root = new LaneTreeNode (null);
		Dictionary<int, LaneTreeNode> nodes = new Dictionary<int, LaneTreeNode> ();
		List<DBLane> lanes_clone = new List<DBLane> (lanes);

		while (lanes_clone.Count != 0) {
			int c = lanes_clone.Count;
			for (int i = lanes_clone.Count - 1; i >= 0; i--) {
				DBLane lane = lanes_clone [i];
				if (!lane.parent_lane_id.HasValue) {
					LaneTreeNode node = new LaneTreeNode (lane, host_lanes);
					root.Children.Add (node);
					nodes [lane.id] = node;
					lanes_clone.RemoveAt (i);
					continue;
				}

				if (nodes.ContainsKey (lane.parent_lane_id.Value)) {
					LaneTreeNode node = new LaneTreeNode (lane, host_lanes);
					nodes [lane.parent_lane_id.Value].Children.Add (node);
					nodes [lane.id] = node;
					lanes_clone.RemoveAt (i);
					continue;
				}
			}
			if (c == lanes_clone.Count) {
				Console.WriteLine ("Infinite recursion detected");
				break;
			}
		}

		return root;
	}
}
