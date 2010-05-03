/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */
 
-- 
-- SQL statements to use the generator to create types.
-- This isn't meant to go into the db.
-- 
CREATE VIEW RevisionWorkView AS 
	SELECT 
		Work.id, Work.command_id, Work.state, Work.starttime, Work.endtime, Work.duration, Work.logfile, Work.summary, 
		Host.host, 
		Lane.lane, 
		Revision.author, Revision.revision, 
		Command.command, 
		Command.nonfatal, Command.alwaysexecute, Command.sequence, Command.internal,
		RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.revision_id, 
		RevisionWork.state AS revisionwork_state,
		WorkHost.host AS workhost
	FROM Work
	INNER JOIN Revision ON Work.revision_id = Revision.id 
	INNER JOIN Lane ON Work.lane_id = Lane.id 
	INNER JOIN Host ON Work.host_id = Host.id 
	INNER JOIN Host AS WorkHost ON RevisionWork.workhost_id = WorkHost.id
	INNER JOIN Command ON Work.command_id = Command.id
	INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id
	WHERE 
		RevisionWork.host_id = @host_id AND
		RevisionWork.lane_id = @lane_id AND
		Work.revisionwork_id IN 
			(SELECT RevisionWork.id 
				FROM RevisionWork 
				INNER JOIN Revision on RevisionWork.id = Revision.id 
				WHERE RevisionWork.lane_id = @lane_id AND RevisionWork.host_id = @host_id 
				ORDER BY Revision.date DESC LIMIT @limit OFFSET @offset) 
	ORDER BY Revision.date DESC; 
	
CREATE VIEW RevisionWorkView2 AS 
	SELECT
		RevisionWork.id, RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.revision_id, 
		RevisionWork.state, RevisionWork.completed,
		Revision.revision, Revision.date
	FROM RevisionWork
	INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
	ORDER BY 
		Revision.date DESC;