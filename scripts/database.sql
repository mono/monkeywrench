SET client_encoding = 'UTF8';

-- DROP SCHEMA IF EXISTS public CASCADE;
-- DROP DATABASE IF EXISTS builder;

-- CREATE SCHEMA public;
CREATE DATABASE builder OWNER builder;

\connect builder

CREATE TABLE Release (
	id             serial    PRIMARY KEY,
	version        text      NOT NULL DEFAULT '', -- 1.0.0.*
	revision       text      NOT NULL DEFAULT '', -- the git sha
	description    text      NOT NULL DEFAULT '',
	filename       text      NOT NULL DEFAULT '', -- where the released binary (compressed) is stored on disk

	UNIQUE (version),
	UNIQUE (revision)
);

-- 
-- Enums:
--
-- DependencyCondition (0, 1, 2) 
--
-- State (0 - 9)
--
-- MatchMode:
-- * 0: space separated shell globs
-- * 1: regexp
-- * 2: exact match
--
-- DeleteCondition:
-- * 0: never (to make it default to not delete)
-- * 1: delete after x days (example: x = 7, would delete all data which is more than one week old)
-- * 2: delete after x built revisions (example: x = 100, if there are 200 revisions, revisions 50 - 200 are built, we'd delete data from revisions 151 - 200)
--
--

CREATE TABLE Host (
	id              serial     PRIMARY KEY,
	host            text       NOT NULL,     -- the name of this host (no commas allowed)
	description     text       NOT NULL DEFAULT '', -- a description for this host
	architecture    text       NOT NULL DEFAULT '', -- this host's architecture.
	queuemanagement int        NOT NULL DEFAULT 0,  -- how this host manages its queue. 
                                                    -- 0: minimize the number of revisions currently in work
                                                    --    * there might be a significant delay until the latest revision is built.
                                                    -- 1: start building the latest revision as soon as possible
                                                    --    * if the bot can't keep up with the number of commits, it'll run out of disk space.
                                                    -- 2: send one revisionwork at a time to the bot, even if several lanes are configured for it,
                                                    --    and cycle through the configured lanes when selecting revisionwork.
	enabled         boolean    NOT NULL DEFAULT TRUE, -- if this host is enabled.
	release_id      int        NULL REFERENCES Release (id) ON DELETE CASCADE, -- the release this host should use. May be null - in which case the updating is not done automatically.
	UNIQUE (host)
);

-- host/master host relationships
-- a host will do work for any of its master hosts and itself.
-- TODO: add priority?
CREATE TABLE MasterHost (
    id              serial     PRIMARY KEY,
    host_id         int        NOT NULL REFERENCES Host (id),
    master_host_id  int        NOT NULL REFERENCES Host (id),
    
    UNIQUE (host_id, master_host_id)     
);

CREATE TABLE Lane (
	id             serial     PRIMARY KEY,
	lane           text       UNIQUE NOT NULL,
	source_control text       NOT NULL DEFAULT 'svn', -- the source control system. only svn or git supported so far
	repository     text       NOT NULL,               -- the source control repository.
	min_revision   text       NOT NULL DEFAULT '1',   -- the first revision to do.
	max_revision   text       NOT NULL DEFAULT '',    -- the last revision to do. '' defaults to all revisions
	parent_lane_id int        NULL DEFAULT NULL,      -- the parent lane (if any) of this lane
	commit_filter  text       NOT NULL DEFAULT '',    -- a filter to filter out commits. Syntax not decided yet. An empty filter means include all commits to the repository.
	UNIQUE (lane)
);
INSERT INTO Lane (lane, source_control, repository) VALUES ('monkeywrench', 'git', 'git://github.com/mono/monkeywrench');

CREATE TABLE EnvironmentVariable (
	id              serial    PRIMARY KEY,
	host_id         int       NULL REFERENCES Host (id),
	lane_id         int       NULL REFERENCES Lane (id),
	name            text      NULL DEFAULT NULL,
	value           text      NULL DEFAULT NULL,

	UNIQUE (host_id, lane_id, name)
);

CREATE TABLE FileDeletionDirective (
	id             serial     PRIMARY KEY,
	name           text       UNIQUE NOT NULL,    -- a descriptive name     
	filename       text       NOT NULL,           -- the filename to act upon. space separated shell globs.
	match_mode     int        NOT NULL DEFAULT 0, -- value of MatchMode enum above, applied to 'filename'
	condition      int        NOT NULL DEFAULT 0, -- value of DeleteCondition enum above.
	x              int        NOT NULL DEFAULT 0  -- the parameter of DeleteCondition
);

CREATE TABLE LaneDeletionDirective (
	id                           serial     PRIMARY KEY,
	lane_id                      int        NOT NULL References Lane (id),
	file_deletion_directive_id   int        NOT NULL References FileDeletionDirective (id),
	enabled                      boolean    NOT NULL DEFAULT false, -- if the deletion directive is enabled
	
	UNIQUE (lane_id, file_deletion_directive_id)
);

CREATE TABLE LaneDependency (
	id                serial     PRIMARY KEY,
	lane_id           int        NOT NULL REFERENCES Lane(id), -- the lane we're configuring
	dependent_lane_id int        NOT NULL REFERENCES Lane(id), -- the lane we're depending on
	dependent_host_id int        NULL REFERENCES Host(id),     -- the host used to satisfy the condition (null to include all hosts)
	condition         int        NOT NULL,                     -- the condition
	                                                           -- 0: no condition at all
	                                                           -- 1: dependent_lane_id has succeeded (for the same revision)
	                                                           -- 2: dependent_lane has produced a file whose name is 'filename' (for the same revision)
    filename          text       NULL DEFAULT NULL,
    download_files    text       NULL DEFAULT NULL 			   -- comma separated list of files to download, admits * and ? as wild cards
);

CREATE TABLE Command (
	id             serial     PRIMARY KEY,
	lane_id        int        NULL REFERENCES Lane(id),         -- this can be null to remove a command from a lane (deleting the command won't work if there already is work executed for the command)
	command        text       NOT NULL,                         -- the actual file to execute. This should probably be a filename in Lanefile.
	filename       text       NOT NULL DEFAULT 'bash',          -- the program used to execute the command
	arguments      text       NOT NULL DEFAULT '-ex {0}',       -- the arguments passed to the program. The command will be saved to a temporary file on disk, 
	                                                            -- and {0} will be replaced with the filename.
	sequence       int        NOT NULL,                         -- the step sequence. lowest number will be executed first, 
	                                                            -- having several commands with same sequence means they can run in parallel 
	                                                            -- (it doesn't necessarily mean the harness will actually run anything in parallel)
	alwaysexecute  boolean    NOT NULL DEFAULT FALSE,           -- if this command will always be executed, even if any previous steps failed. Typical case is a cleanup command.
	nonfatal       boolean    NOT NULL DEFAULT FALSE,           -- if this command will allow subsequent steps to be executed even if this step fails. Typical case is a test command.
	internal       boolean    NOT NULL DEFAULT FALSE,           -- if the user has to be logged in to see files this command produces.
	timeout        int        NOT NULL DEFAULT 60,				-- after how many minutes should this step time out
	working_directory text    NULL DEFAULT NULL,                -- path this command should run in (relative to BUILD_DATA_SOURCE if it's a relative path)
	upload_files   text       NULL DEFAULT NULL                 -- comma separated list of files to upload, admits * and ? as wild cards
);

CREATE TABLE HostLane (
	id          serial     PRIMARY KEY,
	host_id     int        NOT NULL REFERENCES Host (id),
	lane_id     int        NOT NULL REFERENCES Lane (id),
	enabled     boolean    NOT NULL DEFAULT TRUE                -- if the lane is enabled on this host.
);

CREATE TABLE Lanefile (
	id             serial     PRIMARY KEY,
	name           text       NOT NULL,                          -- the filename
	contents       text       NOT NULL,
	mime           text       NOT NULL DEFAULT 'text/plain',
	
	-- this is some simple change tracking
	-- on every change a new Lanefile is stored, with the old contents and original_id referencing the real Lanefile
	original_id    int        NULL REFERENCES Lanefile (id),
	changed_date   timestamp  NULL -- the date the change was made	
);

CREATE TABLE Lanefiles (
	id             serial     PRIMARY KEY,
	lanefile_id    int        NOT NULL REFERENCES Lanefile (id),
	lane_id        int        NOT NULL REFERENCES Lane (id)
);

--
-- Do NOT put any cascade delete clauses on the File table.
-- we try to delete File, and we rely on an exception being thrown if the File is being used somewhere.
CREATE TABLE File (
	id              serial     PRIMARY KEY,
	filename        text       NOT NULL DEFAULT '',   -- filenames can be duplicate
	md5             text       UNIQUE NOT NULL,       -- having an md5 checksum allows us to use the same record for all equal files
	file_id         int        NULL DEFAULT NULL,         -- the large object id (should this be defined as oid instead of int?)
                                                      -- if this is null, the file is stored on the disk (in the db/files sub directory of DataDirectory as specified in MonkeyWrench.xml)
	mime            text       NOT NULL DEFAULT '',   -- the mime type of the file
	compressed_mime text       NOT NULL DEFAULT '',   -- if the file is stored compressed, this field is not '', and it specifies the compression algorithm (application/zip, tar, etc).
	                                                  -- this allows us to store for instance log files compressed, and the web server can deliver the compressed log file by
	                                                  -- adding the proper http response header.
	                                                  -- the md5 sum is calculated off the uncompressed file.
	size            int        NOT NULL DEFAULT 0,    -- filesize. int32 since I don't think we ever want to store files > 2GB in the database.
	hidden          boolean    NOT NULL DEFAULT FALSE --
);
CREATE INDEX file_idx_file_id_key ON File (file_id);

CREATE TABLE Revision (
	id       serial     PRIMARY KEY,
	lane_id  int        NOT NULL REFERENCES Lane(id),
	revision text       NOT NULL,
	author   text       NOT NULL DEFAULT '',
	date     timestamp  NOT NULL DEFAULT '2000-01-01 00:00:00+0',
	log      text       NOT NULL DEFAULT '', --TODO: delete this field
	log_file_id int     NULL DEFAULT NULL REFERENCES File (id), -- the file where the log is stored
	diff     text       NOT NULL DEFAULT '', --TODO: delete this field
	diff_file_id int    NULL DEFAULT NULL REFERENCES File (id), -- the file where the diff is stored.
	UNIQUE (lane_id, revision)
);
CREATE INDEX Revision_revision_idx ON Revision (revision);

CREATE TABLE RevisionWork (
	id             serial     PRIMARY KEY,
	lane_id        int        NOT NULL REFERENCES Lane (id) ON DELETE CASCADE,
	host_id        int        NOT NULL REFERENCES Host (id) ON DELETE CASCADE,
	workhost_id    int        NULL DEFAULT NULL REFERENCES Host (id) ON DELETE CASCADE, -- the host which is actually working on this revision. If NULL, work no started.
	revision_id    int        NOT NULL REFERENCES Revision (id) ON DELETE CASCADE,
	state          int        NOT NULL DEFAULT 0, -- same as Work.state, though not all are applicable

	-- ** possible states (evaluated in this order) ** 
	-- no work yet (transitional state until work has been added)
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
	completed      boolean    NOT NULL DEFAULT FALSE, -- if this revision has completed its work
	endtime        timestamp  NOT NULL DEFAULT '2000-01-01 00:00:00+0', -- the UTC time this revisionwork finished working
	-- alter table revisionwork add column endtime        timestamp  NOT NULL DEFAULT '2000-01-01 00:00:00+0';
	UNIQUE (lane_id, host_id, revision_id)
);
CREATE INDEX RevisionWork_revision_id_idx ON RevisionWork (revision_id);
CREATE INDEX RevisionWork_workhost_id_idx ON RevisionWork (workhost_id);
CREATE INDEX RevisionWork_host_id_idx ON RevisionWork (host_id);
CREATE INDEX RevisionWork_lane_id_idx ON RevisionWork (lane_id);
CREATE INDEX RevisionWork_endtime_idx ON RevisionWork (endtime);
CREATE INDEX RevisionWork_completed_idx ON RevisionWork (completed);
CREATE INDEX RevisionWork_state_idx ON RevisionWork (state);
-- -- recreate host_id fkey with delete cascade
-- alter table RevisionWork add constraint revisionwork_host_id_fkey2 foreign key (host_id) references host (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_host_id_fkey;
-- alter table RevisionWork add constraint revisionwork_host_id_fkey foreign key (host_id) references host (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_host_id_fkey2;
-- -- recreate lane_id fkey with delete cascade
-- alter table RevisionWork add constraint revisionwork_lane_id_fkey2 foreign key (lane_id) references lane (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_lane_id_fkey;
-- alter table RevisionWork add constraint revisionwork_lane_id_fkey foreign key (lane_id) references lane (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_lane_id_fkey2;
-- -- recreate revision_id fkey with delete cascade
-- alter table RevisionWork add constraint revisionwork_revision_id_fkey2 foreign key (revision_id) references revision (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_revision_id_fkey;
-- alter table RevisionWork add constraint revisionwork_revision_id_fkey foreign key (revision_id) references revision (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_revision_id_fkey2;
-- -- recreate workhost_id fkey with delete cascade
-- alter table RevisionWork add constraint revisionwork_workhost_id_fkey2 foreign key (workhost_id) references host (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_workhost_id_fkey;
-- alter table RevisionWork add constraint revisionwork_workhost_id_fkey foreign key (workhost_id) references host (id) on delete cascade;
-- alter table RevisionWork drop constraint revisionwork_workhost_id_fkey2;

CREATE TABLE Work (
	id               serial    PRIMARY KEY,
	--TODO: Pending removal -- lane_id          int       NOT NULL REFERENCES Lane(id), -- the lane
	host_id          int       NULL REFERENCES Host(id),  -- the host that is doing the work, null if not assigned.
	--TODO: Pending removal -- revision_id      int       NOT NULL REFERENCES Revision(id), -- the revision to use for this step
	command_id       int       NOT NULL REFERENCES Command(id),  -- the command to execute
	state            int       NOT NULL DEFAULT 0,                       -- 0 queued, 1 executing, 2 failed, 3 success, 
	                                                                     -- 4 aborted, 5 timeout, 6 paused, 7 skipped, 
	                                                                     -- 8 issues (RevisionWork only for nonfatal failures)
	                                                                     -- 9 dependency not fulfilled
	                                                                     -- 10 no work added yet [used in the RevisionWork table, not here]
	starttime        timestamp NOT NULL DEFAULT '2000-01-01 00:00:00+0', -- the UTC time when the step started to execute
	endtime          timestamp NOT NULL DEFAULT '2000-01-01 00:00:00+0', -- the UTC time when the step stopped to execute
	duration         int       NOT NULL DEFAULT 0,                       -- duration in seconds
	logfile          text      NOT NULL DEFAULT '',                      -- path of the log file
	summary          text      NOT NULL DEFAULT '',                      -- a one line summary of the log
	revisionwork_id  int       REFERENCES RevisionWork (id) ON DELETE CASCADE -- make NOT NULL after successful move
);
CREATE INDEX Work_revisionwork_id_idx ON Work (revisionwork_id);
CREATE INDEX Work_command_id_idx ON Work (command_id);

CREATE TABLE WorkFile (
	id             serial     PRIMARY KEY,
	work_id        int        NOT NULL REFERENCES Work (id) ON DELETE CASCADE,
	file_id        int        NOT NULL REFERENCES File (id),
	hidden         boolean    NOT NULL DEFAULT FALSE,
	filename       text       NOT NULL DEFAULT ''     -- we need to have a filename here too, since File is unique based on md5, we can have several WorkFiles with different names and same content.
	                                                  -- in any case, if this field is '', the filename is File's filename.
);
CREATE INDEX workfile_idx_file_id_key ON WorkFile (file_id);
CREATE INDEX workfile_idx_work_id_key ON WorkFile (work_id);
CREATE INDEX workfile_idx_filename_key ON WorkFile(filename);
CREATE INDEX workfile_idx_filename_pattern_key ON WorkFile(filename text_pattern_ops);

CREATE TABLE Person ( -- 'User' is a reserved word in sql...
	id             serial    PRIMARY KEY,
	login          text      NOT NULL,            -- the login name of the user a-zA-Z0-9_-
	password       text      NOT NULL DEFAULT '', -- the password (in plain text)
	fullname       text      NOT NULL DEFAULT '', -- the full name of the user
	roles          text      NULL DEFAULT NULL,   -- comma separated list of roles the user is member of
                                                  -- current values: <none>, Administrator, BuildBot
	irc_nicknames  text      NULL DEFAULT NULL,   -- comma or space separated list of nick names the user is known as on irc
	UNIQUE (login)
);
INSERT INTO Person (login, password, fullname, roles) VALUES ('admin', 'admin', 'admin', 'Administrator');
-- alter table person add column irc_nicknames text null default null;

CREATE TABLE UserEmail (
	id             serial    PRIMARY KEY,
	person_id      int       NOT NULL REFERENCES Person (id) ON DELETE CASCADE,
	email          text      NOT NULL
);
CREATE INDEX useremail_idx_email ON UserEmail (email);

CREATE TABLE Login (
	id             serial    PRIMARY KEY,
	cookie         text      UNIQUE NOT NULL,                 -- the cookie stored on the client machine
	person_id      int       NOT NULL REFERENCES Person (id) ON DELETE CASCADE, -- the user this login is valid for
	expires        timestamp NOT NULL,                   	  -- the date/time this login expires
	ip4            text      NOT NULL DEFAULT ''              -- the ip the user is connecting from
);
-- alter table login add constraint login_person_id_fkey2 foreign key (person_id) references person (id) on delete cascade;
-- alter table login drop constraint login_person_id_fkey;
-- alter table login add constraint login_person_id_fkey foreign key (person_id) references person (id) on delete cascade;
-- alter table login drop constraint login_person_id_fkey2;

CREATE TABLE IrcIdentity (
	id         serial    PRIMARY KEY,
	name       text      NOT NULL DEFAULT '', 
	servers    text      NOT NULL DEFAULT '',             -- a comma separated list of irc servers.
	password   text      NOT NULL DEFAULT '',             -- the password for the irc server
	channels   text      NOT NULL DEFAULT '',             -- a comma separated list of irc channels to join.
	nicks      text      NOT NULL DEFAULT 'monkeywrench'  -- a comma separated list of irc nicks to use
	use_ssl    boolean   NOT NULL DEFAULT FALSE,          -- if the server requires ssl
	join_channels boolean   NOT NULL DEFAULT TRUE,           -- if the channel(s) should be joined, or just /msg'ed
);
-- alter table ircidentity add column use_ssl boolean not null default false;
-- alter table ircidentity add column join_channels boolean not null default true;
-- alter table ircidentity add column password text not null default '';

CREATE TABLE EmailIdentity (
	id       serial    PRIMARY KEY,
	name     text      NOT NULL DEFAULT '', 
	email    text      NOT NULL DEFAULT '',             -- the email address used to send email
	password text      NOT NULL DEFAULT ''              -- the password for the above email address
);

CREATE TABLE Notification (
	id               serial    PRIMARY KEY,
	name             text      NOT NULL DEFAULT '',
	ircidentity_id   int       NULL REFERENCES IrcIdentity (id) ON DELETE CASCADE, 
	emailidentity_id int       NULL REFERENCES EmailIdentity (id) ON DELETE CASCADE,
	mode             int       NOT NULL DEFAULT 0,                                    -- 0: Default 1: MoonlightDrt 2: NUnit
	type             int       NOT NULL DEFAULT 0                                     -- 0: fatal failures only 1: non-fatal failures only 2: all failures
);

CREATE TABLE LaneNotification (
	id              serial    PRIMARY KEY,
	lane_id         int       NOT NULL REFERENCES Lane (id) ON DELETE CASCADE,
	notification_id int       NOT NULL REFERENCES Notification (id) ON DELETE CASCADE
);

CREATE TABLE BuildBotStatus (
	id             serial    PRIMARY KEY,
	host_id        int       NOT NULL REFERENCES Host (id) ON DELETE CASCADE,
	version        text      NOT NULL DEFAULT '',
	description    text      NOT NULL DEFAULT '',
	report_date    timestamp NOT NULL DEFAULT now ()
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
		Revision.author, Revision.revision, Revision.date
	FROM Work 
		INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id
		INNER JOIN Revision ON RevisionWork.revision_id = Revision.id 
		INNER JOIN Lane ON RevisionWork.lane_id = Lane.id 
		INNER JOIN Host AS MasterHost ON RevisionWork.host_id = MasterHost.id 
		LEFT JOIN Host AS WorkHost ON Work.host_id = WorkHost.id
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
	SELECT WorkFile.id, WorkFile.work_id, WorkFile.file_id, WorkFile.filename, WorkFile.hidden, File.mime, File.compressed_mime, File.md5, Command.internal, File.file_id AS file_file_id
	FROM WorkFile
		INNER JOIN File ON WorkFile.file_id = File.id
		INNER JOIN Work ON WorkFile.work_id = Work.id
		INNER JOIN Command ON Work.command_id = Command.id;
		
CREATE VIEW LaneDeletionDirectiveView AS
	SELECT LaneDeletionDirective.id, LaneDeletionDirective.lane_id, LaneDeletionDirective.file_deletion_directive_id, LaneDeletionDirective.enabled,
		FileDeletionDirective.name, FileDeletionDirective.filename, FileDeletionDirective.match_mode, FileDeletionDirective.condition, FileDeletionDirective.x
	FROM LaneDeletionDirective
		INNER JOIN FileDeletionDirective ON FileDeletionDirective.id = LaneDeletionDirective.file_deletion_directive_id;
		
-- ignore generator --		

-- method to get id for revisionwork, adding a new record if none is found.
CREATE LANGUAGE plpgsql;
CREATE OR REPLACE FUNCTION add_revisionwork (lane int, host int, revision int) RETURNS INT AS $$ 
BEGIN 
	IF 0 = (SELECT COUNT(*) FROM RevisionWork WHERE lane_id = lane AND host_id = host AND revision_id = revision) THEN
		INSERT INTO revisionwork (lane_id, host_id, revision_id) VALUES (lane, host, revision); 
	END IF; 
	RETURN (SELECT id FROM RevisionWork WHERE lane_id = lane AND host_id = host AND revision_id = revision);
END; 
$$ 
LANGUAGE plpgsql; 

-- unignore generator --

