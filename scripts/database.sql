SET client_encoding = 'UTF8';

-- DROP SCHEMA IF EXISTS public CASCADE;
-- DROP DATABASE IF EXISTS builder;

CREATE SCHEMA public;
CREATE DATABASE builder OWNER builder;

\connect builder


CREATE TABLE Host (
	id              serial     PRIMARY KEY,
	host            text       UNIQUE NOT NULL,     -- the name of this host (no commas allowed)
	description     text       NOT NULL DEFAULT '', -- a description for this host
	architecture    text       NOT NULL DEFAULT '', -- this host's architecture.
	queuemanagement int        NOT NULL DEFAULT 0   -- how this host manages its queue. 
                                                    -- 0: minimize the number of revisions currently in work
                                                    --    * there might be a significant delay until the latest revision is built.
                                                    -- 1: start building the latest revision as soon as possible
                                                    --    * if the bot can't keep up with the number of commits, it'll run out of disk space.
);

CREATE TABLE Lane (
	id             serial     PRIMARY KEY,
	lane           text       UNIQUE NOT NULL,
	source_control text       NOT NULL DEFAULT 'svn', -- the source control system. only svn supported so far
	repository     text       NOT NULL,               -- the source control repository.
	min_revision   text       NOT NULL DEFAULT '1',   -- the first revision to do.
	max_revision   text       NOT NULL DEFAULT ''     -- the last revision to do. '' defaults to all revisions
);

CREATE TABLE LaneDependency (
	id                serial     PRIMARY KEY,
	lane_id           int        NOT NULL REFERENCES Lane(id), -- the lane we're configuring
	dependent_lane_id int        NOT NULL REFERENCES Lane(id), -- the lane we're depending on
	condition         int        NOT NULL,                     -- the condition
	                                                           -- 0: no condition at all
	                                                           -- 1: dependent_lane_id has succeeded (for the same revision)
	                                                           -- 2: dependent_lane has produced a file whose name is 'filename' (for the same revision)
    filename          text       NULL DEFAULT NULL            
);

CREATE TABLE Command (
	id             serial     PRIMARY KEY,
	lane_id        int        NOT NULL REFERENCES Lane(id),
	command        text       NOT NULL,                         -- the actual file to execute. This should probably be a filename in Lanefile.
	filename       text       NOT NULL DEFAULT 'bash',          -- the program used to execute the command
	arguments      text       NOT NULL DEFAULT '-ex {0}',       -- the arguments passed to the program. The command will be saved to a temporary file on disk, 
	                                                            -- and {0} will be replaced with the filename.
	sequence       int        NOT NULL,                         -- the step sequence. lowest number will be executed first, 
	                                                            -- having several commands with same sequence means they can run in parallel 
	                                                            -- (it doesn't necessarily mean the harness will actually run anything in parallel)
	alwaysexecute  boolean    NOT NULL DEFAULT FALSE,           -- if this command will always be executed, even if any previous steps failed. Typical case is a cleanup command.
	nonfatal       boolean    NOT NULL DEFAULT FALSE,           -- if this command will allow subsequent steps to be executed even if this step fails. Typical case is a test command.
	internal       boolean    NOT NULL DEFAULT FALSE            -- if the user has to be logged in to see files this command produces.
);

CREATE TABLE HostLane (
	id          serial     PRIMARY KEY,
	host_id     int        NOT NULL REFERENCES Host (id),
	lane_id     int        NOT NULL REFERENCES Lane (id),
	enabled     boolean    NOT NULL DEFAULT TRUE                -- if the lane is enabled on this host.
);

CREATE TABLE Lanefile (
	id             serial     PRIMARY KEY,
	lane_id        int        NOT NULL REFERENCES Lane(id),
	name           text       NOT NULL,                          -- the filename
	contents       text       NOT NULL,
	mime           text       NOT NULL DEFAULT 'text/plain'
);

CREATE TABLE Revision (
	id       serial     PRIMARY KEY,
	lane_id  int        NOT NULL REFERENCES Lane(id),
	revision text       NOT NULL,
	author   text       NOT NULL DEFAULT '',
	date     timestamp  NOT NULL DEFAULT '2000-01-01 00:00:00+0',
	log      text       NOT NULL DEFAULT '',
	diff     text       NOT NULL DEFAULT '',
	UNIQUE (lane_id, revision)
);

CREATE TABLE Work (
	id               serial    PRIMARY KEY,
	--TODO: Pending removal -- lane_id          int       NOT NULL REFERENCES Lane(id), -- the lane
	host_id          int       NOT NULL REFERENCES Host(id), 
	--TODO: Pending removal -- revision_id      int       NOT NULL REFERENCES Revision(id), -- the revision to use for this step
	command_id       int       NOT NULL REFERENCES Command(id),  -- the command to execute
	state            int       NOT NULL DEFAULT 0,                       -- 0 queued, 1 executing, 2 failed, 3 success, 
	                                                                     -- 4 aborted, 5 timeout, 6 paused, 7 skipped, 
	                                                                     -- 8 issues (RevisionWork only for nonfatal failures)
	                                                                     -- 9 dependency not fulfilled
	starttime        timestamp NOT NULL DEFAULT '2000-01-01 00:00:00+0', -- the UTC time when the step started to execute
	endtime          timestamp NOT NULL DEFAULT '2000-01-01 00:00:00+0', -- the UTC time when the step stopped to execute
	duration         int       NOT NULL DEFAULT 0,                       -- duration in seconds
	logfile          text      NOT NULL DEFAULT '',                      -- path of the log file
	summary          text      NOT NULL DEFAULT '',                      -- a one line summary of the log
	revisionwork_id  int       REFERENCES RevisionWork (id) -- make NOT NULL after successful move
);

CREATE TABLE File (
	id              serial     PRIMARY KEY,
	filename        text       NOT NULL DEFAULT '',   -- filenames can be duplicate
	md5             text       UNIQUE NOT NULL,       -- having an md5 checksum allows us to use the same record for all equal files 
	file_id         int        NOT NULL,              -- the large object id (should this be defined as oid instead of int?)
	mime            text       NOT NULL DEFAULT '',   -- the mime type of the file
	compressed_mime text       NOT NULL DEFAULT '',   -- if the file is stored compressed, this field is not '', and it specifies the compression algorithm (application/zip, tar, etc). 
	                                                  -- this allows us to store for instance log files compressed, and the web server can deliver the compressed log file by
	                                                  -- adding the proper http response header. 
	                                                  -- the md5 sum is calculated off the uncompressed file.
	size            int        NOT NULL DEFAULT 0,    -- filesize. int32 since I don't think we ever want to store files > 2GB in the database.
	hidden          boolean    NOT NULL DEFAULT FALSE -- 
);
CREATE INDEX file_idx_file_id_key ON File (file_id);

CREATE TABLE WorkFile (
	id             serial     PRIMARY KEY,
	work_id        int        NOT NULL REFERENCES Work (id),
	file_id        int        NOT NULL REFERENCES File (id),
	hidden         boolean    NOT NULL DEFAULT FALSE,
	filename       text       NOT NULL DEFAULT ''     -- we need to have a filename here too, since File is unique based on md5, we can have several WorkFiles with different names and same content.
	                                                  -- in any case, if this field is '', the filename is File's filename.
	UNIQUE (work_id, file_id) -- it doesn't make sense to have the same file referenced twice for a work record
);
CREATE INDEX workfile_idx_file_id_key ON WorkFile (file_id);
CREATE INDEX workfile_idx_work_id_key ON WorkFile (work_id);

CREATE TABLE RevisionWork (
	id             serial     PRIMARY KEY,
	lane_id        int        NOT NULL REFERENCES Lane (id),
	host_id        int        NOT NULL REFERENCES Host (id),
	workhost_id    int        NULL     REFERENCES Host (id) DEFAULT NULL , -- the host which is actually working on this revision. If NULL, same as host_id.
	revision_id    int        NOT NULL REFERENCES Revision (id),
	state          int        NOT NULL DEFAULT 0, -- same as Work.state, though not all are applicable

	-- ** possible states (evaluated in this order) ** 
	-- dependency not fulfilled (any Work.State == dependency not fulfilled)
	-- paused (any Work.State == paused)
	-- queued (all Work.State == queued)
	-- success (all Work.State == success)
	-- failed (any fatal Work.State == failed || Work.state == aborted)
	-- timeout (any fatal Work.State == timeout)
	-- issues (any nonfatal Work.State == failed || Work.State == timeout || Work.State == aborted)
	-- executing (any Work.State == executing)
	-- executing (default)
	
	lock_expires   timestamp  NOT NULL DEFAULT '2000-01-01 00:00:00+0', -- the UTC time when this revisionwork's lock (if any) expires
	completed      boolean    NOT NULL DEFAULT FALSE, -- if this revision has completed it's work
	
	UNIQUE (lane_id, host_id, revision_id)
);


CREATE TABLE Person ( -- 'User' is a reserved word in sql...
	id             serial    PRIMARY KEY,
	login          text      NOT NULL,            -- the login name of the user a-zA-Z0-9_-
	password       text      NOT NULL DEFAULT '', -- the password (in plain text)
	fullname       text      NOT NULL DEFAULT ''  -- the full name of the user
);

CREATE TABLE Login (
	id             serial    PRIMARY KEY,
	cookie         text      UNIQUE NOT NULL,                 -- the cookie stored on the client machine
	person_id      int       NOT NULL REFERENCES Person (id), -- the user this login is valid for
	expires        timestamp NOT NULL,                   	  -- the date/time this login expires
	ip4            text      NOT NULL DEFAULT ''              -- the ip the user is connecting from
);


CREATE VIEW WorkView2 AS 
	SELECT 
		Work.id, Lane.lane, Work.command_id, Work.state, 
		Work.starttime, Work.endtime, Work.duration, Work.logfile, Work.summary, Work.host_id AS workhost_id, 
		Command.nonfatal, Command.alwaysexecute, Command.sequence, Command.internal, Command.command,
		RevisionWork.id AS revisionwork_id,
		RevisionWork.host_id AS masterhost_id,
		RevisionWork.lane_id,
		RevisionWork.revision_id,
		MasterHost.host AS masterhost, 
		WorkHost.host AS workhost,
		Revision.author, Revision.revision
	FROM Work 
		INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id
		INNER JOIN Revision ON RevisionWork.revision_id = Revision.id 
		INNER JOIN Lane ON RevisionWork.lane_id = Lane.id 
		INNER JOIN Host AS MasterHost ON RevisionWork.host_id = MasterHost.id 
		INNER JOIN Host AS WorkHost ON Work.host_id = WorkHost.id
		INNER JOIN Command ON Work.command_id = Command.id;


CREATE VIEW HostLaneView AS
	SELECT HostLane.id, HostLane.lane_id, HostLane.host_id, HostLane.enabled, Lane.lane, Host.host
	FROM HostLane
		INNER JOIN Host ON HostLane.host_id = Host.id
		INNER JOIN Lane ON HostLane.lane_id = Lane.id;

CREATE VIEW LoginView AS
	SELECT Login.id, Login.cookie, Login.person_id, Login.ip4, Person.login, Person.fullname
	FROM Login
		INNER JOIN Person ON Login.person_id = Person.id
	WHERE expires > now ();
	
CREATE VIEW WorkFileView AS
	SELECT WorkFile.id, WorkFile.work_id, WorkFile.file_id, WorkFile.filename, WorkFile.hidden, File.mime, File.compressed_mime, Command.internal
	FROM WorkFile
		INNER JOIN File ON WorkFile.file_id = File.id
		INNER JOIN Work ON WorkFile.work_id = Work.id
		INNER JOIN Command ON Work.command_id = Command.id;
		
-- ignore generator --		

-- method to get id for revisionwork, adding a new record if none is found.
CREATE OR REPLACE FUNCTION add_revisionwork (lane int, host int, revision int) RETURNS INT AS $$ 
BEGIN 
	IF 0 = (SELECT COUNT(*) FROM RevisionWork WHERE lane_id = lane AND host_id = host AND revision_id = revision) THEN
		INSERT INTO revisionwork (lane_id, host_id, revision_id) VALUES (lane, host, revision); 
	END IF; 
	RETURN (SELECT id FROM RevisionWork WHERE lane_id = lane AND host_id = host AND revision_id = revision);
END; 
$$ 
LANGUAGE plpgsql; 

CREATE OR REPLACE FUNCTION get_revisionwork (lane int, host int, revision int, int, int) RETURNS SETOF AS $$
BEGIN
	SELECT revisionwork.id,revision.revision INTO revisionworkview_temp 
	from revisionwork 
	inner join revision on revision.id = revisionwork.revision_id 
	where 
		revisionwork.lane_id = $1 and revisionwork.host_id = $2 
		ORDER BY revision.date LIMIT $3 OFFSET $4;
		
RETURN 
		SELECT 
		Work.id, Work.revision_id, Work.lane_id, Work.host_id, Work.command_id, Work.state, Work.starttime, Work.endtime, Work.duration, Work.logfile, Work.summary, 
		Host.host, 
		Lane.lane, 
		Revision.author, Revision.revision, 
		Command.command, 
		Command.nonfatal, Command.alwaysexecute, Command.sequence, Command.internal
	FROM Work
	INNER JOIN Revision ON Work.revision_id = Revision.id 
	INNER JOIN Lane ON Work.lane_id = Lane.id 
	INNER JOIN Host ON Work.host_id = Host.id 
	INNER JOIN Command ON Work.command_id = Command.id
	INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id
	WHERE 
		RevisionWork.host_id = @host_id AND
		RevisionWork.lane_id = @lane_id AND
		Work.revisionwork_id IN revisionworkview_temp
	ORDER BY Revision.date DESC; 
END;
$$
LANGUAGE plpgsql;

-- unignore generator --