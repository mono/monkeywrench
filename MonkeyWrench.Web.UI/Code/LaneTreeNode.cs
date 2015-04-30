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

	public void Sort ()
	{
		foreach (var child in Children)
			child.Sort ();
		Children.Sort ((a, b) => string.Compare (a.Lane.lane, b.Lane.lane));
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

	static List<DBLane> FilterToLatestMonth (IEnumerable<DBLane> lanes)
	{
		var rv = new List<DBLane> ();
		var already_in = new HashSet<int> ();
		var map = new Dictionary<int, DBLane> ();

		// find all the modified lanes in the last month, or never modified at all
		foreach (var lane in lanes) {
			map [lane.id] = lane;
			if (lane.changed_date.HasValue && lane.changed_date.Value.AddMonths (1) < DateTime.Now)
				continue;
			rv.Add (lane);
			already_in.Add (lane.id);
		}

		// include all the parent lanes, recursively
		for (int i = 0; i < rv.Count; i++) {
			var lane = rv [i];

			while (lane.parent_lane_id.HasValue) {
				if (!map.ContainsKey (lane.parent_lane_id.Value)) {
					MonkeyWrench.Logger.Log ("Can't find lane id: {0} in map", lane.parent_lane_id.Value);
					break;
				}
				lane = map [lane.parent_lane_id.Value];
				if (!already_in.Contains (lane.id)) {
					rv.Add (lane);
					already_in.Add (lane.id);
				}
			}
		}

		return rv;
	}

	public static LaneTreeNode BuildTree (IEnumerable<DBLane> lanes, IEnumerable<DBHostLane> host_lanes)
	{
		// we need to create a tree of the lanes
		LaneTreeNode root = new LaneTreeNode (null);
		Dictionary<int, LaneTreeNode> nodes = new Dictionary<int, LaneTreeNode> ();
		List<DBLane> lanes_clone = FilterToLatestMonth (lanes);

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

		root.Sort ();

		return root;
	}
}
